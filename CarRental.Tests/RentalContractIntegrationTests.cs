using System.Data;
using _CarRental.Domain.Entities;
using _CarRental.Domain.Enums;
using _CarRental.Domain.Exceptions;
using _CarRental.Domain.Interfaces.Repositories;
using _CarRental.Domain.Services;
using _CarRental.Infrastructure;
using Microsoft.Data.SqlClient;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Xunit;

namespace _CarRental.Tests;

public sealed class RentalContractIntegrationTests
{
    [SqlIntegrationFact]
    public async Task ReservationConvertsOnlyOnceAndReturnUpdatesLifecycleRecords()
    {
        var connectionString = Environment.GetEnvironmentVariable("CAR_RENTAL_TEST_CONNECTION_STRING")!;
        var fixture = new Fixture();
        try
        {
            await using var provider = CreateServiceProvider(connectionString);
            int contractId;
            await using (var scope = provider.CreateAsyncScope())
            {
                var unitOfWork = scope.ServiceProvider.GetRequiredService<IUnitOfWork>();
                fixture.BranchId = await unitOfWork.Branches.CreateAsync(new Branch
                {
                    Name = "T013 " + fixture.Suffix, Address = "Contract integration", City = "Amman"
                });
                fixture.CategoryId = await unitOfWork.Lookups.CreateAsync(VehicleLookupType.Category, "T013 " + fixture.Suffix, null);
                fixture.FuelTypeId = await unitOfWork.Lookups.CreateAsync(VehicleLookupType.FuelType, "T013 " + fixture.Suffix, null);
                fixture.TransmissionTypeId = await unitOfWork.Lookups.CreateAsync(VehicleLookupType.TransmissionType, "T013 " + fixture.Suffix, null);
                var customerUser = await unitOfWork.Users.CreateAsync(NewUser("customer", fixture));
                fixture.UserIds.Add(customerUser);
                fixture.CustomerId = await unitOfWork.Customers.CreateAsync(new Customer
                {
                    UserId = customerUser, FullName = "T013 Integration Customer",
                    DrivingLicenseNumber = "T013-" + fixture.Suffix,
                    DrivingLicenseExpiry = DateTime.UtcNow.Date.AddYears(1)
                });
                var employeeUser = await unitOfWork.Users.CreateAsync(NewUser("employee", fixture));
                fixture.UserIds.Add(employeeUser);
                fixture.EmployeeId = await unitOfWork.Employees.CreateAsync(new Employee
                {
                    UserId = employeeUser, BranchId = fixture.BranchId, Position = "T013 Integration",
                    HireDate = DateTime.UtcNow.Date
                });
                fixture.VehicleId = await unitOfWork.Vehicles.CreateAsync(new Vehicle
                {
                    PlateNumber = "T013" + fixture.Suffix[..16].ToUpperInvariant(), Make = "Test", Model = "Contract",
                    Year = 2025, CategoryId = fixture.CategoryId, FuelTypeId = fixture.FuelTypeId,
                    TransmissionTypeId = fixture.TransmissionTypeId, SeatsCount = 5, DailyPrice = 40m,
                    BranchId = fixture.BranchId, CurrentMileage = 100
                });
                var pickup = DateTime.UtcNow.Date.AddDays(2).AddHours(10);
                fixture.ReservationId = await unitOfWork.Reservations.CreateAsync(new Reservation
                {
                    CustomerId = fixture.CustomerId, VehicleId = fixture.VehicleId,
                    PickupBranchId = fixture.BranchId, ReturnBranchId = fixture.BranchId,
                    PickupDateTime = pickup, ExpectedReturnDateTime = pickup.AddDays(2)
                });
                await unitOfWork.Reservations.UpdateStatusAsync(fixture.ReservationId, ReservationStatus.Pending,
                    ReservationStatus.Confirmed, fixture.EmployeeId);
                await unitOfWork.Vehicles.SetStatusAsync(fixture.VehicleId, VehicleStatus.Reserved);

                var contract = RentalContract.Create(fixture.ReservationId, fixture.CustomerId, fixture.VehicleId,
                    fixture.EmployeeId, DateTime.UtcNow, 100, FuelLevel.Full, null, 2, 80m, 0m, 0m);
                await unitOfWork.BeginTransactionAsync();
                contractId = await unitOfWork.Contracts.CreateFromReservationAsync(contract);
                await unitOfWork.CommitAsync();

                await unitOfWork.BeginTransactionAsync();
                var conflict = await Assert.ThrowsAsync<ConflictException>(() => unitOfWork.Contracts.CreateFromReservationAsync(contract));
                Assert.Equal("RESERVATION_NOT_CONFIRMED", conflict.Code);
                await unitOfWork.RollbackAsync();
                Assert.Equal(VehicleStatus.Rented.ToString(), (await unitOfWork.Vehicles.GetByIdAsync(fixture.VehicleId))!.Status);
            }

            await using var verifyScope = provider.CreateAsyncScope();
            var verifyUnitOfWork = verifyScope.ServiceProvider.GetRequiredService<IUnitOfWork>();
            var savedContract = await verifyUnitOfWork.Contracts.GetByIdAsync(contractId);
            Assert.NotNull(savedContract);
            Assert.Equal(fixture.ReservationId, savedContract.ReservationId);

            var returnAt = savedContract.ActualPickupDate.AddDays(2).AddHours(1);
            var finalDays = RentalPriceCalculator.CalculateDays(savedContract.ActualPickupDate, returnAt);
            var finalBasic = RentalPriceCalculator.CalculateFinalBasic(savedContract.BasicRentalPrice, savedContract.RentalDays, finalDays);
            savedContract.Complete(fixture.EmployeeId, returnAt, 110, FuelLevel.Half, "Returned in test",
                finalDays, finalBasic, fuelCharges: 5m, damageCharges: 3m);
            await verifyUnitOfWork.BeginTransactionAsync();
            await verifyUnitOfWork.Contracts.CompleteAsync(savedContract, fixture.BranchId, requiresMaintenance: false);
            await verifyUnitOfWork.CommitAsync();

            var returnedContract = await verifyUnitOfWork.Contracts.GetByIdAsync(contractId);
            var returnedVehicle = await verifyUnitOfWork.Vehicles.GetByIdAsync(fixture.VehicleId);
            var completedReservation = await verifyUnitOfWork.Reservations.GetByIdAsync(fixture.ReservationId);
            Assert.Equal(ContractStatus.Completed, returnedContract?.Status);
            Assert.Equal(128m, returnedContract?.TotalAmount);
            Assert.Equal(VehicleStatus.Available.ToString(), returnedVehicle?.Status);
            Assert.Equal(fixture.BranchId, returnedVehicle?.BranchId);
            Assert.Equal(110, returnedVehicle?.CurrentMileage);
            Assert.Equal(ReservationStatus.Completed, completedReservation?.Status);

            async Task<bool> TryCreateConcurrentPaymentAsync()
            {
                await using var scope = provider.CreateAsyncScope();
                var unitOfWork = scope.ServiceProvider.GetRequiredService<IUnitOfWork>();
                await unitOfWork.BeginTransactionAsync();
                try
                {
                    await unitOfWork.Payments.CreateAsync(Payment.Create(contractId, 70m, PaymentMethod.Cash, null));
                    await unitOfWork.CommitAsync();
                    return true;
                }
                catch (ConflictException exception) when (exception.Code == "OVERPAYMENT_NOT_ALLOWED")
                {
                    await unitOfWork.RollbackAsync();
                    return false;
                }
                catch
                {
                    await unitOfWork.RollbackAsync();
                    throw;
                }
            }

            var paymentAttempts = await Task.WhenAll(TryCreateConcurrentPaymentAsync(), TryCreateConcurrentPaymentAsync());
            Assert.Single(paymentAttempts, succeeded => succeeded);
            Assert.Single(paymentAttempts, succeeded => !succeeded);
            var payment = (await verifyUnitOfWork.Payments.ListByContractAsync(contractId)).Single();
            await verifyUnitOfWork.BeginTransactionAsync();
            await verifyUnitOfWork.Payments.UpdateStatusAsync(payment.PaymentId, PaymentStatus.Paid.ToString());
            await verifyUnitOfWork.CommitAsync();
            var paidBalance = await verifyUnitOfWork.Payments.GetBalanceAsync(contractId);
            Assert.Equal(70m, paidBalance.PaidAmount);
            Assert.Equal(58m, paidBalance.Balance);

            await verifyUnitOfWork.BeginTransactionAsync();
            await verifyUnitOfWork.Payments.UpdateStatusAsync(payment.PaymentId, PaymentStatus.Refunded.ToString());
            await verifyUnitOfWork.CommitAsync();
            var refundedBalance = await verifyUnitOfWork.Payments.GetBalanceAsync(contractId);
            Assert.Equal(70m, refundedBalance.RefundedAmount);
            Assert.Equal(128m, refundedBalance.Balance);
        }
        finally
        {
            await CleanupAsync(connectionString, fixture);
        }
    }

