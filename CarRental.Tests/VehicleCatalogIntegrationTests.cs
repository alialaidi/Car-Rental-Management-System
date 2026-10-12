using System.Data;
using _CarRental.Application.Common.Interfaces;
using _CarRental.Domain.Entities;
using _CarRental.Domain.Enums;
using _CarRental.Domain.Interfaces.Repositories;
using _CarRental.Domain.ValueObjects;
using _CarRental.Infrastructure;
using Microsoft.Data.SqlClient;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Xunit;

namespace _CarRental.Tests;

public sealed class VehicleCatalogIntegrationTests
{
    [SqlIntegrationFact]
    public async Task CatalogSearchAndAvailabilityShareReservationContractAndMaintenanceRules()
    {
        var ids = new FixtureIds();
        var connectionString = Environment.GetEnvironmentVariable("CAR_RENTAL_TEST_CONNECTION_STRING")!;
        try
        {
            await using var provider = CreateServiceProvider(connectionString);
            await using (var scope = provider.CreateAsyncScope())
            {
                var unitOfWork = scope.ServiceProvider.GetRequiredService<IUnitOfWork>();
                var suffix = Guid.NewGuid().ToString("N");
                ids.BranchId = await unitOfWork.Branches.CreateAsync(new Branch
                {
                    Name = "T010 " + suffix,
                    Address = "Integration test",
                    City = "Amman"
                });
                ids.OtherBranchId = await unitOfWork.Branches.CreateAsync(new Branch
                {
                    Name = "T010 Other " + suffix,
                    Address = "Integration test",
                    City = "Zarqa"
                });
                ids.CategoryId = await unitOfWork.Lookups.CreateAsync(VehicleLookupType.Category, "T010 " + suffix, null);
                ids.OtherCategoryId = await unitOfWork.Lookups.CreateAsync(VehicleLookupType.Category, "T010 Other " + suffix, null);
                ids.FuelTypeId = await unitOfWork.Lookups.CreateAsync(VehicleLookupType.FuelType, "T010 " + suffix, null);
                ids.OtherFuelTypeId = await unitOfWork.Lookups.CreateAsync(VehicleLookupType.FuelType, "T010 Other " + suffix, null);
                ids.TransmissionTypeId = await unitOfWork.Lookups.CreateAsync(VehicleLookupType.TransmissionType, "T010 " + suffix, null);
                ids.OtherTransmissionTypeId = await unitOfWork.Lookups.CreateAsync(VehicleLookupType.TransmissionType, "T010 Other " + suffix, null);

                ids.CustomerUserId = await unitOfWork.Users.CreateAsync(NewUser("customer", suffix));
                ids.CustomerId = await unitOfWork.Customers.CreateAsync(new Customer
                {
                    UserId = ids.CustomerUserId,
                    FullName = "T010 Integration Customer",
                    DrivingLicenseNumber = "T010-" + suffix,
                    DrivingLicenseExpiry = DateTime.UtcNow.Date.AddYears(1)
                });
                ids.EmployeeUserId = await unitOfWork.Users.CreateAsync(NewUser("employee", suffix));
                ids.EmployeeId = await unitOfWork.Employees.CreateAsync(new Employee
                {
                    UserId = ids.EmployeeUserId,
                    BranchId = ids.BranchId,
                    Position = "T010 Test Employee",
                    HireDate = DateTime.UtcNow.Date.AddYears(-1)
                });
                ids.VehicleId = await unitOfWork.Vehicles.CreateAsync(new Vehicle
                {
                    PlateNumber = "T010" + suffix[..16].ToUpperInvariant(),
                    Make = "Test",
                    Model = "Catalog",
                    Year = 2025,
                    CategoryId = ids.CategoryId,
                    FuelTypeId = ids.FuelTypeId,
                    TransmissionTypeId = ids.TransmissionTypeId,
                    SeatsCount = 5,
                    DailyPrice = 40,
                    BranchId = ids.BranchId,
                    CurrentMileage = 100
                });
                ids.VehicleIds.Add(ids.VehicleId);
                await unitOfWork.VehicleImages.AddAsync(new VehicleImage { VehicleId = ids.VehicleId, ImageUrl = "https://integration.test/secondary.jpg" });
                await unitOfWork.VehicleImages.AddAsync(new VehicleImage { VehicleId = ids.VehicleId, ImageUrl = "https://integration.test/primary.jpg", IsPrimary = true });

                ids.VehicleIds.Add(await unitOfWork.Vehicles.CreateAsync(NewVehicle(ids, "category", categoryId: ids.OtherCategoryId)));
                ids.VehicleIds.Add(await unitOfWork.Vehicles.CreateAsync(NewVehicle(ids, "fuel", fuelTypeId: ids.OtherFuelTypeId)));
                ids.VehicleIds.Add(await unitOfWork.Vehicles.CreateAsync(NewVehicle(ids, "transmission", transmissionTypeId: ids.OtherTransmissionTypeId)));
                ids.VehicleIds.Add(await unitOfWork.Vehicles.CreateAsync(NewVehicle(ids, "branch", branchId: ids.OtherBranchId)));
                ids.VehicleIds.Add(await unitOfWork.Vehicles.CreateAsync(NewVehicle(ids, "price", dailyPrice: 60)));
                ids.VehicleIds.Add(await unitOfWork.Vehicles.CreateAsync(NewVehicle(ids, "seats", seatsCount: 2)));

                var allVehicles = new VehicleSearchCriteria(null, null, null, null, null, null, null,
                    null, null, 1, 100, "price", "asc");
                var allResults = await unitOfWork.Vehicles.SearchAvailableAsync(allVehicles);
                Assert.Equal(7, allResults.TotalCount);
                Assert.Equal(ids.VehicleId, allResults.Items[0].Vehicle.VehicleId);
                var secondPage = await unitOfWork.Vehicles.SearchAvailableAsync(allVehicles with { Page = 2, PageSize = 1 });
                Assert.Equal(7, secondPage.TotalCount);
                Assert.Single(secondPage.Items);
                Assert.Equal(6, (await unitOfWork.Vehicles.SearchAvailableAsync(allVehicles with { CategoryId = ids.CategoryId })).TotalCount);
                Assert.Equal(6, (await unitOfWork.Vehicles.SearchAvailableAsync(allVehicles with { FuelTypeId = ids.FuelTypeId })).TotalCount);
                Assert.Equal(6, (await unitOfWork.Vehicles.SearchAvailableAsync(allVehicles with { TransmissionTypeId = ids.TransmissionTypeId })).TotalCount);
                Assert.Equal(6, (await unitOfWork.Vehicles.SearchAvailableAsync(allVehicles with { BranchId = ids.BranchId })).TotalCount);
                Assert.Equal(1, (await unitOfWork.Vehicles.SearchAvailableAsync(allVehicles with { MinimumDailyPrice = 45 })).TotalCount);
                Assert.Equal(6, (await unitOfWork.Vehicles.SearchAvailableAsync(allVehicles with { MaximumDailyPrice = 50 })).TotalCount);
                Assert.Equal(6, (await unitOfWork.Vehicles.SearchAvailableAsync(allVehicles with { MinimumSeats = 5 })).TotalCount);

                var window = new VehicleSearchCriteria(ids.CategoryId, ids.FuelTypeId, ids.TransmissionTypeId, ids.BranchId, 30, 50, 4,
                    Utc(2026, 11, 10, 10), Utc(2026, 11, 12, 10), 1, 100, "price", "asc");
                var initialPage = await unitOfWork.Vehicles.SearchAvailableAsync(window);
                Assert.Equal(1, initialPage.TotalCount);
                Assert.Equal("https://integration.test/primary.jpg", Assert.Single(initialPage.Items).PrimaryImageUrl);

                var details = await unitOfWork.Vehicles.GetCatalogDetailsAsync(ids.VehicleId);
                Assert.NotNull(details);
                Assert.Equal("T010 " + suffix, details.CatalogItem.CategoryName);
                Assert.Equal("https://integration.test/primary.jpg", details.Images[0].ImageUrl);

                await InsertReservationAsync(connectionString, ids, Utc(2026, 11, 11, 11), Utc(2026, 11, 12, 10));
                Assert.False(await unitOfWork.Vehicles.IsAvailableAsync(ids.VehicleId, Utc(2026, 11, 10, 10), Utc(2026, 11, 12, 10)));
                Assert.Empty((await unitOfWork.Vehicles.SearchAvailableAsync(window)).Items);
                Assert.True(await unitOfWork.Vehicles.IsAvailableAsync(ids.VehicleId, Utc(2026, 11, 12, 10), Utc(2026, 11, 14, 10)));
                await ExecuteAsync(connectionString, "DELETE FROM dbo.Reservations WHERE VehicleId = @VehicleId", ids);

                await InsertActiveContractAsync(connectionString, ids);
                Assert.False(await unitOfWork.Vehicles.IsAvailableAsync(ids.VehicleId, Utc(2026, 11, 10, 10), Utc(2026, 11, 12, 10)));
                await ExecuteAsync(connectionString, "DELETE FROM dbo.RentalContracts WHERE VehicleId = @VehicleId", ids);

                await InsertMaintenanceAsync(connectionString, ids, new DateTime(2026, 11, 11), new DateTime(2026, 11, 12));
                Assert.False(await unitOfWork.Vehicles.IsAvailableAsync(ids.VehicleId, Utc(2026, 11, 10, 10), Utc(2026, 11, 12, 10)));
                Assert.Empty((await unitOfWork.Vehicles.SearchAvailableAsync(window with
                {
                    PickupDateTimeUtc = null,
                    ExpectedReturnDateTimeUtc = null
                })).Items);
                Assert.True(await unitOfWork.Vehicles.IsAvailableAsync(ids.VehicleId, Utc(2026, 11, 14, 10), Utc(2026, 11, 15, 10)));
                await ExecuteAsync(connectionString, "DELETE FROM dbo.MaintenanceRecords WHERE VehicleId = @VehicleId", ids);

                await ExecuteAsync(connectionString, "UPDATE dbo.Vehicles SET Status = N'Maintenance' WHERE VehicleId = @VehicleId", ids);
                Assert.False(await unitOfWork.Vehicles.IsAvailableAsync(ids.VehicleId, Utc(2026, 11, 14, 10), Utc(2026, 11, 15, 10)));
            }
        }
        finally
        {
            await CleanupAsync(connectionString, ids);
        }
    }

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

