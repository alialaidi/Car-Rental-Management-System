using _CarRental.Application.Common.Interfaces;
using _CarRental.Application.Common.Models;
using _CarRental.Application.DTOs.Vehicles;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace _CarRental.API.Controllers;

[ApiController]
[Route("api/catalog/vehicles")]
[Authorize]
public sealed class VehicleCatalogController(IVehicleQueryService vehicleQueryService) : ControllerBase
{


    [HttpGet]
    [ProducesResponseType(typeof(PagedResult<VehicleListItemDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public Task<PagedResult<VehicleListItemDto>> Search([FromQuery] VehicleSearchRequest request) =>
        vehicleQueryService.SearchAsync(request);



    [HttpGet("{vehicleId:int}")]
    [ProducesResponseType(typeof(VehicleDetailsDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public Task<VehicleDetailsDto> GetDetails(int vehicleId) =>
        vehicleQueryService.GetDetailsAsync(vehicleId);


}