    private static User NewUser(string kind, Fixture fixture) => new()
    {
        Username = $"t013_{kind}_{fixture.Suffix[..12]}",
        Email = $"t013_{kind}_{fixture.Suffix}@integration.test",
        PasswordHash = "integration-test-hash"
    };

    private static ServiceProvider CreateServiceProvider(string connectionString)
    {
        var configuration = new ConfigurationBuilder().AddInMemoryCollection(new Dictionary<string, string?>
        {
            ["ConnectionStrings:CarRentalDb"] = connectionString,
            ["Jwt:Issuer"] = "CarRentalSystem.Tests",
            ["Jwt:Audience"] = "CarRentalSystem.Tests.Client",
            ["Jwt:SigningKey"] = "test-only-signing-key-with-32-bytes-or-more"
        }).Build();
        var services = new ServiceCollection();
        services.AddLogging();
        services.AddInfrastructure(configuration);
        return services.BuildServiceProvider();
    }

    private static async Task CleanupAsync(string connectionString, Fixture fixture)
    {
        if (string.IsNullOrWhiteSpace(connectionString)) return;
        await using var connection = new SqlConnection(connectionString);
        await connection.OpenAsync();
        await using var command = connection.CreateCommand();
        command.CommandText = """
            DELETE p FROM dbo.Payments p INNER JOIN dbo.RentalContracts c ON c.ContractId = p.ContractId WHERE c.ReservationId = @ReservationId;
            DELETE FROM dbo.RentalContracts WHERE ReservationId = @ReservationId;
            DELETE FROM dbo.Reservations WHERE ReservationId = @ReservationId;
            DELETE FROM dbo.Vehicles WHERE VehicleId = @VehicleId;
            DELETE FROM dbo.Customers WHERE CustomerId = @CustomerId;
            DELETE FROM dbo.Employees WHERE EmployeeId = @EmployeeId;
            DELETE FROM dbo.Users WHERE UserId IN (@CustomerUserId, @EmployeeUserId);
            DELETE FROM dbo.Branches WHERE BranchId = @BranchId;
            DELETE FROM dbo.VehicleCategories WHERE CategoryId = @CategoryId;
            DELETE FROM dbo.FuelTypes WHERE FuelTypeId = @FuelTypeId;
            DELETE FROM dbo.TransmissionTypes WHERE TransmissionTypeId = @TransmissionTypeId;
            """;
        command.Parameters.Add("@ReservationId", SqlDbType.Int).Value = fixture.ReservationId;
        command.Parameters.Add("@VehicleId", SqlDbType.Int).Value = fixture.VehicleId;
        command.Parameters.Add("@CustomerId", SqlDbType.Int).Value = fixture.CustomerId;
        command.Parameters.Add("@EmployeeId", SqlDbType.Int).Value = fixture.EmployeeId;
        command.Parameters.Add("@CustomerUserId", SqlDbType.Int).Value = fixture.UserIds.ElementAtOrDefault(0);
        command.Parameters.Add("@EmployeeUserId", SqlDbType.Int).Value = fixture.UserIds.ElementAtOrDefault(1);
        command.Parameters.Add("@BranchId", SqlDbType.Int).Value = fixture.BranchId;
        command.Parameters.Add("@CategoryId", SqlDbType.Int).Value = fixture.CategoryId;
        command.Parameters.Add("@FuelTypeId", SqlDbType.Int).Value = fixture.FuelTypeId;
        command.Parameters.Add("@TransmissionTypeId", SqlDbType.Int).Value = fixture.TransmissionTypeId;
        await command.ExecuteNonQueryAsync();
    }

    private sealed class Fixture
    {
        public string Suffix { get; } = Guid.NewGuid().ToString("N");
        public int BranchId { get; set; }
        public int CategoryId { get; set; }
        public int FuelTypeId { get; set; }
        public int TransmissionTypeId { get; set; }
        public int CustomerId { get; set; }
        public int EmployeeId { get; set; }
        public int VehicleId { get; set; }
        public int ReservationId { get; set; }
        public List<int> UserIds { get; } = [];
    }
}
