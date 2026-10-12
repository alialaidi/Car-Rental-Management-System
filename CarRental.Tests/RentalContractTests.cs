using _CarRental.Application.Common.Interfaces;
using _CarRental.Application.Common.Models;
using _CarRental.Application.DTOs.Contracts;
using _CarRental.Application.Services;
using _CarRental.Application.Validators;
using _CarRental.Domain.Entities;
using _CarRental.Domain.Constants;
using _CarRental.Domain.Enums;
using _CarRental.Domain.Exceptions;
using _CarRental.Domain.Interfaces.Repositories;
using _CarRental.Domain.Services;
using _CarRental.Domain.ValueObjects;
using Xunit;

namespace _CarRental.Tests;

public sealed class RentalContractTests
{
    private static readonly DateTime Now = new(2026, 10, 11, 12, 0, 0, DateTimeKind.Utc);

    [Theory]
    [InlineData(1, 0, 1)]
    [InlineData(0, 23, 1)]
    [InlineData(1, 1, 2)]
    [InlineData(-1, 0, 1)]
    public void CalculateDaysRoundsPartialDaysUpAndUsesMinimumOneDay(int days, int hours, int expected)
    {
        Assert.Equal(expected, RentalPriceCalculator.CalculateDays(Now, Now.AddDays(days).AddHours(hours)));
    }

    [Fact]
    public void CalculateBasicRoundsMoneyAwayFromZero()
    {
        Assert.Equal(20.00m, RentalPriceCalculator.CalculateBasic(19.995m, 1));
    }

    [Fact]
    public void CalculateTotalRejectsDiscountAboveBasicAndFees()
    {
        Assert.Throws<ArgumentOutOfRangeException>(() => RentalPriceCalculator.CalculateTotal(100m, 10m, 111m));
    }

    [Fact]
    public void CalculateFinalBasicChargesLateDaysFromPickupPriceSnapshot()
    {
        Assert.Equal(60m, RentalPriceCalculator.CalculateFinalBasic(40m, 2, 3));
        Assert.Equal(64m, RentalPriceCalculator.CalculateFinalTotal(60m, 3m, 2m, 4m, 5m));
    }

    [Fact]
    public void CompleteRejectsReturnBeforePickup()
    {
        var contract = ActiveContract();
        var error = Assert.Throws<BusinessRuleException>(() => contract.Complete(
            321, Now.AddMinutes(-1), 101, FuelLevel.Half, null, 1, 20m, 0m, 0m));
        Assert.Equal("RETURN_BEFORE_PICKUP", error.Code);
    }

    [Fact]
    public void CompleteRejectsFinalNegativeTotal()
    {
        var contract = ActiveContract();
        var error = Assert.Throws<BusinessRuleException>(() => contract.Complete(
            321, Now.AddDays(1), 101, FuelLevel.Half, null, 1, 1m, 0m, 0m));
        Assert.Equal("INVALID_CONTRACT_AMOUNT", error.Code);
    }

    [Fact]
    public async Task CreateConvertsConfirmedReservationAndSnapshotsPrice()
    {
        var fixture = new Fixture();

        var contract = await fixture.Service.CreateAsync(new CreateContractRequest(
            77, null, FuelLevel.Half, "Small scratch on rear bumper", DiscountAmount: 5m, AdditionalFees: 3m));

        Assert.Equal(77, contract.ReservationId);
        Assert.Equal("RC-20261011-000001", contract.ContractNumber);
        Assert.Equal(42, contract.CustomerId);
        Assert.Equal(3, contract.VehicleId);
        Assert.Equal(321, contract.EmployeeId);
        Assert.Equal(2, contract.RentalDays);
        Assert.Equal(40m, contract.BasicRentalPrice);
        Assert.Equal(5m, contract.DiscountAmount);
        Assert.Equal(3m, contract.AdditionalFees);
        Assert.Equal(38m, contract.TotalAmount);
        Assert.Equal(100, contract.PickupMileage);
        Assert.Equal("Active", contract.Status);
        Assert.Equal(1, fixture.Contracts.CreateCount);
        Assert.Equal(1, fixture.UnitOfWork.CommitCount);
        Assert.Equal("Rented", fixture.Vehicles.Vehicle.Status);
        Assert.Equal(AuditActions.RentalContractCreated, Assert.Single(fixture.Audit.Actions));
    }

