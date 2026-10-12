using System.Globalization;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using _CarRental.Application.Common.Interfaces;
using _CarRental.Domain.Constants;
using _CarRental.Domain.Exceptions;
using _CarRental.Domain.Interfaces.Repositories;

namespace _CarRental.API.Extensions;

public sealed class CurrentUser(IHttpContextAccessor httpContextAccessor, IUnitOfWork unitOfWork) : ICurrentUser
{
    private Task<int>? _customerIdTask;
    private Task<int>? _employeeIdTask;
    private Task<int>? _branchIdTask;

    private ClaimsPrincipal Principal => httpContextAccessor.HttpContext?.User
        ?? throw new UnauthorizedException("An authenticated user is required.");

    public int UserId
    {
        get
        {
            var value = Principal.FindFirst(JwtRegisteredClaimNames.Sub)?.Value;
            return int.TryParse(value, NumberStyles.None, CultureInfo.InvariantCulture, out var userId) && userId > 0
                ? userId
                : throw new UnauthorizedException("The bearer token does not contain a valid user id.");
        }
    }

    public IReadOnlyCollection<string> Roles => Principal.FindAll("role")
        .Select(claim => claim.Value)
        .Distinct(StringComparer.Ordinal)
        .ToArray();

    public bool IsInRole(string role) => Roles.Contains(role, StringComparer.Ordinal);

    public Task<int> GetCustomerIdAsync()
    {
        if (!IsInRole(RoleNames.Customer))
            throw new ForbiddenException("The current user is not a customer.");

        return _customerIdTask ??= ResolveCustomerIdAsync(UserId);
    }

    public Task<int> GetEmployeeIdAsync()
    {
        if (!IsInRole(RoleNames.Employee) && !IsInRole(RoleNames.BranchManager))
            throw new ForbiddenException("The current user is not an employee.");

        return _employeeIdTask ??= ResolveEmployeeIdAsync(UserId);
    }

    public Task<int> GetBranchIdAsync() => _branchIdTask ??= ResolveBranchIdAsync();

    private async Task<int> ResolveCustomerIdAsync(int userId)
    {
        var customer = await unitOfWork.Customers.GetByUserIdAsync(userId);
        return customer?.CustomerId
            ?? throw new NotFoundException("The current user does not have a customer profile.");
    }

    private async Task<int> ResolveEmployeeIdAsync(int userId)
    {
        var employee = await unitOfWork.Employees.GetByUserIdAsync(userId);
        return employee?.EmployeeId
            ?? throw new NotFoundException("The current user does not have an employee profile.");
    }

    private async Task<int> ResolveBranchIdAsync()
    {
        if (IsInRole(RoleNames.Admin))
            throw new ForbiddenException("Administrators do not have a branch scope.");

        var employeeId = await GetEmployeeIdAsync();
        var employee = await unitOfWork.Employees.GetByIdAsync(employeeId);
        return employee?.BranchId
            ?? throw new NotFoundException("The current employee does not have a branch.");
    }
}
