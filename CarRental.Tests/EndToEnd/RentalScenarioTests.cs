using System.Data;
using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using _CarRental.Application.Common.Interfaces;
using _CarRental.Application.DTOs;
using _CarRental.Application.DTOs.Branches;
using _CarRental.Application.DTOs.Contracts;
using _CarRental.Application.DTOs.Employees;
using _CarRental.Application.DTOs.Payments;
using _CarRental.Application.DTOs.Reservations;
using _CarRental.Application.DTOs.Reviews;
using _CarRental.Application.DTOs.Vehicles;
using _CarRental.Application.Common.Models;
using _CarRental.Domain.Constants;
using _CarRental.Domain.Entities;
using _CarRental.Domain.Enums;
using _CarRental.Domain.Interfaces.Repositories;
using _CarRental.Infrastructure.Identity;
using Microsoft.Data.SqlClient;
using Microsoft.Extensions.DependencyInjection;
using Xunit;

namespace _CarRental.Tests.EndToEnd;

public sealed class RentalScenarioTests
{
    [SqlIntegrationFact]
    public async Task CustomerCanCompleteRentalScenarioThroughHttpApi()
    {
        var connectionString = Environment.GetEnvironmentVariable("CAR_RENTAL_TEST_CONNECTION_STRING")!;
        var fixture = new Fixture();
        using var factory = new ApiAuthenticationTests.AuthenticatedApiFactory(authPermitLimit: 100);
        try
        {
            await using (var scope = factory.Services.CreateAsyncScope())
            {
                var unitOfWork = scope.ServiceProvider.GetRequiredService<IUnitOfWork>();
                var hasher = scope.ServiceProvider.GetRequiredService<IPasswordHasher>();
                var adminPassword = hasher.Hash("Admin-test-Password-4821");
                fixture.AdminUserId = await unitOfWork.Users.CreateAsync(new User
                {
                    Username = fixture.AdminUsername, Email = fixture.AdminEmail, PasswordHash = adminPassword
                });
                var adminRoleId = await unitOfWork.Users.GetRoleIdByNameAsync(RoleNames.Admin)
                    ?? throw new InvalidOperationException("Admin role is missing from test database seed.");
                await unitOfWork.Users.AssignRoleAsync(fixture.AdminUserId, adminRoleId);
                fixture.CategoryId = await unitOfWork.Lookups.CreateAsync(VehicleLookupType.Category, fixture.CategoryName, null);
                fixture.FuelTypeId = await unitOfWork.Lookups.CreateAsync(VehicleLookupType.FuelType, fixture.FuelTypeName, null);
                fixture.TransmissionTypeId = await unitOfWork.Lookups.CreateAsync(VehicleLookupType.TransmissionType, fixture.TransmissionName, null);
            }

            var jwtService = factory.Services.GetRequiredService<IJwtService>();
            using var adminClient = factory.CreateClient();
            adminClient.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer",
                jwtService.GenerateAccessToken(fixture.AdminUserId, fixture.AdminUsername, [RoleNames.Admin]).Token);

            var branch = await PostAsync<BranchDto>(adminClient, "/api/branches",
                new CreateBranchRequest { Name = fixture.BranchName, Address = "E2E Test Address", City = "Amman" }, HttpStatusCode.Created);
            fixture.BranchId = branch.BranchId;

            var employee = await PostAsync<EmployeeDto>(adminClient, "/api/employees", new CreateEmployeeRequest
            {
                Username = fixture.EmployeeUsername, Email = fixture.EmployeeEmail,
                Password = "Employee-test-Password-4821", BranchId = fixture.BranchId,
                Position = "Rental Agent", HireDate = DateTime.UtcNow.Date
            }, HttpStatusCode.Created);
            fixture.EmployeeUserId = employee.UserId;

            var vehicle = await PostAsync<VehicleDto>(adminClient, "/api/vehicles", new CreateVehicleRequest
            {
                PlateNumber = fixture.PlateNumber, Make = "Scenario", Model = "TestCar", Year = DateTime.UtcNow.Year,
                CategoryId = fixture.CategoryId, FuelTypeId = fixture.FuelTypeId, TransmissionTypeId = fixture.TransmissionTypeId,
                SeatsCount = 5, DailyPrice = 40m, BranchId = fixture.BranchId, CurrentMileage = 100
            }, HttpStatusCode.Created);
            fixture.VehicleId = vehicle.VehicleId;

            using var customerClient = factory.CreateClient();
            const string customerPassword = "Customer-test-Password-4821";
            _ = await PostAsync<AuthResult>(customerClient, "/api/auth/register", new RegisterRequest
            {
                Username = fixture.CustomerUsername, Email = fixture.CustomerEmail, Password = customerPassword,
                FullName = "Scenario Customer", DrivingLicenseNumber = fixture.LicenseNumber,
                DrivingLicenseExpiry = DateTime.UtcNow.Date.AddYears(2), Address = "Amman"
            }, HttpStatusCode.Created);
            var login = await PostAsync<AuthResult>(customerClient, "/api/auth/login",
                new LoginRequest { UsernameOrEmail = fixture.CustomerUsername, Password = customerPassword }, HttpStatusCode.OK);
            customerClient.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", login.AccessToken);

            var catalog = await customerClient.GetFromJsonAsync<PagedResult<VehicleListItemDto>>(
                $"/api/catalog/vehicles?branchId={fixture.BranchId}");
            Assert.NotNull(catalog);
            Assert.Contains(catalog.Items, item => item.VehicleId == fixture.VehicleId);

            var pickup = DateTimeOffset.UtcNow.AddDays(2);
            var reservation = await PostAsync<ReservationDto>(customerClient, "/api/reservations", new
            {
                vehicleId = fixture.VehicleId, pickupBranchId = fixture.BranchId, returnBranchId = fixture.BranchId,
                pickupDateTime = pickup, expectedReturnDateTime = pickup.AddDays(3)
            }, HttpStatusCode.Created);
            fixture.ReservationId = reservation.ReservationId;

            using var employeeClient = factory.CreateClient();
            employeeClient.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer",
                jwtService.GenerateAccessToken(employee.UserId, employee.Username, [RoleNames.Employee]).Token);
            _ = await PostAsync<ReservationDto>(employeeClient, $"/api/staff/reservations/{fixture.ReservationId}/confirm",
                body: null, HttpStatusCode.OK);
            var contract = await PostAsync<ContractDto>(employeeClient, "/api/contracts", new CreateContractRequest(
                ReservationId: fixture.ReservationId, PickupMileage: 100, PickupFuelLevel: FuelLevel.Full,
                ConditionNotes: "E2E pickup"), HttpStatusCode.Created);
            fixture.ContractId = contract.ContractId;

