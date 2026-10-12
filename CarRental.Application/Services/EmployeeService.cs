using System.Globalization;
using _CarRental.Application.Common.Interfaces;
using _CarRental.Application.Common.Models;
using _CarRental.Application.Common.Security;
using _CarRental.Application.DTOs.Employees;
using _CarRental.Application.Validators;
using _CarRental.Domain.Constants;
using _CarRental.Domain.Entities;
using _CarRental.Domain.Exceptions;
using _CarRental.Domain.Interfaces.Repositories;

namespace _CarRental.Application.Services;

public sealed class EmployeeService(
    IUnitOfWork unitOfWork,
    ICurrentUser currentUser,
    IPasswordHasher passwordHasher,
    IDateTimeProvider dateTimeProvider,
    IAuditLogger auditLogger) : IEmployeeService
{
    public async Task<EmployeeDto> CreateAsync(CreateEmployeeRequest request)
    {
        StaffRequestValidator.Validate(request, dateTimeProvider.UtcNow);
        await EnsureBranchScopeAsync(request.BranchId);
        if (request.IsBranchManager && !currentUser.IsInRole(RoleNames.Admin))
            throw new ForbiddenException("Only an administrator can create a branch manager.");

        var branch = await unitOfWork.Branches.GetByIdAsync(request.BranchId)
            ?? throw new NotFoundException("Branch was not found.");
        var roleName = request.IsBranchManager ? RoleNames.BranchManager : RoleNames.Employee;
        var roleId = await unitOfWork.Users.GetRoleIdByNameAsync(roleName)
            ?? throw new InvalidOperationException($"Role {roleName} is not seeded.");
        var passwordHash = passwordHasher.Hash(request.Password);
        var userId = 0;
        var employeeId = 0;
        await unitOfWork.BeginTransactionAsync();
        try
        {
            userId = await unitOfWork.Users.CreateAsync(new User
            {
                Username = request.Username,
                Email = request.Email,
                PasswordHash = passwordHash,
                PhoneNumber = request.PhoneNumber,
                IsActive = true
            });
            await unitOfWork.Users.AssignRoleAsync(userId, roleId);
            employeeId = await unitOfWork.Employees.CreateAsync(new Employee
            {
                UserId = userId, BranchId = request.BranchId, Position = request.Position, HireDate = request.HireDate.Date
            });
            if (request.IsBranchManager)
            {
                if (branch.ManagerEmployeeId is int oldManagerId)
                {
                    var oldManager = await unitOfWork.Employees.GetByIdAsync(oldManagerId)
                        ?? throw new NotFoundException("The branch manager profile was not found.");
                    var managerRole = await unitOfWork.Users.GetRoleIdByNameAsync(RoleNames.BranchManager)
                        ?? throw new InvalidOperationException("The BranchManager role has not been seeded.");
                    var employeeRole = await unitOfWork.Users.GetRoleIdByNameAsync(RoleNames.Employee)
                        ?? throw new InvalidOperationException("The Employee role has not been seeded.");
                    await unitOfWork.Users.RemoveRoleAsync(oldManager.UserId, managerRole);
                    await unitOfWork.Users.AssignRoleAsync(oldManager.UserId, employeeRole);
                }
                await unitOfWork.Branches.SetManagerAsync(request.BranchId, employeeId);
            }
            await unitOfWork.CommitAsync();
        }
        catch
        {
            await unitOfWork.RollbackAsync();
            throw;
        }

        await auditLogger.LogAsync(AuditActions.EmployeeCreated, "Employees", employeeId.ToString(CultureInfo.InvariantCulture), null,
            new { UserId = userId, request.BranchId, Position = request.Position }, currentUser.UserId);
        return new EmployeeDto(employeeId, userId, request.Username, request.Email, request.PhoneNumber, true,
            request.BranchId, request.Position, request.HireDate.Date, [roleName]);
    }

    public async Task<EmployeeDto> GetAsync(int employeeId)
    {
        var employee = await RequireEmployeeAsync(employeeId);
        await EnsureBranchScopeAsync(employee.BranchId);
        return await ToDtoAsync(employee);
    }

    public async Task<PagedResult<EmployeeDto>> ListByBranchAsync(int branchId, PagedRequest request)
    {
        ArgumentNullException.ThrowIfNull(request);
        await EnsureBranchScopeAsync(branchId);
        if (await unitOfWork.Branches.GetByIdAsync(branchId) is null)
            throw new NotFoundException("Branch was not found.");
        var employees = await unitOfWork.Employees.ListByBranchAsync(branchId, request.Page, request.PageSize);
        var items = new List<EmployeeDto>(employees.Count);
        foreach (var employee in employees) items.Add(await ToDtoAsync(employee));
        return new PagedResult<EmployeeDto>(items, request.Page, request.PageSize, await unitOfWork.Employees.GetCountByBranchAsync(branchId));
    }

    public async Task<EmployeeDto> UpdateAsync(int employeeId, UpdateEmployeeRequest request)
    {
        StaffRequestValidator.Validate(request, dateTimeProvider.UtcNow);
        var employee = await RequireEmployeeAsync(employeeId);
        await EnsureBranchScopeAsync(employee.BranchId);
        await EnsureBranchScopeAsync(request.BranchId);
        if (employee.BranchId != request.BranchId)
        {
            var currentBranch = await unitOfWork.Branches.GetByIdAsync(employee.BranchId);
            if (currentBranch?.ManagerEmployeeId == employee.EmployeeId)
                throw new ConflictException("BRANCH_MANAGER_REASSIGNMENT_REQUIRED", "Assign a different branch manager before moving this employee.");
        }
        if (await unitOfWork.Branches.GetByIdAsync(request.BranchId) is null)
            throw new NotFoundException("Branch was not found.");
        var oldValue = new { employee.BranchId, employee.Position, employee.HireDate };
        employee.BranchId = request.BranchId;
        employee.Position = request.Position;
        employee.HireDate = request.HireDate.Date;
        await unitOfWork.Employees.UpdateAsync(employee);
        await auditLogger.LogAsync(AuditActions.EmployeeUpdated, "Employees", employeeId.ToString(CultureInfo.InvariantCulture),
            oldValue, new { employee.BranchId, employee.Position, employee.HireDate }, currentUser.UserId);
        return await ToDtoAsync(employee);
    }

    private async Task EnsureBranchScopeAsync(int branchId)
    {
        if (currentUser.IsInRole(RoleNames.Admin)) return;
        if (!currentUser.IsInRole(RoleNames.BranchManager))
            throw new ForbiddenException("Only administrators and branch managers can manage employees.");
        await new OwnershipGuard(currentUser).EnsureBranchAsync(branchId);
    }

    private async Task<Employee> RequireEmployeeAsync(int id) =>
        await unitOfWork.Employees.GetByIdAsync(id) ?? throw new NotFoundException("Employee was not found.");

    private async Task<EmployeeDto> ToDtoAsync(Employee employee)
    {
        var user = await unitOfWork.Users.GetByIdAsync(employee.UserId) ?? throw new NotFoundException("Employee user was not found.");
        var roles = await unitOfWork.Users.GetRoleNamesAsync(user.UserId);
        return new EmployeeDto(employee.EmployeeId, user.UserId, user.Username, user.Email, user.PhoneNumber, user.IsActive,
            employee.BranchId, employee.Position, employee.HireDate, roles);
    }


}
