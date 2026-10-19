using _CarRental.Application.Common.Interfaces;
using _CarRental.Application.Common.Models;
using _CarRental.Application.DTOs.Reservations;
using _CarRental.Application.Services;
using _CarRental.Application.Validators;
using _CarRental.Domain.Constants;
using _CarRental.Domain.Entities;
using _CarRental.Domain.Enums;
using _CarRental.Domain.Exceptions;
using _CarRental.Domain.Interfaces.Repositories;
using _CarRental.Domain.ValueObjects;
using Xunit;

namespace _CarRental.Tests;

public sealed class ReservationServiceTests
{
    private static readonly DateTime Now = new(2026, 10, 11, 12, 0, 0, DateTimeKind.Utc);

    [Fact]
    public async Task CreateUsesCustomerFromCurrentUserAndAuditsPendingReservation()
    {
        var fixture = new Fixture();

        var result = await fixture.Service.CreateAsync(ValidRequest());

        Assert.Equal(ReservationStatus.Pending.ToString(), result.Status);
        Assert.Equal(42, fixture.Reservations.Created!.CustomerId);
        Assert.Equal(AuditActions.ReservationCreated, Assert.Single(fixture.Audit.Entries).Action);
        Assert.Equal(501, fixture.Audit.Entries[0].ActorUserId);
    }

    [Fact]
    public async Task CreateRejectsLicenseThatExpiresBeforeExpectedReturn()
    {
        var fixture = new Fixture();
        fixture.Customers.Customer.DrivingLicenseExpiry = Now.Date.AddDays(1);

        var exception = await Assert.ThrowsAsync<BusinessRuleException>(() => fixture.Service.CreateAsync(ValidRequest()));

        Assert.Equal("DRIVING_LICENSE_EXPIRED", exception.Code);
        Assert.Null(fixture.Reservations.Created);
    }

    [Fact]
    public async Task CreateRejectsUnavailableVehicleAsConflict()
    {
        var fixture = new Fixture();
        fixture.Vehicles.IsAvailable = false;

        var exception = await Assert.ThrowsAsync<ConflictException>(() => fixture.Service.CreateAsync(ValidRequest()));

        Assert.Equal("VEHICLE_NOT_AVAILABLE", exception.Code);
        Assert.Null(fixture.Reservations.Created);
    }

    [Fact]
    public async Task CustomerCannotReadAnotherCustomersReservation()
    {
        var fixture = new Fixture();
        fixture.Reservations.Existing = new Reservation
        {
            ReservationId = 77,
            CustomerId = 999,
            VehicleId = 3,
            PickupBranchId = 1,
            ReturnBranchId = 1,
            PickupDateTime = Now.AddDays(1),
            ExpectedReturnDateTime = Now.AddDays(2),
            CreatedAt = Now
        };

        await Assert.ThrowsAsync<NotFoundException>(() => fixture.Service.GetMineAsync(77));
    }

    [Fact]
    public async Task CustomerCanCancelOnlyPendingReservation()
    {
        var fixture = new Fixture();
        fixture.Reservations.Existing = ReservationForCurrentCustomer(ReservationStatus.Pending);

        var result = await fixture.Service.CancelMineAsync(77);

        Assert.Equal(ReservationStatus.Cancelled.ToString(), result.Status);
        Assert.Equal((ReservationStatus.Pending, ReservationStatus.Cancelled, (int?)null), fixture.Reservations.StatusUpdate);
        Assert.Equal(AuditActions.ReservationStatusChanged, Assert.Single(fixture.Audit.Entries).Action);
    }

    [Fact]
    public async Task ConfirmedReservationCannotBeCancelledByCustomer()
    {
        var fixture = new Fixture();
        fixture.Reservations.Existing = ReservationForCurrentCustomer(ReservationStatus.Confirmed);

        var exception = await Assert.ThrowsAsync<ConflictException>(() => fixture.Service.CancelMineAsync(77));

        Assert.Equal("RESERVATION_CONFIRMED_CONTACT_BRANCH", exception.Code);
        Assert.Null(fixture.Reservations.StatusUpdate);
    }