    [Fact]
    public async Task CreateRejectsReservationThatIsNotConfirmed()
    {
        var fixture = new Fixture();
        fixture.Reservations.Existing = ReservationFor(ReservationStatus.Pending);

        var error = await Assert.ThrowsAsync<ConflictException>(() => fixture.Service.CreateAsync(ValidRequest()));

        Assert.Equal("RESERVATION_NOT_CONFIRMED", error.Code);
        Assert.Equal(0, fixture.Contracts.CreateCount);
    }

    [Fact]
    public async Task CreateRejectsMileageBelowVehicleCurrentMileage()
    {
        var fixture = new Fixture();

        var error = await Assert.ThrowsAsync<BusinessRuleException>(() =>
            fixture.Service.CreateAsync(ValidRequest() with { PickupMileage = 99 }));

        Assert.Equal("INVALID_PICKUP_MILEAGE", error.Code);
        Assert.Equal(0, fixture.Contracts.CreateCount);
    }

    [Fact]
    public async Task CreateRejectsAnotherBranchReservation()
    {
        var fixture = new Fixture();
        fixture.CurrentUser.BranchId = 2;

        await Assert.ThrowsAsync<ForbiddenException>(() => fixture.Service.CreateAsync(ValidRequest()));

        Assert.Equal(0, fixture.Contracts.CreateCount);
    }

    [Fact]
    public async Task CreateRejectsDiscountThatMakesTotalNegativeAsBusinessRule()
    {
        var fixture = new Fixture();

        var error = await Assert.ThrowsAsync<BusinessRuleException>(() =>
            fixture.Service.CreateAsync(ValidRequest() with { DiscountAmount = 100m }));

        Assert.Equal("INVALID_CONTRACT_AMOUNT", error.Code);
        Assert.Equal(0, fixture.Contracts.CreateCount);
    }

    [Fact]
    public async Task CustomerCannotReadAnotherCustomersContract()
    {
        var fixture = new Fixture();
        fixture.CurrentUser.Roles = ["Customer"];
        fixture.Contracts.Contract.CustomerId = 900;

        await Assert.ThrowsAsync<NotFoundException>(() => fixture.Service.GetByIdAsync(1));
    }

    [Fact]
    public async Task ReturnCompletesContractAndAuditsFinalTotal()
    {
        var fixture = new Fixture();
        var result = await fixture.Service.ReturnAsync(1, new ReturnVehicleRequest(
            123, FuelLevel.Quarter, FuelCharges: 2m, DamageCharges: 4m, ConditionNotes: "Returned clean",
            RequiresMaintenance: true, ActualReturnDate: new DateTimeOffset(Now.AddDays(2).AddHours(1))));

        Assert.Equal("Completed", result.Status);
        Assert.Equal(3, result.RentalDays);
        Assert.Equal(60m, result.BasicRentalPrice);
        Assert.Equal(64m, result.TotalAmount);
        Assert.Equal(123, result.ReturnMileage);
        Assert.Equal("Quarter", result.ReturnFuelLevel);
        Assert.Equal(Now.AddDays(2).AddHours(1), result.ActualReturnDateUtc);
        Assert.Equal(1, fixture.Contracts.CompleteCount);
        Assert.Equal(1, fixture.UnitOfWork.CommitCount);
        Assert.Equal(1, fixture.Contracts.ReturnBranchId);
        Assert.True(fixture.Contracts.RequiresMaintenance);
        Assert.Contains("Return: Returned clean", fixture.Contracts.Contract.ConditionNotes);
        Assert.Equal(AuditActions.RentalContractReturned, Assert.Single(fixture.Audit.Actions));
    }

