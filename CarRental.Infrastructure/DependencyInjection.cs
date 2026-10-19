using _CarRental.Application.Common.Interfaces;
using _CarRental.Application.Common.Models;
using _CarRental.Domain.Interfaces.Repositories;
using _CarRental.Infrastructure.Persistence;
using _CarRental.Infrastructure.Auditing;
using _CarRental.Infrastructure.Persistence.Repositories;
using _CarRental.Infrastructure.Persistence.UnitOfWork;
using _CarRental.Infrastructure.Identity;
using _CarRental.Infrastructure.Logging;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace _CarRental.Infrastructure;

public static class DependencyInjection
{
    public static IServiceCollection AddInfrastructure(this IServiceCollection services, IConfiguration configuration)
    {
        ArgumentNullException.ThrowIfNull(configuration);
        var connectionString = configuration.GetConnectionString(DatabaseOptions.ConnectionStringName);
        var jwtOptions = new JwtOptions
        {
            Issuer = configuration[$"{JwtOptions.SectionName}:Issuer"] ?? string.Empty,
            Audience = configuration[$"{JwtOptions.SectionName}:Audience"] ?? string.Empty,
            SigningKey = configuration[$"{JwtOptions.SectionName}:SigningKey"] ?? string.Empty,
            AccessTokenMinutes = int.TryParse(configuration[$"{JwtOptions.SectionName}:AccessTokenMinutes"], out var accessTokenMinutes) ? accessTokenMinutes : 15,
            RefreshTokenDays = int.TryParse(configuration[$"{JwtOptions.SectionName}:RefreshTokenDays"], out var refreshTokenDays) ? refreshTokenDays : 7
        };
        jwtOptions.Validate();

        services.AddSingleton(new DatabaseOptions(connectionString));
        services.AddSingleton(jwtOptions);
        services.AddSingleton<IDateTimeProvider, SystemDateTimeProvider>();
        services.AddSingleton<IPasswordHasher, BcryptPasswordHasher>();
        services.AddSingleton<IJwtService, JwtService>();
        services.AddScoped<IDbConnectionFactory, SqlConnectionFactory>();
        services.AddScoped<IUnitOfWork, UnitOfWork>();
        services.AddScoped<IAuditLogRepository, AuditLogRepository>();
        services.AddScoped<IAuditLogger, AuditLogger>();
        var monitoring = configuration.GetSection("Monitoring:Alerts");
        var serverErrorThreshold = int.TryParse(monitoring["ServerErrors:Threshold"], out var errors) ? errors : 5;
        var serverErrorWindowSeconds = int.TryParse(monitoring["ServerErrors:WindowSeconds"], out var errorWindow) ? errorWindow : 60;
        var failedLoginThreshold = int.TryParse(monitoring["FailedLogins:ThresholdPerSource"], out var logins) ? logins : 5;
        var failedLoginWindowSeconds = int.TryParse(monitoring["FailedLogins:WindowSeconds"], out var loginWindow) ? loginWindow : 300;
        services.AddSingleton<IAlertNotifier>(provider => new LogAlertNotifier(
            provider.GetRequiredService<ILogger<LogAlertNotifier>>(), serverErrorThreshold,
            TimeSpan.FromSeconds(Math.Clamp(serverErrorWindowSeconds, 1, 3600)), failedLoginThreshold,
            TimeSpan.FromSeconds(Math.Clamp(failedLoginWindowSeconds, 1, 3600))));
        services.AddScoped<IDatabaseHealthProbe, SqlHealthCheck>();
        return services;
    }
}
