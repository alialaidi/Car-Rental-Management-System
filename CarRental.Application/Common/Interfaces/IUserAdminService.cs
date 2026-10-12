using _CarRental.Application.Common.Models;
using _CarRental.Application.DTOs.Users;

namespace _CarRental.Application.Common.Interfaces;

public interface IUserAdminService
{
    Task<PagedResult<UserDto>> ListAsync(PagedRequest request, string? search, bool? isActive);
    Task SetActiveAsync(int userId, bool isActive);
    Task AssignRoleAsync(int userId, string roleName);
    Task RemoveRoleAsync(int userId, string roleName);
}