            var returned = await PostAsync<ContractDto>(employeeClient, $"/api/contracts/{fixture.ContractId}/return",
                new ReturnVehicleRequest(110, FuelLevel.Full, ActualReturnDate: DateTimeOffset.UtcNow.AddDays(1)), HttpStatusCode.OK);
            var payment = await PostAsync<PaymentDto>(employeeClient, $"/api/contracts/{fixture.ContractId}/payments",
                new CreatePaymentRequest(returned.TotalAmount, PaymentMethod.Cash), HttpStatusCode.Created);
            fixture.PaymentId = payment.PaymentId;
            _ = await PostAsync<PaymentDto>(employeeClient, $"/api/payments/{fixture.PaymentId}/status",
                new UpdatePaymentStatusRequest(PaymentStatus.Paid), HttpStatusCode.OK);

            var review = await PostAsync<ReviewDto>(customerClient, $"/api/contracts/{fixture.ContractId}/review",
                new CreateReviewRequest(5, "Complete API rental scenario."), HttpStatusCode.Created);
            fixture.ReviewId = review.ReviewId;
            Assert.Equal(fixture.VehicleId, review.VehicleId);
            Assert.Equal("Completed", returned.Status);
        }
        finally
        {
            await CleanupAsync(connectionString, fixture);
        }
    }

    private static async Task<T> PostAsync<T>(HttpClient client, string path, object? body, HttpStatusCode expected)
    {
        using var response = body is null ? await client.PostAsync(path, null) : await client.PostAsJsonAsync(path, body);
        var content = await response.Content.ReadAsStringAsync();
        Assert.True(response.StatusCode == expected,
            $"POST {path} expected {(int)expected}, received {(int)response.StatusCode}: {content}");
        return System.Text.Json.JsonSerializer.Deserialize<T>(content,
            new System.Text.Json.JsonSerializerOptions(System.Text.Json.JsonSerializerDefaults.Web))!;
    }

    private static async Task CleanupAsync(string connectionString, Fixture fixture)
    {
        await using var connection = new SqlConnection(connectionString);
        await connection.OpenAsync();
        await using var command = connection.CreateCommand();
        command.CommandText = """
            DELETE FROM dbo.AuditLogs WHERE ActorUserId IN
                (SELECT UserId FROM dbo.Users WHERE Username IN (@AdminUsername, @EmployeeUsername, @CustomerUsername));
            DELETE FROM dbo.Reviews WHERE ContractId = @ContractId;
            DELETE FROM dbo.Payments WHERE ContractId = @ContractId;
            DELETE FROM dbo.RentalContracts WHERE ContractId = @ContractId;
            DELETE FROM dbo.Reservations WHERE ReservationId = @ReservationId;
            DELETE FROM dbo.Vehicles WHERE VehicleId = @VehicleId;
            DELETE FROM dbo.Customers WHERE UserId IN (SELECT UserId FROM dbo.Users WHERE Username = @CustomerUsername);
            DELETE FROM dbo.Employees WHERE UserId IN (SELECT UserId FROM dbo.Users WHERE Username = @EmployeeUsername);
            DELETE FROM dbo.RefreshTokens WHERE UserId IN
                (SELECT UserId FROM dbo.Users WHERE Username IN (@AdminUsername, @EmployeeUsername, @CustomerUsername));
            DELETE FROM dbo.UserRoles WHERE UserId IN
                (SELECT UserId FROM dbo.Users WHERE Username IN (@AdminUsername, @EmployeeUsername, @CustomerUsername));
            DELETE FROM dbo.Users WHERE Username IN (@AdminUsername, @EmployeeUsername, @CustomerUsername);
            DELETE FROM dbo.Branches WHERE BranchId = @BranchId;
            DELETE FROM dbo.VehicleCategories WHERE CategoryId = @CategoryId;
            DELETE FROM dbo.FuelTypes WHERE FuelTypeId = @FuelTypeId;
            DELETE FROM dbo.TransmissionTypes WHERE TransmissionTypeId = @TransmissionTypeId;
            """;
        command.Parameters.Add("@AdminUsername", SqlDbType.NVarChar, 50).Value = fixture.AdminUsername;
        command.Parameters.Add("@EmployeeUsername", SqlDbType.NVarChar, 50).Value = fixture.EmployeeUsername;
        command.Parameters.Add("@CustomerUsername", SqlDbType.NVarChar, 50).Value = fixture.CustomerUsername;
        command.Parameters.Add("@ContractId", SqlDbType.Int).Value = fixture.ContractId;
        command.Parameters.Add("@ReservationId", SqlDbType.Int).Value = fixture.ReservationId;
        command.Parameters.Add("@VehicleId", SqlDbType.Int).Value = fixture.VehicleId;
        command.Parameters.Add("@BranchId", SqlDbType.Int).Value = fixture.BranchId;
        command.Parameters.Add("@CategoryId", SqlDbType.Int).Value = fixture.CategoryId;
        command.Parameters.Add("@FuelTypeId", SqlDbType.Int).Value = fixture.FuelTypeId;
        command.Parameters.Add("@TransmissionTypeId", SqlDbType.Int).Value = fixture.TransmissionTypeId;
        await command.ExecuteNonQueryAsync();
    }

    private sealed class Fixture
    {
        private string Suffix { get; } = Guid.NewGuid().ToString("N");
        public string AdminUsername => $"e2e_admin_{Suffix[..16]}";
        public string AdminEmail => $"{AdminUsername}@integration.test";
        public string EmployeeUsername => $"e2e_employee_{Suffix[..16]}";
        public string EmployeeEmail => $"{EmployeeUsername}@integration.test";
        public string CustomerUsername => $"e2e_customer_{Suffix[..16]}";
        public string CustomerEmail => $"{CustomerUsername}@integration.test";
        public string BranchName => $"E2E Branch {Suffix[..12]}";
        public string CategoryName => $"E2E Category {Suffix[..12]}";
        public string FuelTypeName => $"E2E Fuel {Suffix[..12]}";
        public string TransmissionName => $"E2E Transmission {Suffix[..12]}";
        public string PlateNumber => $"E2E{Suffix[..10].ToUpperInvariant()}";
        public string LicenseNumber => $"E2E-{Suffix}";
        public int AdminUserId { get; set; }
        public int EmployeeUserId { get; set; }
        public int BranchId { get; set; }
        public int CategoryId { get; set; }
        public int FuelTypeId { get; set; }
        public int TransmissionTypeId { get; set; }
        public int VehicleId { get; set; }
        public int ReservationId { get; set; }
        public int ContractId { get; set; }
        public int PaymentId { get; set; }
        public int ReviewId { get; set; }
    }
}
