using System.Globalization;
using _CarRental.Application.Common.Interfaces;
using _CarRental.Application.Common.Models;
using _CarRental.Application.DTOs.Branches;
using _CarRental.Application.Validators;
using _CarRental.Domain.Constants;
using _CarRental.Domain.Entities;
using _CarRental.Domain.Exceptions;
using _CarRental.Domain.Interfaces.Repositories;

namespace _CarRental.Application.Services;

public sealed class BranchService(IUnitOfWork unitOfWork, ICurrentUser currentUser, IAuditLogger auditLogger, IDateTimeProvider dateTimeProvider) : IBranchService
{
    public async Task<BranchDto> CreateAsync(CreateBranchRequest request)
    {
        var branch = Map(StaffRequestValidator.Validate(request));
        branch.BranchId = await unitOfWork.Branches.CreateAsync(branch);
        await AuditAsync(AuditActions.BranchCreated, branch.BranchId, new { branch.Name, branch.City });
        return ToDto(branch);
    }

    public async Task<BranchDto> GetAsync(int branchId) => ToDto(await RequireBranchAsync(branchId));

    public async Task<PagedResult<BranchDto>> ListAsync(PagedRequest request)
    {
        ArgumentNullException.ThrowIfNull(request);
        var items = await unitOfWork.Branches.ListAsync(request.Page, request.PageSize);
        return new PagedResult<BranchDto>(items.Select(ToDto).ToArray(), request.Page, request.PageSize, await unitOfWork.Branches.GetCountAsync());
    }

    public async Task<BranchDto> UpdateAsync(int branchId, UpdateBranchRequest request)
    {
        var existing = await RequireBranchAsync(branchId);
        var updated = Map(StaffRequestValidator.Validate(request));
        updated.BranchId = branchId;
        updated.ManagerEmployeeId = existing.ManagerEmployeeId;
        await unitOfWork.Branches.UpdateAsync(updated);
        await AuditAsync(AuditActions.BranchUpdated, branchId, new { updated.Name, updated.City });
        return ToDto(updated);
    }

    public async Task DeleteAsync(int branchId)
    {
        await RequireBranchAsync(branchId);
        if (await unitOfWork.Branches.HasDependentsAsync(branchId))
            throw new ConflictException("BRANCH_HAS_DEPENDENTS", "A branch with employees or vehicles cannot be deleted.");
        await unitOfWork.Branches.DeleteAsync(branchId);
        await AuditAsync(AuditActions.BranchDeleted, branchId, new { Result = "deleted" });
    }

    public async Task SetManagerAsync(int branchId, int? employeeId)
    {
        var branch = await RequireBranchAsync(branchId);
        Employee? next = null;
        if (employeeId.HasValue)
        {
            next = await unitOfWork.Employees.GetByIdAsync(employeeId.Value)
                ?? throw new NotFoundException("Employee was not found.");
            if (next.BranchId != branchId)
                throw new ConflictException("MANAGER_BRANCH_MISMATCH", "The manager must belong to the branch.");
            var managerUser = await unitOfWork.Users.GetByIdAsync(next.UserId)
                ?? throw new NotFoundException("Employee user was not found.");
            if (!managerUser.IsActive)
                throw new ConflictException("INACTIVE_BRANCH_MANAGER", "An inactive user cannot be assigned as branch manager.");
        }

        await unitOfWork.BeginTransactionAsync();
        try
        {
            if (branch.ManagerEmployeeId is int oldId && oldId != employeeId)
                await SetStaffRoleAsync(oldId, RoleNames.Employee);
            if (next is not null)
                await SetStaffRoleAsync(next.EmployeeId, RoleNames.BranchManager);
            await unitOfWork.Branches.SetManagerAsync(branchId, employeeId);
            await unitOfWork.CommitAsync();
        }
        catch
        {
            await unitOfWork.RollbackAsync();
            throw;
        }

        await AuditAsync(AuditActions.BranchManagerChanged, branchId, new { EmployeeId = employeeId });
    }

    private async Task SetStaffRoleAsync(int employeeId, string selectedRole)
    {
        var employee = await unitOfWork.Employees.GetByIdAsync(employeeId) ?? throw new NotFoundException("Employee was not found.");
        var otherRole = selectedRole == RoleNames.Employee ? RoleNames.BranchManager : RoleNames.Employee;
        var selectedRoleId = await unitOfWork.Users.GetRoleIdByNameAsync(selectedRole) ?? throw new InvalidOperationException($"Role {selectedRole} is not seeded.");
        var otherRoleId = await unitOfWork.Users.GetRoleIdByNameAsync(otherRole) ?? throw new InvalidOperationException($"Role {otherRole} is not seeded.");
        await unitOfWork.Users.RemoveRoleAsync(employee.UserId, otherRoleId);
        await unitOfWork.Users.AssignRoleAsync(employee.UserId, selectedRoleId);
        await unitOfWork.RefreshTokens.RevokeAllForUserAsync(employee.UserId, dateTimeProvider.UtcNow);
    }

    private async Task<Branch> RequireBranchAsync(int id) =>
        await unitOfWork.Branches.GetByIdAsync(id) ?? throw new NotFoundException("Branch was not found.");

    private Task AuditAsync(string action, int branchId, object value) => auditLogger.LogAsync(
        action, "Branches", branchId.ToString(CultureInfo.InvariantCulture), null, value, currentUser.UserId);

    private static Branch Map(CreateBranchRequest request) => new() { Name = request.Name, Address = request.Address, City = request.City, Phone = request.Phone };
    
    private static BranchDto ToDto(Branch branch) => new(branch.BranchId, branch.Name, branch.Address, branch.City, branch.Phone, branch.ManagerEmployeeId);


}
