using _CarRental.Application.Common.Interfaces;
using _CarRental.Domain.Constants;
using _CarRental.Domain.Exceptions;

namespace _CarRental.Application.Common.Security;

public sealed class OwnershipGuard(ICurrentUser currentUser)
{
    public async Task EnsureCustomerAsync(int ownerCustomerId)
    {
        if (ownerCustomerId <= 0)
            throw new ArgumentOutOfRangeException(nameof(ownerCustomerId));

        if (currentUser.IsInRole(RoleNames.Admin))
            return;

        var currentCustomerId = await currentUser.GetCustomerIdAsync();
        if (currentCustomerId != ownerCustomerId)
            throw new ForbiddenException("You are not allowed to access this customer's resource.");
    }

    public async Task EnsureBranchAsync(int branchId)
    {
        if (branchId <= 0)
            throw new ArgumentOutOfRangeException(nameof(branchId));

        if (currentUser.IsInRole(RoleNames.Admin))
            return;

        if (await currentUser.GetBranchIdAsync() != branchId)
            throw new ForbiddenException("You are not allowed to access this branch.");
    }
}