    private static User NewUser(string kind, string suffix) => new()
    {
        Username = $"t010_{kind}_{suffix[..12]}",
        Email = $"t010_{kind}_{suffix}@integration.test",
        PasswordHash = "integration-test-hash"
    };

    private static Vehicle NewVehicle(FixtureIds ids, string model, int? categoryId = null, int? fuelTypeId = null,
        int? transmissionTypeId = null, int? branchId = null, decimal dailyPrice = 40, int seatsCount = 5) => new()
    {
        PlateNumber = "T010" + Guid.NewGuid().ToString("N")[..16].ToUpperInvariant(),
        Make = "Test",
        Model = model,
        Year = 2025,
        CategoryId = categoryId ?? ids.CategoryId,
        FuelTypeId = fuelTypeId ?? ids.FuelTypeId,
        TransmissionTypeId = transmissionTypeId ?? ids.TransmissionTypeId,
        SeatsCount = seatsCount,
        DailyPrice = dailyPrice,
        BranchId = branchId ?? ids.BranchId,
        CurrentMileage = 100
    };

    private static async Task InsertReservationAsync(string connectionString, FixtureIds ids, DateTime pickup, DateTime expectedReturn)
    {
        const string sql = "INSERT dbo.Reservations (CustomerId, VehicleId, PickupBranchId, ReturnBranchId, PickupDateTime, ExpectedReturnDateTime, Status) VALUES (@CustomerId, @VehicleId, @BranchId, @BranchId, @Pickup, @Return, N'Pending');";
        await ExecuteAsync(connectionString, sql, ids, command =>
        {
            command.Parameters.Add("@CustomerId", SqlDbType.Int).Value = ids.CustomerId;
            command.Parameters.Add("@Pickup", SqlDbType.DateTime2).Value = pickup;
            command.Parameters.Add("@Return", SqlDbType.DateTime2).Value = expectedReturn;
        });
    }

