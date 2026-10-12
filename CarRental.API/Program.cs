using _CarRental.API.Middleware;
using _CarRental.Application;
using _CarRental.Application.Common.Interfaces;
using _CarRental.Application.Common.Models;
using _CarRental.API.Extensions;
using _CarRental.Infrastructure;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Mvc;
using Microsoft.IdentityModel.Tokens;
using System.IdentityModel.Tokens.Jwt;
using System.Text;
using System.Threading.RateLimiting;
using Microsoft.AspNetCore.RateLimiting;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();
builder.Services.AddApplication();
builder.Services.AddInfrastructure(builder.Configuration);
builder.Services.AddHttpContextAccessor();
builder.Services.AddScoped<IRequestContext, HttpRequestContext>();
builder.Services.AddScoped<ICurrentUser, CurrentUser>();

var jwtOptions = new JwtOptions
{
    Issuer = builder.Configuration[$"{JwtOptions.SectionName}:Issuer"] ?? string.Empty,
    Audience = builder.Configuration[$"{JwtOptions.SectionName}:Audience"] ?? string.Empty,
    SigningKey = builder.Configuration[$"{JwtOptions.SectionName}:SigningKey"] ?? string.Empty,
    AccessTokenMinutes = int.TryParse(builder.Configuration[$"{JwtOptions.SectionName}:AccessTokenMinutes"], out var accessTokenMinutes) ? accessTokenMinutes : 15,
    RefreshTokenDays = int.TryParse(builder.Configuration[$"{JwtOptions.SectionName}:RefreshTokenDays"], out var refreshTokenDays) ? refreshTokenDays : 7
};

jwtOptions.Validate();
var signingKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwtOptions.SigningKey));
builder.Services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
    .AddJwtBearer(options =>
    {
        options.RequireHttpsMetadata = !builder.Environment.IsDevelopment();
        options.MapInboundClaims = false;
        options.TokenValidationParameters = new TokenValidationParameters
        {
            ValidateIssuer = true,
            ValidIssuer = jwtOptions.Issuer,
            ValidateAudience = true,
            ValidAudience = jwtOptions.Audience,
            ValidateIssuerSigningKey = true,
            IssuerSigningKey = signingKey,
            ValidateLifetime = true,
            RequireExpirationTime = true,
            RequireSignedTokens = true,
            ValidAlgorithms = [SecurityAlgorithms.HmacSha256],
            ClockSkew = TimeSpan.Zero,
            NameClaimType = JwtRegisteredClaimNames.UniqueName,
            RoleClaimType = "role"
        };
        options.Events = new JwtBearerEvents
        {
            OnChallenge = async context =>
            {
                context.HandleResponse();
                await WriteAuthProblemAsync(context.Response, StatusCodes.Status401Unauthorized,
                    "Unauthorized", "A valid bearer token is required.", context.HttpContext.Request.Path,
                    context.HttpContext.TraceIdentifier, context.HttpContext.RequestAborted);
            },
            OnForbidden = async context =>
            {
                await WriteAuthProblemAsync(context.Response, StatusCodes.Status403Forbidden,
                    "Forbidden", "You are not allowed to access this resource.", context.Request.Path,
                    context.HttpContext.TraceIdentifier, context.HttpContext.RequestAborted);
            }
        };
    });


builder.Services.AddAppAuthorization();
var authPermitLimit = int.TryParse(builder.Configuration["RateLimiting:Auth:PermitLimit"], out var configuredPermitLimit) ? configuredPermitLimit : 5;
var authWindowSeconds = int.TryParse(builder.Configuration["RateLimiting:Auth:WindowSeconds"], out var configuredWindowSeconds) ? configuredWindowSeconds : 60;


builder.Services.AddRateLimiter(options =>
{
    options.RejectionStatusCode = StatusCodes.Status429TooManyRequests;
    options.AddPolicy("auth", httpContext => RateLimitPartition.GetFixedWindowLimiter(
        httpContext.Connection.RemoteIpAddress?.ToString() ?? "unknown",
        _ => new FixedWindowRateLimiterOptions
        {
            PermitLimit = authPermitLimit,
            Window = TimeSpan.FromSeconds(authWindowSeconds),
            QueueProcessingOrder = QueueProcessingOrder.OldestFirst,
            QueueLimit = 0,
            AutoReplenishment = true
        }));
    options.OnRejected = async (context, cancellationToken) =>
    {
        if (context.Lease.TryGetMetadata(MetadataName.RetryAfter, out var retryAfter))
            context.HttpContext.Response.Headers.RetryAfter = Math.Ceiling(retryAfter.TotalSeconds).ToString();
        await WriteAuthProblemAsync(context.HttpContext.Response, StatusCodes.Status429TooManyRequests,
            "Too many requests", "Try again later.", context.HttpContext.Request.Path,
            context.HttpContext.TraceIdentifier, cancellationToken);
    };
});


builder.Services.AddCors(options => options.AddDefaultPolicy(policy =>
    policy.WithOrigins(builder.Configuration.GetSection("Cors:AllowedOrigins").Get<string[]>() ?? [])
        .AllowAnyHeader()
        .AllowAnyMethod()));


var app = builder.Build();

app.UseMiddleware<GlobalExceptionMiddleware>();
app.MapGet("/health", () => Results.Ok(new { status = "ok" })).AllowAnonymous();
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseHttpsRedirection();
app.UseRouting();
app.UseCors();
app.UseRateLimiter();
app.UseAuthentication();
app.UseAuthorization();

app.MapControllers();

app.Run();

static Task WriteAuthProblemAsync(
    HttpResponse response,
    int status,
    string title,
    string detail,
    PathString path,
    string traceId,
    CancellationToken cancellationToken)
{
    response.StatusCode = status;
    response.ContentType = "application/problem+json";
    var problem = new ProblemDetails { Status = status, Title = title, Detail = detail, Instance = path.Value };
    problem.Extensions["traceId"] = traceId;
    return response.WriteAsJsonAsync(problem, options: null, contentType: "application/problem+json", cancellationToken: cancellationToken);
}

public partial class Program { }
