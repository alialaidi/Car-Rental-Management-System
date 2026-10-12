using Microsoft.Extensions.DependencyInjection;
using _CarRental.Application.Common.Interfaces;
using _CarRental.Application.Services;
using _CarRental.Application.Validators;
using _CarRental.Application.Common.Security;

namespace _CarRental.Application;

public static class DependencyInjection
{
    public static IServiceCollection AddApplication(this IServiceCollection services)
    {
        services.AddScoped<IAuthService, AuthService>();
        services.AddScoped<RegisterRequestValidator>();
        services.AddScoped<LoginRequestValidator>();
        services.AddScoped<OwnershipGuard>();
        services.AddScoped<IBranchService, BranchService>();
        services.AddScoped<IEmployeeService, EmployeeService>();
        services.AddScoped<IUserAdminService, UserAdminService>();
        services.AddScoped<IVehicleService, VehicleService>();
        services.AddScoped<IVehicleQueryService, VehicleQueryService>();
        services.AddScoped<VehicleSearchRequestValidator>();
        services.AddScoped<ILookupService, LookupService>();
        services.AddScoped<IReservationService, ReservationService>();
        services.AddScoped<CreateReservationRequestValidator>();
        services.AddScoped<IRentalContractService, RentalContractService>();
        services.AddScoped<CreateContractRequestValidator>();
        services.AddScoped<ReturnVehicleRequestValidator>();
        services.AddScoped<IPaymentService, PaymentService>();
        services.AddScoped<CreatePaymentRequestValidator>();
        return services;
    }
}
