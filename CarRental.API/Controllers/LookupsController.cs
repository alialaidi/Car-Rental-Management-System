using _CarRental.Application.Common.Interfaces;
using _CarRental.Application.Constants.PolicyNames;
using _CarRental.Application.DTOs.Lookups;
using _CarRental.Domain.Enums;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace _CarRental.API.Controllers;

[ApiController]
[Route("api/lookups")]
[Authorize]
public sealed class LookupsController(ILookupService lookupService):ControllerBase
{
    [HttpGet("{type}")]
    public async Task<IActionResult> List(string type)=>TryType(type,out var parsed)?Ok(await lookupService.ListAsync(parsed)):BadRequest(new{error="Unknown lookup type."});
    [HttpPost("{type}")]
    [Authorize(Policy=Policies.ManageCars)]
    public async Task<IActionResult> Create(string type,LookupRequest request)
    {if(!TryType(type,out var parsed))return BadRequest(new{error="Unknown lookup type."});var result=await lookupService.CreateAsync(parsed,request);return CreatedAtAction(nameof(List),new{type},result);}
    [HttpPut("{type}/{id:int}")]
    [Authorize(Policy=Policies.ManageCars)]
    public async Task<IActionResult> Update(string type,int id,LookupRequest request)
    {if(!TryType(type,out var parsed))return BadRequest(new{error="Unknown lookup type."});return Ok(await lookupService.UpdateAsync(parsed,id,request));}
    [HttpDelete("{type}/{id:int}")]
    [Authorize(Policy=Policies.ManageCars)]
    public async Task<IActionResult> Delete(string type,int id)
    {if(!TryType(type,out var parsed))return BadRequest(new{error="Unknown lookup type."});await lookupService.DeleteAsync(parsed,id);return NoContent();}

    private static bool TryType(string value,out VehicleLookupType type)
    {
        type=value?.Trim().ToLowerInvariant() switch{"category" or "categories"=>VehicleLookupType.Category,"fueltype" or "fuel-types"=>VehicleLookupType.FuelType,"transmissiontype" or "transmission-types"=>VehicleLookupType.TransmissionType,_=>default};
        return value is not null&&new[]{"category","categories","fueltype","fuel-types","transmissiontype","transmission-types"}.Contains(value.Trim(),StringComparer.OrdinalIgnoreCase);
    }
}
