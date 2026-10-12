using _CarRental.Application.Common.Interfaces;
using _CarRental.Application.Common.Models;
using _CarRental.Application.Constants.PolicyNames;
using _CarRental.Application.DTOs.Branches;
using _CarRental.Domain.Constants;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace _CarRental.API.Controllers;

[ApiController]
[Route("api/branches")]
[Authorize(Roles = RoleNames.Admin + "," + RoleNames.BranchManager + "," + RoleNames.Employee)]
public sealed class BranchesController(IBranchService branchService) : ControllerBase
{
    [HttpGet]
    public Task<PagedResult<BranchDto>> List([FromQuery] PagedRequest request) => branchService.ListAsync(request);

    [HttpGet("{branchId:int}")]
    public Task<BranchDto> Get(int branchId) => branchService.GetAsync(branchId);

    [HttpPost]
    [Authorize(Roles = RoleNames.Admin)]
    public async Task<IActionResult> Create(CreateBranchRequest request)
    {
        var branch = await branchService.CreateAsync(request);
        return CreatedAtAction(nameof(Get), new { branchId = branch.BranchId }, branch);
    }

    [HttpPut("{branchId:int}")]
    [Authorize(Roles = RoleNames.Admin)]
    public Task<BranchDto> Update(int branchId, UpdateBranchRequest request) => branchService.UpdateAsync(branchId, request);

    [HttpDelete("{branchId:int}")]
    [Authorize(Roles = RoleNames.Admin)]
    public async Task<IActionResult> Delete(int branchId)
    {
        await branchService.DeleteAsync(branchId);
        return NoContent();
    }

    [HttpPut("{branchId:int}/manager")]
    [Authorize(Roles = RoleNames.Admin)]
    public async Task<IActionResult> SetManager(int branchId, SetManagerRequest request)
    {
        await branchService.SetManagerAsync(branchId, request.EmployeeId);
        return NoContent();
    }
}