    [Fact]
    public async Task ReturnRejectsReturnToAnotherBranch()
    {
        var fixture = new Fixture();
        fixture.CurrentUser.BranchId = 2;

        await Assert.ThrowsAsync<ForbiddenException>(() => fixture.Service.ReturnAsync(1, ValidReturnRequest()));

        Assert.Equal(0, fixture.Contracts.CompleteCount);
    }

    [Fact]
    public async Task ReturnRejectsCompletedContract()
    {
        var fixture = new Fixture();
        fixture.Contracts.Contract.Status = ContractStatus.Completed;

        var error = await Assert.ThrowsAsync<ConflictException>(() => fixture.Service.ReturnAsync(1, ValidReturnRequest()));

        Assert.Equal("CONTRACT_NOT_ACTIVE", error.Code);
        Assert.Equal(0, fixture.Contracts.CompleteCount);
    }

    [Fact]
    public async Task ReturnRejectsMileageBelowPickupMileage()
    {
        var fixture = new Fixture();

        var error = await Assert.ThrowsAsync<BusinessRuleException>(() =>
            fixture.Service.ReturnAsync(1, ValidReturnRequest() with { ReturnMileage = 99 }));

        Assert.Equal("INVALID_RETURN_MILEAGE", error.Code);
        Assert.Equal(0, fixture.Contracts.CompleteCount);
    }

    [Fact]
    public async Task ReturnRejectsDateBeforePickup()
    {
        var fixture = new Fixture();

        var error = await Assert.ThrowsAsync<BusinessRuleException>(() =>
            fixture.Service.ReturnAsync(1, ValidReturnRequest() with { ActualReturnDate = new DateTimeOffset(Now.AddMinutes(-1)) }));

        Assert.Equal("RETURN_BEFORE_PICKUP", error.Code);
        Assert.Equal(0, fixture.Contracts.CompleteCount);
    }

    [Fact]
    public async Task ReturnRejectsNegativeCharges()
    {
        var fixture = new Fixture();

        await Assert.ThrowsAsync<BusinessRuleException>(() =>
            fixture.Service.ReturnAsync(1, ValidReturnRequest() with { FuelCharges = -1m }));
        Assert.Equal(0, fixture.Contracts.CompleteCount);
    }

    private static CreateContractRequest ValidRequest() => new(77, null, FuelLevel.Half, null);
    private static ReturnVehicleRequest ValidReturnRequest() => new(101, FuelLevel.Full);

    private static RentalContract ActiveContract() => RentalContract.Create(77, 42, 3, 321, Now, 100,
        FuelLevel.Half, null, 2, 40m, 5m, 3m);

    private static Reservation ReservationFor(ReservationStatus status) => new(status)
    {
        ReservationId = 77, CustomerId = 42, VehicleId = 3, PickupBranchId = 1, ReturnBranchId = 1,
        PickupDateTime = Now.AddHours(-1), ExpectedReturnDateTime = Now.AddDays(1).AddHours(1), CreatedAt = Now.AddDays(-1)
    };

    private sealed class Fixture
    {
        public FakeReservationRepository Reservations { get; } = new();
        public FakeVehicleRepository Vehicles { get; } = new();
        public FakeContractRepository Contracts { get; } = new();
        public FakeCurrentUser CurrentUser { get; } = new();
        public FakeAuditLogger Audit { get; } = new();
        public FakeUnitOfWork UnitOfWork { get; }
        public RentalContractService Service { get; }

        public Fixture()
        {
            Reservations.Existing = ReservationFor(ReservationStatus.Confirmed);
            UnitOfWork = new FakeUnitOfWork(Reservations, Vehicles, Contracts);
            Service = new RentalContractService(UnitOfWork, CurrentUser, Audit, new FixedClock(),
                new CreateContractRequestValidator(), new ReturnVehicleRequestValidator());
        }
    }

