using System.Data;
using _CarRental.Domain.Entities;
using _CarRental.Domain.Enums;
using _CarRental.Domain.Exceptions;
using _CarRental.Domain.Interfaces.Repositories;
using _CarRental.Domain.ValueObjects;
using _CarRental.Infrastructure;
using Microsoft.Data.SqlClient;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Xunit;

namespace _CarRental.Tests;

public sealed class ReservationIntegrationTests
{
    [SqlIntegrationFact]
    public async Task ConcurrentReservationCreatesForOneVehicleAllowExactlyOneRequest()
    {
        var connectionString = Environment.GetEnvironmentVariable("CAR_RENTAL_TEST_CONNECTION_STRING")!;
        var fixture = new Fixture();
        try
        {
            await using var provider = CreateServiceProvider(connectionString);
            await using (var scope = provider.CreateAsyncScope())
            {
                var unitOfWork = scope.ServiceProvider.GetRequiredService<IUnitOfWork>();
                fixture.BranchId = await unitOfWork.Branches.CreateAsync(new Branch
                {
                    Name = "T011 " + fixture.Suffix,
                    Address = "Reservation integration",
                    City = "Amman"
                });
                fixture.CategoryId = await unitOfWork.Lookups.CreateAsync(VehicleLookupType.Category, "T011 " + fixture.Suffix, null);
                fixture.FuelTypeId = await unitOfWork.Lookups.CreateAsync(VehicleLookupType.FuelType, "T011 " + fixture.Suffix, null);
                fixture.TransmissionTypeId = await unitOfWork.Lookups.CreateAsync(VehicleLookupType.TransmissionType, "T011 " + fixture.Suffix, null);
                fixture.VehicleId = await unitOfWork.Vehicles.CreateAsync(new Vehicle
                {
                    PlateNumber = "T011" + fixture.Suffix[..16].ToUpperInvariant(),
                    Make = "Test",
                    Model = "Concurrent reservation",
                    Year = 2025,
                    CategoryId = fixture.CategoryId,
                    FuelTypeId = fixture.FuelTypeId,
                    TransmissionTypeId = fixture.TransmissionTypeId,
                    SeatsCount = 5,
                    DailyPrice = 40,
                    BranchId = fixture.BranchId,
                    CurrentMileage = 100
                });

                fixture.CustomerIds.Add(await CreateCustomerAsync(unitOfWork, "first", fixture));
                fixture.CustomerIds.Add(await CreateCustomerAsync(unitOfWork, "second", fixture));
            }

            var pickup = DateTime.UtcNow.Date.AddDays(30).AddHours(10);
            var expectedReturn = pickup.AddDays(2);
            var results = await Task.WhenAll(
                TryCreateAsync(provider, fixture.CustomerIds[0], fixture, pickup, expectedReturn),
                TryCreateAsync(provider, fixture.CustomerIds[1], fixture, pickup, expectedReturn));

            Assert.Single(results, result => result.ReservationId.HasValue);
            Assert.Single(results, result => result.Conflict);

            await using var releaseScope = provider.CreateAsyncScope();
            var releaseUnitOfWork = releaseScope.ServiceProvider.GetRequiredService<IUnitOfWork>();
            var firstReservationId = await CreateReservationAsync(releaseUnitOfWork, fixture.CustomerIds[0], fixture,
                DateTime.UtcNow.Date.AddDays(40).AddHours(10));
            var secondReservationId = await CreateReservationAsync(releaseUnitOfWork, fixture.CustomerIds[1], fixture,
                DateTime.UtcNow.Date.AddDays(45).AddHours(10));

            await releaseUnitOfWork.Reservations.UpdateStatusAsync(firstReservationId, ReservationStatus.Pending, ReservationStatus.Confirmed);
            await releaseUnitOfWork.Vehicles.SetStatusAsync(fixture.VehicleId, VehicleStatus.Reserved);
            await releaseUnitOfWork.Reservations.UpdateStatusAsync(secondReservationId, ReservationStatus.Pending, ReservationStatus.Confirmed);
            await releaseUnitOfWork.Vehicles.SetStatusAsync(fixture.VehicleId, VehicleStatus.Reserved);

            await releaseUnitOfWork.Reservations.UpdateStatusAsync(firstReservationId, ReservationStatus.Confirmed, ReservationStatus.Cancelled);
            Assert.True(await releaseUnitOfWork.Vehicles.HasOtherConfirmedReservationAsync(fixture.VehicleId, firstReservationId));
            Assert.Equal(VehicleStatus.Reserved.ToString(), (await releaseUnitOfWork.Vehicles.GetByIdAsync(fixture.VehicleId))!.Status);

            await releaseUnitOfWork.Reservations.UpdateStatusAsync(secondReservationId, ReservationStatus.Confirmed, ReservationStatus.Cancelled);
            Assert.False(await releaseUnitOfWork.Vehicles.HasOtherConfirmedReservationAsync(fixture.VehicleId, secondReservationId));
            await releaseUnitOfWork.Vehicles.SetStatusAsync(fixture.VehicleId, VehicleStatus.Available);
            Assert.Equal(VehicleStatus.Available.ToString(), (await releaseUnitOfWork.Vehicles.GetByIdAsync(fixture.VehicleId))!.Status);
        }
        finally
        {
            await CleanupAsync(connectionString, fixture);
        }
    }

