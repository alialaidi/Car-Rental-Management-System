using _CarRental.Application.Common.Interfaces;
using _CarRental.Application.Common.Security;
using _CarRental.Domain.Constants;
using _CarRental.Domain.Exceptions;
using Xunit;

namespace _CarRental.Tests;

public sealed class OwnershipGuardTests
{
    [Fact]
    public async Task CustomerCanAccessTheirOwnResource()
    {
        var guard = new OwnershipGuard(new TestCurrentUser(21, [RoleNames.Customer]));

        await guard.EnsureCustomerAsync(21);
    }

    [Fact]
    public async Task CustomerCannotAccessAnotherCustomersResource()
    {
        var guard = new OwnershipGuard(new TestCurrentUser(21, [RoleNames.Customer]));

        await Assert.ThrowsAsync<ForbiddenException>(() => guard.EnsureCustomerAsync(22));
    }

    [Fact]
    public async Task AdminCanAccessCustomerResources()
    {
        var guard = new OwnershipGuard(new TestCurrentUser(1, [RoleNames.Admin]));

        await guard.EnsureCustomerAsync(22);
    }

    private sealed class TestCurrentUser(int userId, IReadOnlyCollection<string> roles) : ICurrentUser
    {
        public int UserId => userId;
        public IReadOnlyCollection<string> Roles => roles;
        public bool IsInRole(string role) => roles.Contains(role, StringComparer.Ordinal);
        public Task<int> GetCustomerIdAsync() => Task.FromResult(21);
        public Task<int> GetEmployeeIdAsync() => Task.FromResult(5);
        public Task<int> GetBranchIdAsync() => Task.FromResult(3);
    }
}
