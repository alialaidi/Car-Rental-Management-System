# Car Rental System API

ASP.NET Core 8 Web API for the Car Rental System. It provides customer authentication and reservations, staff rental operations, payments, fleet maintenance, reviews, reporting, audit queries, and operational health endpoints.

## Requirements

- .NET 8 SDK
- SQL Server for API operations and SQL integration tests
- SQL Server Management Studio (SSMS) or `sqlcmd` for database scripts

## Configure and run

Set local connection and JWT settings with User Secrets, or provide them through the environment. The JWT signing key must contain at least 32 UTF-8 bytes.

```powershell
cd CarRental.API
dotnet user-secrets set "ConnectionStrings:CarRentalDb" "Server=localhost;Database=CarRentalDB;Trusted_Connection=True;TrustServerCertificate=True"
dotnet user-secrets set "Jwt:SigningKey" "<local key with at least 32 bytes>"
dotnet run
```

The Development launch profiles open Swagger at `/swagger`. Use Swagger's **Authorize** button with the access token returned by login; Swagger adds the Bearer scheme. For deployed environments, set `ConnectionStrings__CarRentalDb`, `Jwt__SigningKey`, and other settings using the hosting platform's environment configuration.

Set allowed browser origins under `Cors:AllowedOrigins` in the environment-specific configuration. Example:

```json
{
  "Cors": {
    "AllowedOrigins": [ "http://localhost:5173" ]
  }
}
```

## Database setup

Use a disposable database for the initial setup. `CarRental.Database/Scripts/Tables/RunAll.sql` drops and recreates the project tables; do not run it against data you need. In SSMS, enable **Query > SQLCMD Mode**, open that script from its directory, and execute it. Then execute `CarRental.Database/Scripts/StoredProcedures/RunAll.sql` to create the stored procedures. Seed the roles/lookups/admin using `CarRental.Database/Scripts/Seed/RunSeed.sql`; before running it, set the SQLCMD variable `AdminPasswordHash` to a BCrypt hash generated locally (in SSMS use **Query > SQLCMD Mode > Query Variables**). Never paste a plain password into a seed script or commit production secrets.

The API expects a SQL Server database named `CarRentalDB` and a configured `CarRentalDb` connection string. It does not apply schema changes automatically.

## Operations and security

- `GET /health/live` reports that the API process is running and does not contact SQL Server.
- `GET /health/ready` checks SQL Server and returns 503 when the database cannot be reached. The check has a three-second timeout and returns no connection details.
- Requests are logged with method, path, response status, duration, and a correlation ID. Request bodies and query strings are not logged.
- Authentication and payment write endpoints use IP-partitioned fixed-window rate limits. Tune `RateLimiting:Auth` and `RateLimiting:Payment` per environment.
- Alert thresholds are configured under `Monitoring:Alerts`. Alerts are written to the application log; failed-login alerts do not include source IP addresses.
- Production responses include security headers and HSTS. HTTPS termination and forwarded-header configuration should match the deployment topology.

## Build and tests

From the repository root:

```powershell
dotnet build CarRentalSystem.sln
dotnet test CarRental.Tests/CarRental.Tests.csproj
```

Unit and API tests run without a database. SQL integration tests are skipped unless `CAR_RENTAL_TEST_CONNECTION_STRING` points to a disposable SQL Server database that has the schema, procedures, and seed data installed. The suite includes a full API rental scenario, which requires that same test database.

Configure allowed origins, connection strings, signing keys, and alert/rate-limit thresholds separately for each environment. Do not expose Swagger in production unless the deployment explicitly requires it.