    [Fact]
    public async Task StaffConfirmSetsEmployeeAndReservesVehicleAtomically()
    {
        var fixture = new Fixture();
        fixture.CurrentUser.SetStaff("Employee", branchId: 1, employeeId: 321);
        fixture.Reservations.Existing = ReservationForCurrentCustomer(ReservationStatus.Pending);

        var result = await fixture.Service.ConfirmAsync(77);

        Assert.Equal(ReservationStatus.Confirmed.ToString(), result.Status);
        Assert.Equal(321, fixture.Reservations.Existing.ConfirmedByEmployeeId);
        Assert.Equal((ReservationStatus.Pending, ReservationStatus.Confirmed, 321), fixture.Reservations.StatusUpdate);
        Assert.Equal(VehicleStatus.Reserved.ToString(), fixture.Vehicles.Vehicle.Status);
        Assert.Equal(AuditActions.ReservationStatusChanged, Assert.Single(fixture.Audit.Entries).Action);
    }

    [Fact]
    public async Task StaffCannotReviewAnotherBranchReservation()
    {
        var fixture = new Fixture();
        fixture.CurrentUser.SetStaff("BranchManager", branchId: 2, employeeId: 321);
        fixture.Reservations.Existing = ReservationForCurrentCustomer(ReservationStatus.Pending);

        await Assert.ThrowsAsync<ForbiddenException>(() => fixture.Service.ConfirmAsync(77));
        Assert.Null(fixture.Reservations.StatusUpdate);
    }

    [Fact]
    public async Task RejectReleasesReservedVehicleOnlyWhenNoOtherConfirmedReservationExists()
    {
        var fixture = new Fixture();
        fixture.CurrentUser.SetStaff("Employee", branchId: 1, employeeId: 321);
        fixture.Reservations.Existing = ReservationForCurrentCustomer(ReservationStatus.Pending);
        fixture.Vehicles.Vehicle.Status = VehicleStatus.Reserved.ToString();

        var rejected = await fixture.Service.RejectAsync(77, new RejectReservationRequest());

        Assert.Equal(ReservationStatus.Rejected.ToString(), rejected.Status);
        Assert.Equal(VehicleStatus.Available.ToString(), fixture.Vehicles.Vehicle.Status);
    }

    [Fact]
    public async Task RejectKeepsVehicleReservedWhenAnotherConfirmedReservationExists()
    {
        var fixture = new Fixture();
        fixture.CurrentUser.SetStaff("Employee", branchId: 1, employeeId: 321);
        fixture.Reservations.Existing = ReservationForCurrentCustomer(ReservationStatus.Pending);
        fixture.Vehicles.Vehicle.Status = VehicleStatus.Reserved.ToString();
        fixture.Vehicles.HasOtherConfirmedReservation = true;

        await fixture.Service.RejectAsync(77, new RejectReservationRequest());

        Assert.Equal(VehicleStatus.Reserved.ToString(), fixture.Vehicles.Vehicle.Status);
    }

    [Fact]
    public async Task StaffCannotConfirmNonPendingReservation()
    {
        var fixture = new Fixture();
        fixture.CurrentUser.SetStaff("Employee", branchId: 1, employeeId: 321);
        fixture.Reservations.Existing = ReservationForCurrentCustomer(ReservationStatus.Confirmed);

        await Assert.ThrowsAsync<ConflictException>(() => fixture.Service.ConfirmAsync(77));
        Assert.Null(fixture.Reservations.StatusUpdate);
    }

    [Fact]
    public async Task StaffCannotCancelTerminalReservation()
    {
        var fixture = new Fixture();
        fixture.CurrentUser.SetStaff("Employee", branchId: 1, employeeId: 321);
        fixture.Reservations.Existing = ReservationForCurrentCustomer(ReservationStatus.Rejected);

        await Assert.ThrowsAsync<ConflictException>(() => fixture.Service.CancelByStaffAsync(77));
        Assert.Null(fixture.Reservations.StatusUpdate);
    }

    private static CreateReservationRequest ValidRequest() => new()
    {
        VehicleId = 3,
        PickupBranchId = 1,
        ReturnBranchId = 1,
        PickupDateTime = new DateTimeOffset(Now.AddDays(1)),
        ExpectedReturnDateTime = new DateTimeOffset(Now.AddDays(3))
    };

    private static Reservation ReservationForCurrentCustomer(ReservationStatus status) => new(status)
    {
        ReservationId = 77,
        CustomerId = 42,
        VehicleId = 3,
        PickupBranchId = 1,
        ReturnBranchId = 1,
        PickupDateTime = Now.AddDays(1),
        ExpectedReturnDateTime = Now.AddDays(2),
        CreatedAt = Now
    };

