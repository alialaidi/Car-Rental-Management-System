using _CarRental.Application.Constants.PolicyNames;
using _CarRental.Domain.Constants;
using Microsoft.AspNetCore.Authorization;

namespace _CarRental.API.Extensions;

public static class AuthorizationExtensions
{
    public static IServiceCollection AddAppAuthorization(this IServiceCollection services)
    {
        services.AddAuthorization(options =>
        {
            options.FallbackPolicy = new AuthorizationPolicyBuilder()
                .RequireAuthenticatedUser()
                .Build();

            options.AddPolicy(Policies.ManageCars, policy =>
                policy.RequireRole(RoleNames.Admin, RoleNames.BranchManager));
            options.AddPolicy(Policies.ManageEmployees, policy =>
                policy.RequireRole(RoleNames.Admin, RoleNames.BranchManager));
            options.AddPolicy(Policies.ViewReports, policy =>
                policy.RequireRole(RoleNames.Admin, RoleNames.BranchManager));
            options.AddPolicy(Policies.ProcessPayments, policy =>
                policy.RequireRole(RoleNames.Admin, RoleNames.BranchManager, RoleNames.Employee));
        });

        return services;
    }
}