    private static async Task InsertActiveContractAsync(string connectionString, FixtureIds ids)
    {
        const string sql = "INSERT dbo.RentalContracts (ContractNumber, CustomerId, VehicleId, EmployeeId, ActualPickupDate, PickupMileage, PickupFuelLevel, RentalDays, BasicRentalPrice) VALUES (@ContractNumber, @CustomerId, @VehicleId, @EmployeeId, @Pickup, 100, N'Full', 2, 80);";
        await ExecuteAsync(connectionString, sql, ids, command =>
        {
            command.Parameters.Add("@ContractNumber", SqlDbType.NVarChar, 50).Value = "T010-" + Guid.NewGuid().ToString("N");
            command.Parameters.Add("@CustomerId", SqlDbType.Int).Value = ids.CustomerId;
            command.Parameters.Add("@EmployeeId", SqlDbType.Int).Value = ids.EmployeeId;
            command.Parameters.Add("@Pickup", SqlDbType.DateTime2).Value = Utc(2026, 11, 11, 10);
        });
    }

    private static async Task InsertMaintenanceAsync(string connectionString, FixtureIds ids, DateTime start, DateTime expectedEnd)
    {
        const string sql = "INSERT dbo.MaintenanceRecords (VehicleId, CreatedByEmployeeId, MaintenanceType, StartDate, ExpectedEndDate, Status) VALUES (@VehicleId, @EmployeeId, N'T010 Test', @StartDate, @ExpectedEndDate, N'InProgress');";
        await ExecuteAsync(connectionString, sql, ids, command =>
        {
            command.Parameters.Add("@EmployeeId", SqlDbType.Int).Value = ids.EmployeeId;
            command.Parameters.Add("@StartDate", SqlDbType.Date).Value = start;
            command.Parameters.Add("@ExpectedEndDate", SqlDbType.Date).Value = expectedEnd;
        });
    }