    private sealed class FakeContractRepository : IRentalContractRepository
    {
        public int CreateCount { get; private set; }
        public RentalContract Contract { get; private set; } = new()
        {
            ContractId = 1, ReservationId = 77, ContractNumber = "RC-20261011-000001", CustomerId = 42,
            VehicleId = 3, EmployeeId = 321, ActualPickupDate = Now, PickupMileage = 100,
            PickupFuelLevel = FuelLevel.Half, RentalDays = 2, BasicRentalPrice = 40m,
            DiscountAmount = 5m, AdditionalFees = 3m, TotalAmount = 38m, Status = ContractStatus.Active,
            PickupBranchId = 1
        };
        public int CompleteCount { get; private set; }
        public int ReturnBranchId { get; private set; }
        public bool RequiresMaintenance { get; private set; }

        public Task<int> CreateFromReservationAsync(RentalContract contract)
        {
            CreateCount++;
            contract.ContractId = 1;
            contract.ContractNumber = Contract.ContractNumber;
            Contract.ReservationId = contract.ReservationId;
            Contract.CustomerId = contract.CustomerId;
            Contract.VehicleId = contract.VehicleId;
            Contract.EmployeeId = contract.EmployeeId;
            Contract.ActualPickupDate = contract.ActualPickupDate;
            Contract.PickupMileage = contract.PickupMileage;
            Contract.PickupFuelLevel = contract.PickupFuelLevel;
            Contract.ConditionNotes = contract.ConditionNotes;
            Contract.RentalDays = contract.RentalDays;
            Contract.BasicRentalPrice = contract.BasicRentalPrice;
            Contract.DiscountAmount = contract.DiscountAmount;
            Contract.AdditionalFees = contract.AdditionalFees;
            Contract.TotalAmount = contract.TotalAmount;
            Contract.PickupBranchId = contract.PickupBranchId;
            return Task.FromResult(1);
        }

        public Task CompleteAsync(RentalContract contract, int returnBranchId, bool requiresMaintenance)
        {
            CompleteCount++;
            ReturnBranchId = returnBranchId;
            RequiresMaintenance = requiresMaintenance;
            Contract = contract;
            return Task.CompletedTask;
        }

        public Task<RentalContract?> GetByIdAsync(int contractId) => Task.FromResult<RentalContract?>(contractId == Contract.ContractId ? Contract : null);
        public Task<(IReadOnlyList<RentalContract> Items, int TotalCount)> ListAsync(int? branchId, int page, int pageSize) => throw new NotSupportedException();
        public Task<(IReadOnlyList<RentalContract> Items, int TotalCount)> ListByCustomerAsync(int customerId, int page, int pageSize) => throw new NotSupportedException();
    }

    private sealed class FakeReservationRepository : IReservationRepository
    {
        public Reservation? Existing { get; set; }
        public Task<int> CreateAsync(Reservation reservation) => throw new NotSupportedException();
        public Task<Reservation?> GetByIdAsync(int reservationId) => Task.FromResult(reservationId == Existing?.ReservationId ? Existing : null);
        public Task<(IReadOnlyList<Reservation> Items, int TotalCount)> ListByCustomerAsync(int customerId, int page, int pageSize) => throw new NotSupportedException();
        public Task<(IReadOnlyList<Reservation> Items, int TotalCount)> ListForBranchAsync(int? branchId, ReservationStatus? status, DateTime? pickupFromUtc, DateTime? pickupToUtc, int page, int pageSize) => throw new NotSupportedException();
        public Task UpdateStatusAsync(int reservationId, ReservationStatus expectedStatus, ReservationStatus newStatus, int? confirmedByEmployeeId = null) => throw new NotSupportedException();
    }