    private static async Task<int> CreateCustomerAsync(IUnitOfWork unitOfWork, string kind, Fixture fixture)
    {
        var userId = await unitOfWork.Users.CreateAsync(new User
        {
            Username = $"t011_{kind}_{fixture.Suffix[..12]}",
            Email = $"t011_{kind}_{fixture.Suffix}@integration.test",
            PasswordHash = "integration-test-hash"
        });
        return await unitOfWork.Customers.CreateAsync(new Customer
        {
            UserId = userId,
            FullName = "T011 Integration Customer",
            DrivingLicenseNumber = $"T011-{kind}-{fixture.Suffix}",
            DrivingLicenseExpiry = DateTime.UtcNow.Date.AddYears(1)
        });
    }

    private static async Task<CreateResult> TryCreateAsync(
        IServiceProvider provider, int customerId, Fixture fixture, DateTime pickup, DateTime expectedReturn)
    {
        await using var scope = provider.CreateAsyncScope();
        var unitOfWork = scope.ServiceProvider.GetRequiredService<IUnitOfWork>();
        try
        {
            var reservationId = await unitOfWork.Reservations.CreateAsync(new Reservation
            {
                CustomerId = customerId,
                VehicleId = fixture.VehicleId,
                PickupBranchId = fixture.BranchId,
                ReturnBranchId = fixture.BranchId,
                PickupDateTime = pickup,
                ExpectedReturnDateTime = expectedReturn
            });
            return new CreateResult(reservationId, false);
        }
        catch (ConflictException)
        {
            return new CreateResult(null, true);
        }
    }

    private static Task<int> CreateReservationAsync(IUnitOfWork unitOfWork, int customerId, Fixture fixture, DateTime pickup) =>
        unitOfWork.Reservations.CreateAsync(new Reservation
        {
            CustomerId = customerId,
            VehicleId = fixture.VehicleId,
            PickupBranchId = fixture.BranchId,
            ReturnBranchId = fixture.BranchId,
            PickupDateTime = pickup,
            ExpectedReturnDateTime = pickup.AddDays(2)
        });

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
        if (string.IsNullOrWhiteSpace(connectionString))
            return;

        await using var connection = new SqlConnection(connectionString);
        await connection.OpenAsync();
        await using var command = connection.CreateCommand();
        command.CommandText = """
            DELETE FROM dbo.Reservations WHERE VehicleId = @VehicleId;
            DELETE FROM dbo.Vehicles WHERE VehicleId = @VehicleId;
            DELETE FROM dbo.Users WHERE Username LIKE @UserPrefix;
            DELETE FROM dbo.Branches WHERE BranchId = @BranchId;
            DELETE FROM dbo.VehicleCategories WHERE CategoryId = @CategoryId;
            DELETE FROM dbo.FuelTypes WHERE FuelTypeId = @FuelTypeId;
            DELETE FROM dbo.TransmissionTypes WHERE TransmissionTypeId = @TransmissionTypeId;
            """;
        command.Parameters.Add("@VehicleId", SqlDbType.Int).Value = fixture.VehicleId;
        command.Parameters.Add("@UserPrefix", SqlDbType.NVarChar, 50).Value = $"t011_%_{fixture.Suffix[..12]}";
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
        public int VehicleId { get; set; }
        public List<int> CustomerIds { get; } = [];
    }

    private sealed record CreateResult(int? ReservationId, bool Conflict);
}
