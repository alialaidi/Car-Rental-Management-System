using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;
using _CarRental.Application.Constants.PolicyNames;
using _CarRental.Application.Common.Interfaces;

namespace _CarRental.Tests;

[ApiController]
[Authorize]
[Route("_tests/protected")]
public sealed class TestProtectedController : ControllerBase
{
    [HttpGet]
    public IActionResult Get() => Ok(new { name = User.Identity?.Name, isCustomer = User.IsInRole("Customer") });
}

[ApiController]
[AllowAnonymous]
[EnableRateLimiting("auth")]
[Route("_tests/rate-limited")]
public sealed class RateLimitTestController : ControllerBase
{
    [HttpGet]
    public IActionResult Get() => Ok(new { status = "ok" });
}

[ApiController]
[Route("_tests/policies")]
public sealed class PolicyTestController : ControllerBase
{
    [HttpGet("manage-cars")]
    [Authorize(Policy = Policies.ManageCars)]
    public IActionResult ManageCars() => Ok();

    [HttpGet("manage-employees")]
    [Authorize(Policy = Policies.ManageEmployees)]
    public IActionResult ManageEmployees() => Ok();

    [HttpGet("view-reports")]
    [Authorize(Policy = Policies.ViewReports)]
    public IActionResult ViewReports() => Ok();

    [HttpGet("process-payments")]
    [Authorize(Policy = Policies.ProcessPayments)]
    public IActionResult ProcessPayments() => Ok();
}

[ApiController]
[Authorize(Roles = "Customer")]
[Route("_tests/current-customer")]
public sealed class CurrentCustomerTestController(ICurrentUser currentUser) : ControllerBase
{
    [HttpGet]
    public async Task<IActionResult> Get() => Ok(new { customerId = await currentUser.GetCustomerIdAsync() });
}
