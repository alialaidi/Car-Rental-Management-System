using _CarRental.Application.Common.Interfaces;
using _CarRental.Application.Constants.PolicyNames;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.ModelBinding;

namespace _CarRental.API.Controllers;

[ApiController]
[Route("api/reports")]
[Authorize(Policy = Policies.ViewReports)]
public sealed class ReportsController(IReportService reports) : ControllerBase
{
    [HttpGet("fleet-status")]
    public async Task<IActionResult> FleetStatus([FromQuery] int? branchId) => Ok(await reports.GetFleetStatusAsync(branchId));

    [HttpGet("revenue")]
    public async Task<IActionResult> Revenue([FromQuery, BindRequired] DateOnly from, [FromQuery, BindRequired] DateOnly to, [FromQuery] int? branchId) =>
        Ok(await reports.GetRevenueAsync(from, to, branchId));

    [HttpGet("top-vehicles")]
    public async Task<IActionResult> TopVehicles([FromQuery, BindRequired] DateOnly from, [FromQuery, BindRequired] DateOnly to,
        [FromQuery] int? branchId, [FromQuery] int take = 10) =>
        Ok(await reports.GetTopVehiclesAsync(from, to, branchId, take));

    [HttpGet("maintenance-cost")]
    public async Task<IActionResult> MaintenanceCost([FromQuery, BindRequired] DateOnly from, [FromQuery, BindRequired] DateOnly to, [FromQuery] int? branchId) =>
        Ok(await reports.GetMaintenanceCostAsync(from, to, branchId));

    [HttpGet("payments-summary")]
    public async Task<IActionResult> PaymentSummary([FromQuery, BindRequired] DateOnly from, [FromQuery, BindRequired] DateOnly to, [FromQuery] int? branchId) =>
        Ok(await reports.GetPaymentSummaryAsync(from, to, branchId));
}
