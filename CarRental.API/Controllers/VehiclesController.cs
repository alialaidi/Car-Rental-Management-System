using _CarRental.Application.Common.Interfaces;
using _CarRental.Application.Common.Models;
using _CarRental.Application.Constants.PolicyNames;
using _CarRental.Application.DTOs.Vehicles;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace _CarRental.API.Controllers;

[ApiController]
[Route("api/vehicles")]
[Authorize(Roles = "Admin,BranchManager,Employee")]
public sealed class VehiclesController(IVehicleService vehicleService):ControllerBase
{
    [HttpGet]
    public Task<PagedResult<VehicleDto>> List([FromQuery]int branchId,[FromQuery]PagedRequest request)=>vehicleService.ListByBranchAsync(branchId,request);


    [HttpGet("{vehicleId:int}")]
    public Task<VehicleDto> Get(int vehicleId)=>vehicleService.GetAsync(vehicleId);


    [HttpPost]
    [Authorize(Policy=Policies.ManageCars)]
    public async Task<IActionResult> Create(CreateVehicleRequest request){var result=await vehicleService.CreateAsync(request);return CreatedAtAction(nameof(Get),new{vehicleId=result.VehicleId},result);}
    
    
    [HttpPut("{vehicleId:int}")]
    [Authorize(Policy=Policies.ManageCars)]
    public Task<VehicleDto> Update(int vehicleId,UpdateVehicleRequest request)=>vehicleService.UpdateAsync(vehicleId,request);
    
    
    [HttpDelete("{vehicleId:int}")]
    [Authorize(Policy=Policies.ManageCars)]
    public async Task<IActionResult> Delete(int vehicleId){await vehicleService.DeleteAsync(vehicleId);return NoContent();}
    
    
    [HttpGet("{vehicleId:int}/images")]
    public Task<IReadOnlyList<VehicleImageDto>> Images(int vehicleId)=>vehicleService.ListImagesAsync(vehicleId);
    
    
    [HttpPost("{vehicleId:int}/images")]
    [Authorize(Policy=Policies.ManageCars)]
    public async Task<IActionResult> AddImage(int vehicleId,AddImageRequest request){var image=await vehicleService.AddImageAsync(vehicleId,request);return CreatedAtAction(nameof(Images),new{vehicleId},image);}
    
    
    [HttpDelete("{vehicleId:int}/images/{imageId:int}")]
    [Authorize(Policy=Policies.ManageCars)]
    public async Task<IActionResult> RemoveImage(int vehicleId,int imageId){await vehicleService.RemoveImageAsync(vehicleId,imageId);return NoContent();}
    
    
    [HttpPut("{vehicleId:int}/images/{imageId:int}/primary")]
    [Authorize(Policy=Policies.ManageCars)]
    public async Task<IActionResult> SetPrimary(int vehicleId,int imageId){await vehicleService.SetPrimaryImageAsync(vehicleId,imageId);return NoContent();}


}