    private sealed class Fixture
    {
        public FakeCustomerRepository Customers { get; } = new();
        public FakeVehicleRepository Vehicles { get; } = new();
        public FakeReservationRepository Reservations { get; } = new();
        public FakeAuditLogger Audit { get; } = new();
        public FakeCurrentUser CurrentUser { get; } = new();
        public ReservationService Service { get; }

        public Fixture()
        {
            var unitOfWork = new FakeUnitOfWork(Customers, Vehicles, Reservations);
            Service = new ReservationService(unitOfWork, CurrentUser, Audit,
                new CreateReservationRequestValidator(new FixedDateTimeProvider()), new FixedDateTimeProvider());
        }
    }

    private sealed class FakeCustomerRepository : ICustomerRepository
    {
        public Customer Customer { get; } = new()
        {
            CustomerId = 42,
            UserId = 501,
            DrivingLicenseExpiry = Now.Date.AddYears(1)
        };
        public Task<Customer?> GetByUserIdAsync(int userId) => Task.FromResult<Customer?>(userId == 501 ? Customer : null);
        public Task<bool> ExistsByDrivingLicenseNumberAsync(string drivingLicenseNumber) => Task.FromResult(false);
        public Task<int> CreateAsync(Customer customer) => throw new NotSupportedException();
    }

    private sealed class FakeVehicleRepository : IVehicleRepository
    {
        public bool IsAvailable { get; set; } = true;
        public bool HasOtherConfirmedReservation { get; set; }
        public Vehicle Vehicle { get; } = new() { VehicleId = 3, BranchId = 1 };
        public Task<bool> IsAvailableAsync(int vehicleId, DateTime pickupDateTime, DateTime expectedReturnDateTime) => Task.FromResult(IsAvailable);
        public Task<Vehicle?> GetByIdAsync(int vehicleId) => Task.FromResult<Vehicle?>(vehicleId == 3 ? Vehicle : null);
        public Task SetStatusAsync(int vehicleId, VehicleStatus status) { Vehicle.Status = status.ToString(); return Task.CompletedTask; }
        public Task LockForReservationReviewAsync(int vehicleId) => Task.CompletedTask;
        public Task<bool> HasOtherConfirmedReservationAsync(int vehicleId, int excludingReservationId) => Task.FromResult(HasOtherConfirmedReservation);
        public Task<int> CreateAsync(Vehicle vehicle) => throw new NotSupportedException();
        public Task<IReadOnlyList<Vehicle>> ListByBranchAsync(int branchId, int page, int pageSize) => throw new NotSupportedException();
        public Task<int> GetCountByBranchAsync(int branchId) => throw new NotSupportedException();
        public Task<VehicleCatalogPage> SearchAvailableAsync(VehicleSearchCriteria criteria) => throw new NotSupportedException();
        public Task<VehicleCatalogDetails?> GetCatalogDetailsAsync(int vehicleId) => throw new NotSupportedException();
        public Task UpdateAsync(Vehicle vehicle) => throw new NotSupportedException();
        public Task DeleteAsync(int vehicleId) => throw new NotSupportedException();
    }

    private sealed class FakeBranchRepository : IBranchRepository
    {
        public Task<Branch?> GetByIdAsync(int branchId) => Task.FromResult<Branch?>(branchId > 0 ? new Branch { BranchId = branchId } : null);
        public Task<int> CreateAsync(Branch branch) => throw new NotSupportedException();
        public Task<IReadOnlyList<Branch>> ListAsync(int page, int pageSize) => throw new NotSupportedException();
        public Task<int> GetCountAsync() => throw new NotSupportedException();
        public Task UpdateAsync(Branch branch) => throw new NotSupportedException();
        public Task<bool> HasDependentsAsync(int branchId) => throw new NotSupportedException();
        public Task DeleteAsync(int branchId) => throw new NotSupportedException();
        public Task SetManagerAsync(int branchId, int? employeeId) => throw new NotSupportedException();
    }

    private sealed class FakeReservationRepository : IReservationRepository
    {
        public Reservation? Created { get; private set; }
        public Reservation? Existing { get; set; }
        public (ReservationStatus Expected, ReservationStatus New, int? EmployeeId)? StatusUpdate { get; private set; }

        public Task<int> CreateAsync(Reservation reservation)
        {
            Created = reservation;
            return Task.FromResult(77);
        }

