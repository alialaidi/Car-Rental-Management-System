using System.Reflection;
using _CarRental.API.Controllers;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Routing;
using Microsoft.AspNetCore.RateLimiting;
using Xunit;

namespace _CarRental.Tests.Security;

public sealed class AuthorizationCoverageTests
{
    [Fact]
    public void EveryControllerActionDeclaresAuthorizationOrAnonymousAccess()
    {
        var controllers = typeof(AuthController).Assembly.GetTypes()
            .Where(type => !type.IsAbstract && typeof(ControllerBase).IsAssignableFrom(type));
        var missing = new List<string>();

        foreach (var controller in controllers)
        {
            var classAttributes = controller.GetCustomAttributes(inherit: true);
            var classAuthorizes = classAttributes.OfType<IAuthorizeData>().Any();
            var classAllowsAnonymous = classAttributes.OfType<IAllowAnonymous>().Any();
            foreach (var method in controller.GetMethods(BindingFlags.Instance | BindingFlags.Public | BindingFlags.DeclaredOnly))
            {
                if (!method.GetCustomAttributes(inherit: true).OfType<HttpMethodAttribute>().Any()) continue;
                var actionAttributes = method.GetCustomAttributes(inherit: true);
                var authorized = classAuthorizes || actionAttributes.OfType<IAuthorizeData>().Any();
                var anonymous = classAllowsAnonymous || actionAttributes.OfType<IAllowAnonymous>().Any();
                if (!authorized && !anonymous) missing.Add($"{controller.Name}.{method.Name}");
            }
        }

        Assert.Empty(missing);
    }

    [Fact]
    public void EveryPaymentMutationUsesPaymentRateLimitPolicy()
    {
        var actions = typeof(PaymentsController).GetMethods(BindingFlags.Instance | BindingFlags.Public | BindingFlags.DeclaredOnly)
            .Where(method => method.GetCustomAttributes(inherit: true).OfType<HttpPostAttribute>().Any());

        Assert.NotEmpty(actions);
        Assert.All(actions, action => Assert.Contains(action.GetCustomAttributes(inherit: true)
            .OfType<EnableRateLimitingAttribute>(), limit => limit.PolicyName == "payment"));
    }
}
