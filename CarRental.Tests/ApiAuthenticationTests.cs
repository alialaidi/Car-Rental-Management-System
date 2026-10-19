using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;
using _CarRental.Application.Common.Interfaces;
using _CarRental.Application.Common.Models;
using _CarRental.Application.Constants.PolicyNames;
using _CarRental.Application.DTOs.Branches;
using _CarRental.Application.DTOs.Employees;
using _CarRental.Application.DTOs.Users;
using _CarRental.Application.DTOs.Vehicles;
using _CarRental.Application.DTOs.Lookups;
using _CarRental.Application.DTOs.Reservations;
using _CarRental.Application.DTOs;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.AspNetCore.TestHost;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Xunit;

namespace _CarRental.Tests;

public sealed class ApiAuthenticationTests
{
    [Fact]
    public async Task LivenessEndpointIsAnonymousAndDoesNotRequireDatabase()
    {
        using var factory = new AuthenticatedApiFactory();
        using var client = factory.CreateClient();

        var response = await client.GetAsync("/health/live");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.Equal("Healthy", await response.Content.ReadAsStringAsync());
        Assert.True(response.Headers.Contains("X-Correlation-ID"));
        Assert.Equal("nosniff", response.Headers.GetValues("X-Content-Type-Options").Single());
        Assert.Equal("DENY", response.Headers.GetValues("X-Frame-Options").Single());
    }

    [Fact]
    public async Task SwaggerDocumentPublishesBearerJwtSecurityScheme()
    {
        using var factory = new AuthenticatedApiFactory();
        using var client = factory.CreateClient();

        var response = await client.GetAsync("/swagger/v1/swagger.json");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        using var document = JsonDocument.Parse(await response.Content.ReadAsStringAsync());
        var scheme = document.RootElement.GetProperty("components").GetProperty("securitySchemes").GetProperty("Bearer");
        Assert.Equal("http", scheme.GetProperty("type").GetString());
        Assert.Equal("bearer", scheme.GetProperty("scheme").GetString());
        var paths = document.RootElement.GetProperty("paths");
        Assert.False(paths.GetProperty("/api/auth/login").GetProperty("post").TryGetProperty("security", out _));
        Assert.True(paths.GetProperty("/api/catalog/vehicles").GetProperty("get").TryGetProperty("security", out _));
    }

    [Fact]
    public async Task ReadinessEndpointReturnsServiceUnavailableWhenDatabaseProbeFails()
    {
        using var factory = new AuthenticatedApiFactory(simulateDbUnavailable: true);
        using var client = factory.CreateClient();

        var response = await client.GetAsync("/health/ready");

        Assert.Equal(HttpStatusCode.ServiceUnavailable, response.StatusCode);
        Assert.Equal("Unavailable", await response.Content.ReadAsStringAsync());
    }

    [Fact]
    public async Task VehicleCatalogRequiresAuthentication()
    {
        using var factory = new AuthenticatedApiFactory();
        using var client = factory.CreateClient();

        var response = await client.GetAsync("/api/catalog/vehicles");

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Fact]
    public async Task ReservationsRequireCustomerAuthentication()
    {
        using var factory = new AuthenticatedApiFactory();
        using var client = factory.CreateClient();

        Assert.Equal(HttpStatusCode.Unauthorized, (await client.GetAsync("/api/reservations")).StatusCode);
        Assert.Equal(HttpStatusCode.Unauthorized, (await client.GetAsync("/api/reservations/1")).StatusCode);
    }

    [Fact]
    public async Task ReservationsRejectStaffRoles()
    {
        using var factory = new AuthenticatedApiFactory();
        using var client = factory.CreateClient();
        var token = factory.Services.GetRequiredService<IJwtService>()
            .GenerateAccessToken(501, "staff-test", ["Employee"]).Token;
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);