    private sealed class FakeVehicleRepository : IVehicleRepository
    {
        public Vehicle Vehicle { get; } = new() { VehicleId = 3, DailyPrice = 20m, BranchId = 1, CurrentMileage = 100, Status = "Reserved" };
        public Task<Vehicle?> GetByIdAsync(int vehicleId) => Task.FromResult<Vehicle?>(vehicleId == 3 ? Vehicle : null);
        public Task SetStatusAsync(int vehicleId, VehicleStatus status) { Vehicle.Status = status.ToString(); return Task.CompletedTask; }
        public Task<int> CreateAsync(Vehicle vehicle) => throw new NotSupportedException();
        public Task<IReadOnlyList<Vehicle>> ListByBranchAsync(int branchId, int page, int pageSize) => throw new NotSupportedException();
        public Task<int> GetCountByBranchAsync(int branchId) => throw new NotSupportedException();
        public Task<VehicleCatalogPage> SearchAvailableAsync(VehicleSearchCriteria criteria) => throw new NotSupportedException();
        public Task<VehicleCatalogDetails?> GetCatalogDetailsAsync(int vehicleId) => throw new NotSupportedException();
        public Task<bool> IsAvailableAsync(int vehicleId, DateTime pickupDateTime, DateTime expectedReturnDateTime) => throw new NotSupportedException();
        public Task LockForReservationReviewAsync(int vehicleId) => throw new NotSupportedException();
        public Task<bool> HasOtherConfirmedReservationAsync(int vehicleId, int excludingReservationId) => throw new NotSupportedException();
        public Task UpdateAsync(Vehicle vehicle) => throw new NotSupportedException();
        public Task DeleteAsync(int vehicleId) => throw new NotSupportedException();
    }

    private sealed class FakeUnitOfWork(FakeReservationRepository reservations, FakeVehicleRepository vehicles, FakeContractRepository contracts) : IUnitOfWork
    {
        public int CommitCount { get; private set; }
        public IReservationRepository Reservations => reservations;
        public IVehicleRepository Vehicles => vehicles;
        public IRentalContractRepository Contracts => contracts;
        public IPaymentRepository Payments => throw new NotSupportedException();
        public IUserRepository Users => throw new NotSupportedException();
        public ICustomerRepository Customers => throw new NotSupportedException();
        public IRefreshTokenRepository RefreshTokens => throw new NotSupportedException();
        public IBranchRepository Branches => throw new NotSupportedException();
        public IEmployeeRepository Employees => throw new NotSupportedException();
        public IVehicleImageRepository VehicleImages => throw new NotSupportedException();
        public ILookupRepository Lookups => throw new NotSupportedException();
        public Task BeginTransactionAsync() => Task.CompletedTask;
        public Task CommitAsync() { CommitCount++; vehicles.Vehicle.Status = "Rented"; return Task.CompletedTask; }
        public Task RollbackAsync() => Task.CompletedTask;
        public ValueTask DisposeAsync() => ValueTask.CompletedTask;
    }

    private sealed class FakeCurrentUser : ICurrentUser
    {
        public int UserId => 501;
        public int BranchId { get; set; } = 1;
        public IReadOnlyCollection<string> Roles { get; set; } = ["Employee"];
        public bool IsInRole(string role) => Roles.Contains(role, StringComparer.Ordinal);
        public Task<int> GetCustomerIdAsync() => Task.FromResult(42);
        public Task<int> GetEmployeeIdAsync() => Task.FromResult(321);
        public Task<int> GetBranchIdAsync() => Task.FromResult(BranchId);
    }

    private sealed class FakeAuditLogger : IAuditLogger
    {
        public List<string> Actions { get; } = [];
        public Task LogAsync(string action, string entityName, string? entityId, object? oldValue, object? newValue, int? actorUserId = null)
        { Actions.Add(action); return Task.CompletedTask; }
    }

    private sealed class FixedClock : IDateTimeProvider { public DateTime UtcNow => Now; }
}