        public Task<Reservation?> GetByIdAsync(int reservationId) => Task.FromResult(Existing);
        public Task<(IReadOnlyList<Reservation> Items, int TotalCount)> ListByCustomerAsync(int customerId, int page, int pageSize) =>
            Task.FromResult(((IReadOnlyList<Reservation>)Array.Empty<Reservation>(), 0));
        public Task<(IReadOnlyList<Reservation> Items, int TotalCount)> ListForBranchAsync(
            int? branchId, ReservationStatus? status, DateTime? pickupFromUtc, DateTime? pickupToUtc, int page, int pageSize) =>
            Task.FromResult(((IReadOnlyList<Reservation>)Array.Empty<Reservation>(), 0));
        public Task UpdateStatusAsync(int reservationId, ReservationStatus expectedStatus, ReservationStatus newStatus, int? confirmedByEmployeeId = null)
        {
            StatusUpdate = (expectedStatus, newStatus, confirmedByEmployeeId);
            if (Existing is not null)
            {
                Existing = new Reservation(newStatus)
                {
                    ReservationId = Existing.ReservationId,
                    CustomerId = Existing.CustomerId,
                    VehicleId = Existing.VehicleId,
                    PickupBranchId = Existing.PickupBranchId,
                    ReturnBranchId = Existing.ReturnBranchId,
                    PickupDateTime = Existing.PickupDateTime,
                    ExpectedReturnDateTime = Existing.ExpectedReturnDateTime,
                    ConfirmedByEmployeeId = confirmedByEmployeeId ?? Existing.ConfirmedByEmployeeId,
                    CreatedAt = Existing.CreatedAt
                };
            }
            return Task.CompletedTask;
        }
    }

    private sealed class FakeUnitOfWork(
        ICustomerRepository customers,
        IVehicleRepository vehicles,
        IReservationRepository reservations) : IUnitOfWork
    {
        public ICustomerRepository Customers => customers;
        public IVehicleRepository Vehicles => vehicles;
        public IBranchRepository Branches { get; } = new FakeBranchRepository();
        public IReservationRepository Reservations => reservations;
        public IUserRepository Users => throw new NotSupportedException();
        public IRefreshTokenRepository RefreshTokens => throw new NotSupportedException();
        public IEmployeeRepository Employees => throw new NotSupportedException();
        public IVehicleImageRepository VehicleImages => throw new NotSupportedException();
        public ILookupRepository Lookups => throw new NotSupportedException();
        public IRentalContractRepository Contracts => throw new NotSupportedException();
        public IPaymentRepository Payments => throw new NotSupportedException();
        public IMaintenanceRepository Maintenance => throw new NotSupportedException();
        public IReviewRepository Reviews => throw new NotSupportedException();
        public IReportRepository Reports => throw new NotSupportedException();
        public Task BeginTransactionAsync() => Task.CompletedTask;
        public Task CommitAsync() => Task.CompletedTask;
        public Task RollbackAsync() => Task.CompletedTask;
        public ValueTask DisposeAsync() => ValueTask.CompletedTask;
    }

    private sealed class FakeCurrentUser : ICurrentUser
    {
        private IReadOnlyCollection<string> _roles = ["Customer"];
        public int UserId => 501;
        public int BranchId { get; private set; } = 1;
        public int EmployeeId { get; private set; } = 321;
        public IReadOnlyCollection<string> Roles => _roles;
        public bool IsInRole(string role) => Roles.Contains(role, StringComparer.Ordinal);
        public Task<int> GetCustomerIdAsync() => Task.FromResult(42);
        public Task<int> GetEmployeeIdAsync() => Task.FromResult(EmployeeId);
        public Task<int> GetBranchIdAsync() => Task.FromResult(BranchId);
        public void SetStaff(string role, int branchId, int employeeId)
        {
            _roles = [role];
            BranchId = branchId;
            EmployeeId = employeeId;
        }
    }

    private sealed class FakeAuditLogger : IAuditLogger
    {
        public List<AuditEntry> Entries { get; } = [];
        public Task LogAsync(string action, string entityName, string? entityId, object? oldValue, object? newValue, int? actorUserId = null)
        {
            Entries.Add(new AuditEntry(action, actorUserId));
            return Task.CompletedTask;
        }
    }

    private sealed record AuditEntry(string Action, int? ActorUserId);

    private sealed class FixedDateTimeProvider : IDateTimeProvider
    {
        public DateTime UtcNow => Now;
    }
}
