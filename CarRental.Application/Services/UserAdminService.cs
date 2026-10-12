using System.Globalization;
using _CarRental.Application.Common.Interfaces;
using _CarRental.Application.Common.Models;
using _CarRental.Application.DTOs.Users;
using _CarRental.Domain.Constants;
using _CarRental.Domain.Entities;
using _CarRental.Domain.Exceptions;
using _CarRental.Domain.Interfaces.Repositories;

namespace _CarRental.Application.Services;

public sealed class UserAdminService(IUnitOfWork unitOfWork, ICurrentUser currentUser, IAuditLogger auditLogger, IDateTimeProvider dateTimeProvider, IBranchService branchService) : IUserAdminService
{
    private static readonly string[] SupportedRoles = [RoleNames.Admin, RoleNames.BranchManager, RoleNames.Employee, RoleNames.Customer];

    public async Task<PagedResult<UserDto>> ListAsync(PagedRequest request, string? search, bool? isActive)
    {
        ArgumentNullException.ThrowIfNull(request);
        search = string.IsNullOrWhiteSpace(search) ? null : search.Trim();
        if (search?.Length > 255) throw new BusinessRuleException("Search must not exceed 255 characters.");
        var (users, totalCount) = await unitOfWork.Users.ListAsync(request.Page, request.PageSize, search, isActive);
        var items = new List<UserDto>(users.Count);
        foreach (var user in users) items.Add(await ToDtoAsync(user));
        return new PagedResult<UserDto>(items, request.Page, request.PageSize, totalCount);
    }

    public async Task SetActiveAsync(int userId, bool isActive)
    {
        var user = await RequireUserAsync(userId);
        if (!isActive && userId == currentUser.UserId)
            throw new ConflictException("SELF_DEACTIVATION_NOT_ALLOWED", "You cannot deactivate your own account.");
        if (user.IsActive == isActive) return;

        await unitOfWork.BeginTransactionAsync();
        try
        {
            if (!isActive)
            {
                var employee = await unitOfWork.Employees.GetByUserIdAsync(userId);
                if (employee is not null)
                {
                    var branch = await unitOfWork.Branches.GetByIdAsync(employee.BranchId);
                    if (branch?.ManagerEmployeeId == employee.EmployeeId)
                    {
                        await unitOfWork.Branches.SetManagerAsync(employee.BranchId, null);
                        var managerRoleId = await unitOfWork.Users.GetRoleIdByNameAsync(RoleNames.BranchManager);
                        var employeeRoleId = await unitOfWork.Users.GetRoleIdByNameAsync(RoleNames.Employee);
                        if (managerRoleId.HasValue) await unitOfWork.Users.RemoveRoleAsync(userId, managerRoleId.Value);
                        if (employeeRoleId.HasValue) await unitOfWork.Users.AssignRoleAsync(userId, employeeRoleId.Value);
                    }
                }
            }
            await unitOfWork.Users.SetActiveAsync(userId, isActive);
            if (!isActive) await unitOfWork.RefreshTokens.RevokeAllForUserAsync(userId, dateTimeProvider.UtcNow);
            await unitOfWork.CommitAsync();
        }
        catch
        {
            await unitOfWork.RollbackAsync();
            throw;
        }

        await auditLogger.LogAsync(isActive ? AuditActions.UserActivated : AuditActions.UserDeactivated, "Users",
            userId.ToString(CultureInfo.InvariantCulture), new { user.IsActive }, new { IsActive = isActive }, currentUser.UserId);
    }