        Assert.Equal(HttpStatusCode.Forbidden, (await client.GetAsync("/api/reservations")).StatusCode);
    }

    [Fact]
    public async Task ReservationsRejectInvalidRequestsBeforeDatabaseAccess()
    {
        using var factory = new AuthenticatedApiFactory();
        using var client = factory.CreateClient();
        var token = factory.Services.GetRequiredService<IJwtService>()
            .GenerateAccessToken(501, "customer-test", ["Customer"]).Token;
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);

        var response = await client.PostAsJsonAsync("/api/reservations", new CreateReservationRequest());

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public async Task StaffReservationEndpointsRejectCustomers()
    {
        using var factory = new AuthenticatedApiFactory();
        using var client = factory.CreateClient();
        var token = factory.Services.GetRequiredService<IJwtService>()
            .GenerateAccessToken(501, "customer-test", ["Customer"]).Token;
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);

        Assert.Equal(HttpStatusCode.Forbidden, (await client.GetAsync("/api/staff/reservations")).StatusCode);
        Assert.Equal(HttpStatusCode.Forbidden, (await client.PostAsync("/api/staff/reservations/1/confirm", null)).StatusCode);
    }

    [Fact]
    public async Task VehicleCatalogRejectsInvalidFiltersBeforeDatabaseAccess()
    {
        using var factory = new AuthenticatedApiFactory();
        using var client = factory.CreateClient();
        var token = factory.Services.GetRequiredService<IJwtService>()
            .GenerateAccessToken(501, "catalog-test", ["Customer"]).Token;
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);

        var response = await client.GetAsync("/api/catalog/vehicles?minimumDailyPrice=50&maximumDailyPrice=10");

        Assert.True(response.StatusCode == HttpStatusCode.BadRequest,
            $"Expected 400 but received {(int)response.StatusCode}: {await response.Content.ReadAsStringAsync()}");
    }

    [Fact]
    public async Task AuthorizationPoliciesEnforceConfiguredRoleMatrixAndRequireAuthentication()
    {
        using var factory = new AuthenticatedApiFactory();
        using var client = factory.CreateClient();
        var endpoints = new Dictionary<string, string>
        {
            [Policies.ManageCars] = "/_tests/policies/manage-cars",
            [Policies.ManageEmployees] = "/_tests/policies/manage-employees",
            [Policies.ViewReports] = "/_tests/policies/view-reports",
            [Policies.ProcessPayments] = "/_tests/policies/process-payments"
        };

        foreach (var endpoint in endpoints.Values)
            Assert.Equal(HttpStatusCode.Unauthorized, (await client.GetAsync(endpoint)).StatusCode);

        await AssertPolicyRole("Customer", HttpStatusCode.Forbidden, HttpStatusCode.Forbidden, HttpStatusCode.Forbidden, HttpStatusCode.Forbidden);
        await AssertPolicyRole("Employee", HttpStatusCode.Forbidden, HttpStatusCode.Forbidden, HttpStatusCode.Forbidden, HttpStatusCode.OK);
        await AssertPolicyRole("BranchManager", HttpStatusCode.OK, HttpStatusCode.OK, HttpStatusCode.OK, HttpStatusCode.OK);
        await AssertPolicyRole("Admin", HttpStatusCode.OK, HttpStatusCode.OK, HttpStatusCode.OK, HttpStatusCode.OK);

        async Task AssertPolicyRole(string role, params HttpStatusCode[] expected)
        {
            var token = factory.Services.GetRequiredService<IJwtService>()
                .GenerateAccessToken(501, "policy-test", [role]).Token;
            client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);
            var actual = new List<HttpStatusCode>();
            foreach (var endpoint in endpoints.Values)
                actual.Add((await client.GetAsync(endpoint)).StatusCode);
            Assert.Equal(expected, actual);
            client.DefaultRequestHeaders.Authorization = null;
        }
    }

    [SqlIntegrationFact]
    public async Task CurrentUserResolvesCustomerProfileFromAuthenticatedUserId()
    {
        using var factory = new AuthenticatedApiFactory();
        using var client = factory.CreateClient();
        var registration = await client.PostAsJsonAsync("/api/auth/register", NewRegistrationRequest());
        Assert.Equal(HttpStatusCode.Created, registration.StatusCode);
        var issued = await registration.Content.ReadFromJsonAsync<AuthResult>();
        Assert.NotNull(issued);
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", issued.AccessToken);

        var response = await client.GetAsync("/_tests/current-customer");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        using var body = JsonDocument.Parse(await response.Content.ReadAsStringAsync());
        Assert.True(body.RootElement.GetProperty("customerId").GetInt32() > 0);
    }

    [SqlIntegrationFact]
    public async Task BranchManagerIsScopedAndDeactivationRevokesEmployeeRefreshTokens()
    {
        using var factory = new AuthenticatedApiFactory(authPermitLimit: 100);
        using var client = factory.CreateClient();
        var jwt = factory.Services.GetRequiredService<IJwtService>();
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", jwt.GenerateAccessToken(1, "admin", ["Admin"]).Token);

        var branchAResponse = await client.PostAsJsonAsync("/api/branches", new CreateBranchRequest
        {
            Name = "Branch A " + Guid.NewGuid().ToString("N")[..8], Address = "Address A", City = "Amman"
        });
        var branchBResponse = await client.PostAsJsonAsync("/api/branches", new CreateBranchRequest
        {
            Name = "Branch B " + Guid.NewGuid().ToString("N")[..8], Address = "Address B", City = "Zarqa"
        });
        Assert.Equal(HttpStatusCode.Created, branchAResponse.StatusCode);
        Assert.Equal(HttpStatusCode.Created, branchBResponse.StatusCode);
        var branchA = await branchAResponse.Content.ReadFromJsonAsync<BranchDto>();
        var branchB = await branchBResponse.Content.ReadFromJsonAsync<BranchDto>();
        Assert.NotNull(branchA);
        Assert.NotNull(branchB);

        var managerRequest = NewEmployeeRequest(branchA.BranchId, manager: true);
        var managerResponse = await client.PostAsJsonAsync("/api/employees", managerRequest);
        Assert.Equal(HttpStatusCode.Created, managerResponse.StatusCode);
        var manager = await managerResponse.Content.ReadFromJsonAsync<EmployeeDto>();
        Assert.NotNull(manager);
        Assert.Equal("BranchManager", Assert.Single(manager.Roles));
        var managerBranch = await client.GetFromJsonAsync<BranchDto>($"/api/branches/{branchA.BranchId}");
        Assert.Equal(manager.EmployeeId, managerBranch?.ManagerEmployeeId);
        Assert.Equal(HttpStatusCode.Conflict, (await client.PutAsJsonAsync($"/api/branches/{branchB.BranchId}/manager", new SetManagerRequest { EmployeeId = manager.EmployeeId })).StatusCode);

        var managerLogin = await client.PostAsJsonAsync("/api/auth/login", new LoginRequest
        {
            UsernameOrEmail = managerRequest.Username, Password = managerRequest.Password
        });
        var managerTokens = await managerLogin.Content.ReadFromJsonAsync<AuthResult>();
        Assert.Equal(HttpStatusCode.OK, managerLogin.StatusCode);
        Assert.NotNull(managerTokens);
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", managerTokens.AccessToken);
        Assert.Equal(HttpStatusCode.OK, (await client.GetAsync($"/api/employees?branchId={branchA.BranchId}")).StatusCode);
        Assert.Equal(HttpStatusCode.Forbidden, (await client.GetAsync($"/api/employees?branchId={branchB.BranchId}")).StatusCode);
        Assert.Equal(HttpStatusCode.Forbidden, (await client.DeleteAsync($"/api/branches/{branchA.BranchId}")).StatusCode);

        var staffRequest = NewEmployeeRequest(branchA.BranchId, manager: false);
        Assert.Equal(HttpStatusCode.Created, (await client.PostAsJsonAsync("/api/employees", staffRequest)).StatusCode);
        var staffLogin = await client.PostAsJsonAsync("/api/auth/login", new LoginRequest
        {
            UsernameOrEmail = staffRequest.Username, Password = staffRequest.Password
        });
        var staffTokens = await staffLogin.Content.ReadFromJsonAsync<AuthResult>();
        Assert.Equal(HttpStatusCode.OK, staffLogin.StatusCode);
        Assert.NotNull(staffTokens);
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", staffTokens.AccessToken);
        Assert.Equal(HttpStatusCode.Forbidden, (await client.GetAsync($"/api/employees?branchId={branchA.BranchId}")).StatusCode);

        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", jwt.GenerateAccessToken(1, "admin", ["Admin"]).Token);
        var staffList = await client.GetFromJsonAsync<PagedResult<EmployeeDto>>($"/api/employees?branchId={branchA.BranchId}");
        var staff = Assert.Single(staffList!.Items, item => item.Username == staffRequest.Username);
        var userListResponse = await client.GetAsync("/api/users?page=1&pageSize=100");
        Assert.Equal(HttpStatusCode.OK, userListResponse.StatusCode);
        Assert.DoesNotContain("PasswordHash", await userListResponse.Content.ReadAsStringAsync(), StringComparison.OrdinalIgnoreCase);
        Assert.Equal(HttpStatusCode.NoContent, (await client.PostAsJsonAsync($"/api/users/{staff.UserId}/roles", new AssignRoleRequest { RoleName = "Admin" })).StatusCode);
        Assert.Equal(HttpStatusCode.NoContent, (await client.DeleteAsync($"/api/users/{staff.UserId}/roles/Admin")).StatusCode);
        staffLogin = await client.PostAsJsonAsync("/api/auth/login", new LoginRequest
        {
            UsernameOrEmail = staffRequest.Username, Password = staffRequest.Password
        });
        staffTokens = await staffLogin.Content.ReadFromJsonAsync<AuthResult>();
        Assert.NotNull(staffTokens);
        Assert.Equal(HttpStatusCode.NoContent, (await client.PutAsJsonAsync($"/api/users/{staff.UserId}/active", new SetUserActiveRequest { IsActive = false })).StatusCode);
        Assert.Equal(HttpStatusCode.Unauthorized, (await client.PostAsJsonAsync("/api/auth/refresh", new RefreshTokenRequest { RefreshToken = staffTokens.RefreshToken })).StatusCode);
        Assert.Equal(HttpStatusCode.Unauthorized, (await client.PostAsJsonAsync("/api/auth/login", new LoginRequest
        {
            UsernameOrEmail = staffRequest.Username, Password = staffRequest.Password
        })).StatusCode);
        Assert.Equal(HttpStatusCode.Conflict, (await client.DeleteAsync($"/api/branches/{branchA.BranchId}")).StatusCode);
    }

    [SqlIntegrationFact]
    public async Task FleetManagementEnforcesBranchScopeUniqueLookupsAndPrimaryImages()
    {
        using var factory = new AuthenticatedApiFactory(authPermitLimit: 100);
        using var client = factory.CreateClient();
        var jwt = factory.Services.GetRequiredService<IJwtService>();
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", jwt.GenerateAccessToken(1, "admin", ["Admin"]).Token);
        var branchAResponse = await client.PostAsJsonAsync("/api/branches", new CreateBranchRequest { Name="Fleet A "+Guid.NewGuid().ToString("N")[..8], Address="Address", City="Amman" });
        var branchBResponse = await client.PostAsJsonAsync("/api/branches", new CreateBranchRequest { Name="Fleet B "+Guid.NewGuid().ToString("N")[..8], Address="Address", City="Zarqa" });
        var branchA = await branchAResponse.Content.ReadFromJsonAsync<BranchDto>();
        var branchB = await branchBResponse.Content.ReadFromJsonAsync<BranchDto>();
        Assert.Equal(HttpStatusCode.Created, branchAResponse.StatusCode);
        Assert.Equal(HttpStatusCode.Created, branchBResponse.StatusCode);
        Assert.NotNull(branchA); Assert.NotNull(branchB);

        var managerRequest = NewEmployeeRequest(branchA.BranchId, manager: true);
        Assert.Equal(HttpStatusCode.Created, (await client.PostAsJsonAsync("/api/employees", managerRequest)).StatusCode);
        var managerLogin = await client.PostAsJsonAsync("/api/auth/login", new LoginRequest { UsernameOrEmail=managerRequest.Username, Password=managerRequest.Password });
        var managerTokens = await managerLogin.Content.ReadFromJsonAsync<AuthResult>();
        Assert.NotNull(managerTokens);
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", managerTokens.AccessToken);

        var vehicle = NewVehicleRequest(branchA.BranchId);
        var created = await client.PostAsJsonAsync("/api/vehicles", vehicle);
        Assert.Equal(HttpStatusCode.Created, created.StatusCode);
        var vehicleDto = await created.Content.ReadFromJsonAsync<VehicleDto>();
        Assert.NotNull(vehicleDto);
        Assert.Equal("Available", vehicleDto.Status);
        Assert.Equal(HttpStatusCode.Conflict, (await client.PostAsJsonAsync("/api/vehicles", vehicle)).StatusCode);
        var invalidSeats = NewVehicleRequest(branchA.BranchId); invalidSeats.SeatsCount = 0;
        Assert.Equal(HttpStatusCode.BadRequest, (await client.PostAsJsonAsync("/api/vehicles", invalidSeats)).StatusCode);
        Assert.Equal(HttpStatusCode.NotFound, (await client.PostAsJsonAsync("/api/vehicles", new CreateVehicleRequest
        {
            PlateNumber="MISS"+Guid.NewGuid().ToString("N")[..8],Make="Make",Model="Model",Year=2024,CategoryId=9999,
            FuelTypeId=1,TransmissionTypeId=1,SeatsCount=4,DailyPrice=10,BranchId=branchA.BranchId
        })).StatusCode);
        Assert.Equal(HttpStatusCode.Conflict, (await client.PostAsJsonAsync("/api/lookups/categories", new LookupRequest { Name="Economy" })).StatusCode);
        var otherBranch = NewVehicleRequest(branchB.BranchId);
        Assert.Equal(HttpStatusCode.Forbidden, (await client.PostAsJsonAsync("/api/vehicles", otherBranch)).StatusCode);
        Assert.Equal(HttpStatusCode.Conflict, (await client.DeleteAsync("/api/lookups/categories/1")).StatusCode);

        var image1Response = await client.PostAsJsonAsync($"/api/vehicles/{vehicleDto.VehicleId}/images", new AddImageRequest { ImageUrl="https://images.integration.test/one.jpg", IsPrimary=true });
        var image2Response = await client.PostAsJsonAsync($"/api/vehicles/{vehicleDto.VehicleId}/images", new AddImageRequest { ImageUrl="https://images.integration.test/two.jpg", IsPrimary=true });
        Assert.True(image1Response.StatusCode == HttpStatusCode.Created, await image1Response.Content.ReadAsStringAsync());
        Assert.Equal(HttpStatusCode.Created, image2Response.StatusCode);
        var image1 = await image1Response.Content.ReadFromJsonAsync<VehicleImageDto>();
        var image2 = await image2Response.Content.ReadFromJsonAsync<VehicleImageDto>();
        Assert.NotNull(image1); Assert.NotNull(image2);
        var images = await client.GetFromJsonAsync<List<VehicleImageDto>>($"/api/vehicles/{vehicleDto.VehicleId}/images");
        Assert.Single(images!, image => image.IsPrimary);
        Assert.Contains(images!, image => image.ImageId == image2.ImageId && image.IsPrimary);
        Assert.Equal(HttpStatusCode.NoContent, (await client.PutAsync($"/api/vehicles/{vehicleDto.VehicleId}/images/{image1.ImageId}/primary", null)).StatusCode);
        images = await client.GetFromJsonAsync<List<VehicleImageDto>>($"/api/vehicles/{vehicleDto.VehicleId}/images");
        Assert.Contains(images!, image => image.ImageId == image1.ImageId && image.IsPrimary);
        Assert.Equal(HttpStatusCode.BadRequest, (await client.PostAsJsonAsync($"/api/vehicles/{vehicleDto.VehicleId}/images", new AddImageRequest { ImageUrl="file:///not-allowed.png" })).StatusCode);

        vehicle.DailyPrice = 40.25m;
        var updatedVehicle = await client.PutAsJsonAsync($"/api/vehicles/{vehicleDto.VehicleId}", vehicle);
        Assert.Equal(HttpStatusCode.OK, updatedVehicle.StatusCode);
        Assert.Equal(40.25m, (await updatedVehicle.Content.ReadFromJsonAsync<VehicleDto>())?.DailyPrice);

        var updateToOtherBranch = new UpdateVehicleRequest
        {
            PlateNumber=vehicle.PlateNumber,Make=vehicle.Make,Model=vehicle.Model,Year=vehicle.Year,CategoryId=vehicle.CategoryId,
            FuelTypeId=vehicle.FuelTypeId,TransmissionTypeId=vehicle.TransmissionTypeId,SeatsCount=vehicle.SeatsCount,
            DailyPrice=vehicle.DailyPrice,BranchId=branchB.BranchId,CurrentMileage=vehicle.CurrentMileage
        };
        Assert.Equal(HttpStatusCode.Forbidden, (await client.PutAsJsonAsync($"/api/vehicles/{vehicleDto.VehicleId}", updateToOtherBranch)).StatusCode);
    }

    [Fact]
    public async Task ProtectedEndpointChallengesMissingTokenAndAcceptsValidBearerToken()
    {
        using var factory = new AuthenticatedApiFactory();
        using var client = factory.CreateClient();

        var anonymousResponse = await client.GetAsync("/_tests/protected");
        Assert.Equal(HttpStatusCode.Unauthorized, anonymousResponse.StatusCode);
        Assert.Equal("application/problem+json", anonymousResponse.Content.Headers.ContentType?.MediaType);

        var jwtService = factory.Services.GetRequiredService<IJwtService>();
        var (token, _) = jwtService.GenerateAccessToken(101, "integration-customer", ["Customer"]);
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);

        var authenticatedResponse = await client.GetAsync("/_tests/protected");
        Assert.Equal(HttpStatusCode.OK, authenticatedResponse.StatusCode);
        var body = await authenticatedResponse.Content.ReadAsStringAsync();
        Assert.Contains("integration-customer", body);
        Assert.Contains("\"isCustomer\":true", body);
    }

    [Fact]
    public async Task AuthRateLimitReturns429AfterConfiguredPermitLimit()
    {
        using var factory = new AuthenticatedApiFactory();
        using var client = factory.CreateClient();

        for (var requestNumber = 0; requestNumber < 5; requestNumber++)
        {
            Assert.Equal(HttpStatusCode.OK, (await client.GetAsync("/_tests/rate-limited")).StatusCode);
        }

        var rejected = await client.GetAsync("/_tests/rate-limited");
        Assert.Equal(HttpStatusCode.TooManyRequests, rejected.StatusCode);
        Assert.Equal("application/problem+json", rejected.Content.Headers.ContentType?.MediaType);
        Assert.NotNull(rejected.Headers.RetryAfter);
    }

    [SqlIntegrationFact]
    public async Task RegistrationAndEmailLoginIssueDifferentRefreshTokens()
    {
        using var factory = new AuthenticatedApiFactory();
        using var client = factory.CreateClient();
        var request = NewRegistrationRequest();

        var registeredResponse = await client.PostAsJsonAsync("/api/auth/register", request);
        Assert.Equal(HttpStatusCode.Created, registeredResponse.StatusCode);
        var registered = await registeredResponse.Content.ReadFromJsonAsync<AuthResult>();
        Assert.NotNull(registered);

        var emailToken = await client.PostAsJsonAsync("/api/auth/login", new LoginRequest
        {
            UsernameOrEmail = request.Email.ToUpperInvariant(),
            Password = request.Password
        });
        Assert.Equal(HttpStatusCode.OK, emailToken.StatusCode);
        var loggedIn = await emailToken.Content.ReadFromJsonAsync<AuthResult>();
        Assert.NotNull(loggedIn);
        Assert.NotEqual(registered.RefreshToken, loggedIn.RefreshToken);
        Assert.Equal(HttpStatusCode.OK, (await client.PostAsJsonAsync("/api/auth/refresh", new RefreshTokenRequest
        {
            RefreshToken = registered.RefreshToken
        })).StatusCode);
    }

    [SqlIntegrationFact]
    public async Task RegistrationDuplicatesReturn409AndInvalidRequestReturns400()
    {
        using var factory = new AuthenticatedApiFactory(authPermitLimit: 100);
        using var client = factory.CreateClient();
        var request = NewRegistrationRequest();
        Assert.Equal(HttpStatusCode.Created, (await client.PostAsJsonAsync("/api/auth/register", request)).StatusCode);

        var duplicateUsername = NewRegistrationRequest();
        duplicateUsername.Username = request.Username;
        Assert.Equal(HttpStatusCode.Conflict, (await client.PostAsJsonAsync("/api/auth/register", duplicateUsername)).StatusCode);

        var duplicateEmail = NewRegistrationRequest();
        duplicateEmail.Email = request.Email;
        Assert.Equal(HttpStatusCode.Conflict, (await client.PostAsJsonAsync("/api/auth/register", duplicateEmail)).StatusCode);

        var duplicateLicense = NewRegistrationRequest();
        duplicateLicense.DrivingLicenseNumber = request.DrivingLicenseNumber;
        Assert.Equal(HttpStatusCode.Conflict, (await client.PostAsJsonAsync("/api/auth/register", duplicateLicense)).StatusCode);

        var invalidExpiry = NewRegistrationRequest();
        invalidExpiry.DrivingLicenseExpiry = DateTime.UtcNow.Date;
        Assert.Equal(HttpStatusCode.BadRequest, (await client.PostAsJsonAsync("/api/auth/register", invalidExpiry)).StatusCode);
    }

    [SqlIntegrationFact]
    public async Task UnknownUserAndWrongPasswordShareTheSameUnauthorizedResponse()
    {
        using var factory = new AuthenticatedApiFactory(authPermitLimit: 100);
        using var client = factory.CreateClient();
        var registration = NewRegistrationRequest();
        Assert.Equal(HttpStatusCode.Created, (await client.PostAsJsonAsync("/api/auth/register", registration)).StatusCode);

        var knownUser = await client.PostAsJsonAsync("/api/auth/login", new LoginRequest
        {
            UsernameOrEmail = registration.Username,
            Password = "WrongPass123"
        });
        var unknownUser = await client.PostAsJsonAsync("/api/auth/login", new LoginRequest
        {
            UsernameOrEmail = "unknown_" + Guid.NewGuid().ToString("N"),
            Password = "WrongPass123"
        });

        Assert.Equal(HttpStatusCode.Unauthorized, knownUser.StatusCode);
        Assert.Equal(HttpStatusCode.Unauthorized, unknownUser.StatusCode);
        using var knownProblem = JsonDocument.Parse(await knownUser.Content.ReadAsStringAsync());
        using var unknownProblem = JsonDocument.Parse(await unknownUser.Content.ReadAsStringAsync());
        Assert.Equal(knownProblem.RootElement.GetProperty("detail").GetString(), unknownProblem.RootElement.GetProperty("detail").GetString());
    }

    [SqlIntegrationFact]
    public async Task RefreshRotatesAndReuseRevokesAllSessionsLogoutIsIdempotent()
    {
        using var factory = new AuthenticatedApiFactory(authPermitLimit: 100);
        using var client = factory.CreateClient();
        var register = await client.PostAsJsonAsync("/api/auth/register", NewRegistrationRequest());
        Assert.Equal(HttpStatusCode.Created, register.StatusCode);
        var initial = await register.Content.ReadFromJsonAsync<AuthResult>();
        Assert.NotNull(initial);

        var rotatedResponse = await client.PostAsJsonAsync("/api/auth/refresh", new RefreshTokenRequest { RefreshToken = initial.RefreshToken });
        Assert.Equal(HttpStatusCode.OK, rotatedResponse.StatusCode);
        var rotated = await rotatedResponse.Content.ReadFromJsonAsync<AuthResult>();
        Assert.NotNull(rotated);
        Assert.NotEqual(initial.RefreshToken, rotated.RefreshToken);

        var reuse = await client.PostAsJsonAsync("/api/auth/refresh", new RefreshTokenRequest { RefreshToken = initial.RefreshToken });
        Assert.Equal(HttpStatusCode.Unauthorized, reuse.StatusCode);
        var afterReuse = await client.PostAsJsonAsync("/api/auth/refresh", new RefreshTokenRequest { RefreshToken = rotated.RefreshToken });
        Assert.Equal(HttpStatusCode.Unauthorized, afterReuse.StatusCode);

        var logoutRegister = await client.PostAsJsonAsync("/api/auth/register", NewRegistrationRequest());
        Assert.Equal(HttpStatusCode.Created, logoutRegister.StatusCode);
        var logoutTokens = await logoutRegister.Content.ReadFromJsonAsync<AuthResult>();
        Assert.NotNull(logoutTokens);
        var logout = new RefreshTokenRequest { RefreshToken = logoutTokens.RefreshToken };
        Assert.Equal(HttpStatusCode.NoContent, (await client.PostAsJsonAsync("/api/auth/logout", logout)).StatusCode);
        Assert.Equal(HttpStatusCode.NoContent, (await client.PostAsJsonAsync("/api/auth/logout", logout)).StatusCode);
    }

    [SqlIntegrationFact]
    public async Task ConcurrentRefreshAttemptsAllowOneRotationAndRevokeSessionsOnReplay()
    {
        using var factory = new AuthenticatedApiFactory(authPermitLimit: 100);
        using var client = factory.CreateClient();
        var registration = await client.PostAsJsonAsync("/api/auth/register", NewRegistrationRequest());
        Assert.Equal(HttpStatusCode.Created, registration.StatusCode);
        var issued = await registration.Content.ReadFromJsonAsync<AuthResult>();
        Assert.NotNull(issued);

        var attempts = await Task.WhenAll(
            client.PostAsJsonAsync("/api/auth/refresh", new RefreshTokenRequest { RefreshToken = issued.RefreshToken }),
            client.PostAsJsonAsync("/api/auth/refresh", new RefreshTokenRequest { RefreshToken = issued.RefreshToken }));

        Assert.Single(attempts, response => response.StatusCode == HttpStatusCode.OK);
        Assert.Single(attempts, response => response.StatusCode == HttpStatusCode.Unauthorized);
        var rotated = await attempts.Single(response => response.StatusCode == HttpStatusCode.OK)
            .Content.ReadFromJsonAsync<AuthResult>();
        Assert.NotNull(rotated);

        var replayedSession = await client.PostAsJsonAsync("/api/auth/refresh", new RefreshTokenRequest
        {
            RefreshToken = rotated.RefreshToken
        });
        Assert.Equal(HttpStatusCode.Unauthorized, replayedSession.StatusCode);
    }

    private static RegisterRequest NewRegistrationRequest() => new()
    {
        Username = "user_" + Guid.NewGuid().ToString("N")[..16],
        Email = Guid.NewGuid().ToString("N") + "@integration.test",
        Password = "Passw0rd!",
        PhoneNumber = "0790000000",
        FullName = "Integration Customer",
        DrivingLicenseNumber = "DL" + Guid.NewGuid().ToString("N"),
        DrivingLicenseExpiry = DateTime.UtcNow.Date.AddYears(1),
        Address = "Integration address"
    };

    private static CreateEmployeeRequest NewEmployeeRequest(int branchId, bool manager) => new()
    {
        Username = (manager ? "manager_" : "employee_") + Guid.NewGuid().ToString("N")[..12],
        Email = Guid.NewGuid().ToString("N") + "@integration.test",
        Password = "StaffPass123",
        PhoneNumber = "0790000000",
        BranchId = branchId,
        Position = manager ? "Branch Manager" : "Rental Agent",
        HireDate = DateTime.UtcNow.Date.AddYears(-1),
        IsBranchManager = manager
    };

    private static CreateVehicleRequest NewVehicleRequest(int branchId) => new()
    {
        PlateNumber="TEST"+Guid.NewGuid().ToString("N")[..12].ToUpperInvariant(),Make="Toyota",Model="Corolla",Year=2024,
        CategoryId=1,FuelTypeId=1,TransmissionTypeId=1,SeatsCount=5,DailyPrice=35.50m,BranchId=branchId,CurrentMileage=10
    };

    internal sealed class AuthenticatedApiFactory(int authPermitLimit = 5, bool simulateDbUnavailable = false) : WebApplicationFactory<Program>
    {
        protected override void ConfigureWebHost(IWebHostBuilder builder)
        {
            builder.ConfigureLogging(logging => logging.ClearProviders());
            builder.UseContentRoot(FindApiContentRoot());
            var testConnectionString = Environment.GetEnvironmentVariable("CAR_RENTAL_TEST_CONNECTION_STRING");
            if (string.IsNullOrWhiteSpace(testConnectionString))
                testConnectionString = "Server=localhost;Database=CarRentalDB;Integrated Security=True;TrustServerCertificate=True";
            builder.UseSetting("Jwt:Issuer", "CarRentalSystem.ApiTests");
            builder.UseSetting("Jwt:Audience", "CarRentalSystem.ApiTests.Client");
            builder.UseSetting("Jwt:SigningKey", "api-test-signing-key-at-least-32-bytes-long");
            builder.UseSetting("ConnectionStrings:CarRentalDb", testConnectionString);
            builder.UseSetting("RateLimiting:Auth:PermitLimit", authPermitLimit.ToString());
            builder.ConfigureAppConfiguration((_, configuration) => configuration.AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["Jwt:Issuer"] = "CarRentalSystem.ApiTests",
                ["Jwt:Audience"] = "CarRentalSystem.ApiTests.Client",
                ["Jwt:SigningKey"] = "api-test-signing-key-at-least-32-bytes-long",
                ["ConnectionStrings:CarRentalDb"] = testConnectionString,
                ["RateLimiting:Auth:PermitLimit"] = authPermitLimit.ToString()
            }));

            builder.ConfigureTestServices(services =>
            {
                services.AddControllers().AddApplicationPart(typeof(TestProtectedController).Assembly);
                if (simulateDbUnavailable)
                    services.AddScoped<IDatabaseHealthProbe, UnavailableDatabaseHealthProbe>();
            });
        }

        private static string FindApiContentRoot()
        {
            var directory = new DirectoryInfo(AppContext.BaseDirectory);
            while (directory is not null && !Directory.Exists(Path.Combine(directory.FullName, "CarRental.API")))
                directory = directory.Parent;

            return directory is null
                ? throw new DirectoryNotFoundException("Could not find the repository CarRental.API content root.")
                : Path.Combine(directory.FullName, "CarRental.API");
        }

        private sealed class UnavailableDatabaseHealthProbe : IDatabaseHealthProbe
        {
            public Task<bool> IsHealthyAsync(CancellationToken cancellationToken = default) => Task.FromResult(false);
        }
    }
}
