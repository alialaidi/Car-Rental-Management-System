using _CarRental.Application.Common.Interfaces;
using _CarRental.Application.Common.Models;
using _CarRental.Application.DTOs.Users;
using _CarRental.Domain.Constants;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace _CarRental.API.Controllers;

[ApiController]
[Route("api/users")]
[Authorize(Roles = RoleNames.Admin)]
public sealed class UsersController(IUserAdminService userAdminService) : ControllerBase
{


    [HttpGet]
    public Task<PagedResult<UserDto>> List([FromQuery] PagedRequest request, [FromQuery] string? search, [FromQuery] bool? isActive) =>
        userAdminService.ListAsync(request, search, isActive);



    [HttpPut("{userId:int}/active")]
    public async Task<IActionResult> SetActive(int userId, SetUserActiveRequest request)
    {
        await userAdminService.SetActiveAsync(userId, request.IsActive);
        return NoContent();
    }



    [HttpPost("{userId:int}/roles")]
    public async Task<IActionResult> AssignRole(int userId, AssignRoleRequest request)
    {
        await userAdminService.AssignRoleAsync(userId, request.RoleName);
        return NoContent();
    }



    [HttpDelete("{userId:int}/roles/{roleName}")]
    public async Task<IActionResult> RemoveRole(int userId, string roleName)
    {
        await userAdminService.RemoveRoleAsync(userId, roleName);
        return NoContent();
    }


}