    private static async Task ExecuteAsync(string connectionString, string sql, FixtureIds ids, Action<SqlCommand>? configure = null)
    {
        await using var connection = new SqlConnection(connectionString);
        await connection.OpenAsync();
        await using var command = connection.CreateCommand();
        command.CommandText = sql;
        command.Parameters.Add("@VehicleId", SqlDbType.Int).Value = ids.VehicleId;
        command.Parameters.Add("@BranchId", SqlDbType.Int).Value = ids.BranchId;
        configure?.Invoke(command);
        await command.ExecuteNonQueryAsync();
    }

    private static async Task CleanupAsync(string connectionString, FixtureIds ids)
    {
        if (string.IsNullOrWhiteSpace(connectionString))
            return;
        const string vehicleSql = """
            DELETE FROM dbo.RentalContracts WHERE VehicleId = @VehicleId;
            DELETE FROM dbo.MaintenanceRecords WHERE VehicleId = @VehicleId;
            DELETE FROM dbo.Reservations WHERE VehicleId = @VehicleId;
            DELETE FROM dbo.VehicleImages WHERE VehicleId = @VehicleId;
            DELETE FROM dbo.Vehicles WHERE VehicleId = @VehicleId;
            """;
        foreach (var vehicleId in ids.VehicleIds.DefaultIfEmpty(ids.VehicleId).Distinct())
            await ExecuteAsync(connectionString, vehicleSql, ids, command => command.Parameters["@VehicleId"].Value = vehicleId);

        const string relatedRowsSql = """
            DELETE FROM dbo.Users WHERE UserId IN (@CustomerUserId, @EmployeeUserId);
            DELETE FROM dbo.Branches WHERE BranchId IN (@BranchId, @OtherBranchId);
            DELETE FROM dbo.VehicleCategories WHERE CategoryId IN (@CategoryId, @OtherCategoryId);
            DELETE FROM dbo.FuelTypes WHERE FuelTypeId IN (@FuelTypeId, @OtherFuelTypeId);
            DELETE FROM dbo.TransmissionTypes WHERE TransmissionTypeId IN (@TransmissionTypeId, @OtherTransmissionTypeId);
            """;
        await ExecuteAsync(connectionString, relatedRowsSql, ids, command =>
        {
            command.Parameters.Add("@CustomerUserId", SqlDbType.Int).Value = ids.CustomerUserId;
            command.Parameters.Add("@EmployeeUserId", SqlDbType.Int).Value = ids.EmployeeUserId;
            command.Parameters.Add("@OtherBranchId", SqlDbType.Int).Value = ids.OtherBranchId;
            command.Parameters.Add("@CategoryId", SqlDbType.Int).Value = ids.CategoryId;
            command.Parameters.Add("@OtherCategoryId", SqlDbType.Int).Value = ids.OtherCategoryId;
            command.Parameters.Add("@FuelTypeId", SqlDbType.Int).Value = ids.FuelTypeId;
            command.Parameters.Add("@OtherFuelTypeId", SqlDbType.Int).Value = ids.OtherFuelTypeId;
            command.Parameters.Add("@TransmissionTypeId", SqlDbType.Int).Value = ids.TransmissionTypeId;
            command.Parameters.Add("@OtherTransmissionTypeId", SqlDbType.Int).Value = ids.OtherTransmissionTypeId;
        });
    }

    private static DateTime Utc(int year, int month, int day, int hour) =>
        new(year, month, day, hour, 0, 0, DateTimeKind.Utc);

    private sealed class FixtureIds
    {
        public int BranchId { get; set; }
        public int OtherBranchId { get; set; }
        public int CategoryId { get; set; }
        public int OtherCategoryId { get; set; }
        public int FuelTypeId { get; set; }
        public int OtherFuelTypeId { get; set; }
        public int TransmissionTypeId { get; set; }
        public int OtherTransmissionTypeId { get; set; }
        public int CustomerUserId { get; set; }
        public int CustomerId { get; set; }
        public int EmployeeUserId { get; set; }
        public int EmployeeId { get; set; }
        public int VehicleId { get; set; }
        public List<int> VehicleIds { get; } = [];
    }
}