    public async Task AssignRoleAsync(int userId, string roleName)
    {
        roleName = ValidateRole(roleName);
        _ = await RequireUserAsync(userId);
        var roles = await unitOfWork.Users.GetRoleNamesAsync(userId);
        if (roles.Contains(roleName, StringComparer.Ordinal)) return;
        EnsureRoleCategoryCompatible(roles, roleName);
        if (roleName == RoleNames.BranchManager)
        {
            var employee = await unitOfWork.Employees.GetByUserIdAsync(userId)
                ?? throw new ConflictException("BRANCH_MANAGER_MUST_BE_EMPLOYEE", "A branch manager must have an employee profile.");
            await branchService.SetManagerAsync(employee.BranchId, employee.EmployeeId);
            await auditLogger.LogAsync(AuditActions.UserRoleAssigned, "Users", userId.ToString(CultureInfo.InvariantCulture),
                null, new { Role = roleName }, currentUser.UserId);
            return;
        }
        var roleId = await unitOfWork.Users.GetRoleIdByNameAsync(roleName) ?? throw new InvalidOperationException($"Role {roleName} is not seeded.");
        await unitOfWork.BeginTransactionAsync();
        try
        {
            await unitOfWork.Users.AssignRoleAsync(userId, roleId);
            await unitOfWork.RefreshTokens.RevokeAllForUserAsync(userId, dateTimeProvider.UtcNow);
            await unitOfWork.CommitAsync();
        }
        catch
        {
            await unitOfWork.RollbackAsync();
            throw;
        }
        await auditLogger.LogAsync(AuditActions.UserRoleAssigned, "Users", userId.ToString(CultureInfo.InvariantCulture),
            null, new { Role = roleName }, currentUser.UserId);
    }

    public async Task RemoveRoleAsync(int userId, string roleName)
    {
        roleName = ValidateRole(roleName);
        _ = await RequireUserAsync(userId);
        var roles = await unitOfWork.Users.GetRoleNamesAsync(userId);
        if (!roles.Contains(roleName, StringComparer.Ordinal)) return;
        if (roleName == RoleNames.BranchManager)
        {
            var employee = await unitOfWork.Employees.GetByUserIdAsync(userId);
            if (employee is not null)
            {
                var branch = await unitOfWork.Branches.GetByIdAsync(employee.BranchId);
                if (branch?.ManagerEmployeeId == employee.EmployeeId)
                    throw new ConflictException("BRANCH_MANAGER_ASSIGNMENT_EXISTS", "Assign another manager or clear the branch manager first.");
            }
        }
        var roleId = await unitOfWork.Users.GetRoleIdByNameAsync(roleName) ?? throw new InvalidOperationException($"Role {roleName} is not seeded.");
        await unitOfWork.BeginTransactionAsync();
        try
        {
            await unitOfWork.Users.RemoveRoleAsync(userId, roleId);
            await unitOfWork.RefreshTokens.RevokeAllForUserAsync(userId, dateTimeProvider.UtcNow);
            await unitOfWork.CommitAsync();
        }
        catch
        {
            await unitOfWork.RollbackAsync();
            throw;
        }
        await auditLogger.LogAsync(AuditActions.UserRoleRemoved, "Users", userId.ToString(CultureInfo.InvariantCulture),
            new { Role = roleName }, null, currentUser.UserId);
    }

    private async Task<User> RequireUserAsync(int userId) =>
        await unitOfWork.Users.GetByIdAsync(userId) ?? throw new NotFoundException("User was not found.");

    private async Task<UserDto> ToDtoAsync(User user) => new(user.UserId, user.Username, user.Email, user.PhoneNumber, user.IsActive,
        user.CreatedAt, await unitOfWork.Users.GetRoleNamesAsync(user.UserId));

    private static string ValidateRole(string roleName)
    {
        var value = roleName?.Trim();
        return SupportedRoles.Contains(value, StringComparer.Ordinal)
            ? value!
            : throw new BusinessRuleException("RoleName is not supported.");
    }

    private static void EnsureRoleCategoryCompatible(IReadOnlyList<string> roles, string requestedRole)
    {
        var isCustomerRole = requestedRole == RoleNames.Customer;
        if (roles.Any(role => (role == RoleNames.Customer) != isCustomerRole))
            throw new ConflictException("MIXED_CUSTOMER_STAFF_ROLES_NOT_ALLOWED", "A user cannot hold both customer and staff roles.");
    }


}
