using _CarRental.API;
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
using Microsoft.OpenApi.Models;
using System.Reflection;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(options =>
{
    options.AddSecurityDefinition("Bearer", new OpenApiSecurityScheme
    {
        Name = "Authorization",
        Description = "Enter a JWT access token. The 'Bearer ' prefix is added automatically.",
        In = ParameterLocation.Header,
        Type = SecuritySchemeType.Http,
        Scheme = "bearer",
        BearerFormat = "JWT"
    });
    options.OperationFilter<SwaggerAuthorizeOperationFilter>();
    var xmlPath = Path.Combine(AppContext.BaseDirectory, $"{Assembly.GetExecutingAssembly().GetName().Name}.xml");
    if (File.Exists(xmlPath)) options.IncludeXmlComments(xmlPath);
});
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
var authPermitLimit = Math.Clamp(int.TryParse(builder.Configuration["RateLimiting:Auth:PermitLimit"], out var configuredPermitLimit) ? configuredPermitLimit : 5, 1, 1000);
var authWindowSeconds = Math.Clamp(int.TryParse(builder.Configuration["RateLimiting:Auth:WindowSeconds"], out var configuredWindowSeconds) ? configuredWindowSeconds : 60, 1, 3600);
var paymentPermitLimit = Math.Clamp(int.TryParse(builder.Configuration["RateLimiting:Payment:PermitLimit"], out var configuredPaymentLimit) ? configuredPaymentLimit : 10, 1, 1000);
var paymentWindowSeconds = Math.Clamp(int.TryParse(builder.Configuration["RateLimiting:Payment:WindowSeconds"], out var configuredPaymentWindow) ? configuredPaymentWindow : 60, 1, 3600);


builder.Services.AddRateLimiter(options =>
{
    options.RejectionStatusCode = StatusCodes.Status429TooManyRequests;
    options.AddPolicy("auth", httpContext => CreateIpLimiter(httpContext, authPermitLimit, authWindowSeconds));
    options.AddPolicy("payment", httpContext => CreateIpLimiter(httpContext, paymentPermitLimit, paymentWindowSeconds));
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

app.Use(async (context, next) =>
{
    context.Response.OnStarting(() =>
    {
        context.Response.Headers["X-Content-Type-Options"] = "nosniff";
        context.Response.Headers["X-Frame-Options"] = "DENY";
        context.Response.Headers["Referrer-Policy"] = "strict-origin-when-cross-origin";
        context.Response.Headers["Permissions-Policy"] = "camera=(), microphone=(), geolocation=()";
        return Task.CompletedTask;
    });
    await next();
});
app.UseMiddleware<RequestLoggingMiddleware>();
app.UseMiddleware<GlobalExceptionMiddleware>();
app.MapGet("/health/live", () => Results.Text("Healthy")).AllowAnonymous();
app.MapGet("/health/ready", async (IDatabaseHealthProbe database, CancellationToken cancellationToken) =>
    await database.IsHealthyAsync(cancellationToken)
        ? Results.Text("Healthy")
        : Results.Text("Unavailable", statusCode: StatusCodes.Status503ServiceUnavailable)).AllowAnonymous();
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

if (!app.Environment.IsDevelopment()) app.UseHsts();
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

static RateLimitPartition<string> CreateIpLimiter(HttpContext context, int permitLimit, int windowSeconds) =>
    RateLimitPartition.GetFixedWindowLimiter(context.Connection.RemoteIpAddress?.ToString() ?? "unknown", _ => new FixedWindowRateLimiterOptions
    {
        PermitLimit = permitLimit,
        Window = TimeSpan.FromSeconds(windowSeconds),
        QueueProcessingOrder = QueueProcessingOrder.OldestFirst,
        QueueLimit = 0,
        AutoReplenishment = true
    });

public partial class Program { }
