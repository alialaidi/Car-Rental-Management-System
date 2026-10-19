using _CarRental.Application.Common.Interfaces;
using _CarRental.Application.Common.Models;
using _CarRental.Application.DTOs.Maintenance;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace _CarRental.API.Controllers;

[ApiController]
[Route("api/maintenance")]
[Authorize(Roles = "Employee,BranchManager,Admin")]
public sealed class MaintenanceController(IMaintenanceService maintenanceService) : ControllerBase
{
    [HttpPost]
    [ProducesResponseType(typeof(MaintenanceDto), StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status409Conflict)]
    public async Task<ActionResult<MaintenanceDto>> Create(CreateMaintenanceRequest request)
    {
        var record = await maintenanceService.CreateAsync(request);
        return CreatedAtAction(nameof(Get), new { maintenanceId = record.MaintenanceId }, record);
    }

    [HttpGet("{maintenanceId:int}")]
    [ProducesResponseType(typeof(MaintenanceDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public Task<MaintenanceDto> Get(int maintenanceId) => maintenanceService.GetAsync(maintenanceId);

    [HttpPut("{maintenanceId:int}")]
    [ProducesResponseType(typeof(MaintenanceDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status409Conflict)]
    public Task<MaintenanceDto> Update(int maintenanceId, UpdateMaintenanceRequest request) =>
        maintenanceService.UpdateAsync(maintenanceId, request);

    [HttpPost("{maintenanceId:int}/complete")]
    [ProducesResponseType(typeof(MaintenanceDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status409Conflict)]
    public Task<MaintenanceDto> Complete(int maintenanceId, CompleteMaintenanceRequest request) =>
        maintenanceService.CompleteAsync(maintenanceId, request);

    [HttpPost("{maintenanceId:int}/cancel")]
    [ProducesResponseType(typeof(MaintenanceDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status409Conflict)]
    public Task<MaintenanceDto> Cancel(int maintenanceId) => maintenanceService.CancelAsync(maintenanceId);

    [HttpGet("by-vehicle/{vehicleId:int}")]
    public Task<IReadOnlyList<MaintenanceDto>> ListByVehicle(int vehicleId) => maintenanceService.ListByVehicleAsync(vehicleId);

    [HttpGet("by-branch/{branchId:int}")]
    public Task<PagedResult<MaintenanceDto>> ListByBranch(int branchId, [FromQuery] PagedRequest request) =>
        maintenanceService.ListByBranchAsync(branchId, request);
}
