# Car Rental System — Backend API

An educational car-rental backend built with **ASP.NET Core 8 Web API** and **SQL Server**. It demonstrates how a multi-role business API can support customers, branch employees, managers, and administrators across the rental lifecycle, while keeping HTTP concerns, business rules, and data access in separate layers.

This repository contains the backend and SQL Server scripts. It does not include a frontend application or a payment gateway integration. Payment endpoints record and manage payment records in the project database; they do not charge real cards.

## What the system does

- Registers customers and authenticates users with JWT access tokens and rotating refresh tokens.
- Provides a vehicle catalog, filters and searches vehicles, and manages vehicle photos.
- Lets customers request, view, and cancel reservations that belong to them.
- Lets staff review reservations for their branch and create and close rental contracts.
- Records payments, balances, refunds, and payment status changes.
- Tracks vehicle maintenance, customer reviews, staff, branches, and lookup values.
- Provides operational reports and an administrator-only audit log query.

## Rental lifecycle

1. A customer registers or logs in and receives an access token and refresh token.
2. The customer searches the catalog and creates a reservation for an available vehicle.
3. Branch staff confirms or rejects the reservation. Staff access is scoped to the assigned branch; administrators can review across branches.
4. Staff create a rental contract when handing over the vehicle.
5. Payments are recorded against the contract. Staff record the vehicle return and close the contract.
6. The customer can review a completed contract. The review is tied to the customer’s own rental.

The API validates important business rules in the application layer and uses SQL transactions and stored procedures for critical database operations.

## Solution structure

| Project | Responsibility |
|---|---|
| `CarRental.API` | ASP.NET Core host, controllers, JWT authentication, authorization, middleware, CORS, rate limits, Swagger, health endpoints |
| `CarRental.Application` | Use cases, service interfaces, DTOs, validators, business rules, ownership and branch-scoping checks |
| `CarRental.Domain` | Core entities, enums, repository contracts, domain exceptions, and business concepts |
| `CarRental.Infrastructure` | ADO.NET SQL Server access, stored-procedure repositories, unit of work, BCrypt password hashing, JWT creation, auditing, and logging |
| `CarRental.Database` | SQL Server table, function, stored-procedure, and seed scripts |
| `CarRental.Tests` | Unit, API, authorization, security, and optional SQL integration tests |

### Data access

The backend uses **ADO.NET and SQL Server stored procedures**; it does not use Entity Framework. Repositories call stored procedures through a shared SQL executor and unit of work. The API does not create or migrate the schema automatically: set up the database using the scripts in `CarRental.Database`.

## Requirements

- .NET 8 SDK
- SQL Server (local SQL Server, SQL Server Developer Edition, or another compatible instance)
- SQL Server Management Studio (SSMS) with SQLCMD Mode, or `sqlcmd`

## Set up a local database

> **Destructive setup warning:** `Tables/RunAll.sql` recreates project tables. Use a disposable/empty database. Do not run it against data you need.

1. In SSMS, enable **Query → SQLCMD Mode**.
2. Open and execute `CarRental.Database/Scripts/Tables/RunAll.sql` from its own directory.
3. Open and execute `CarRental.Database/Scripts/StoredProcedures/RunAll.sql` from its own directory.
4. Open `CarRental.Database/Scripts/Seed/RunSeed.sql`. Set the SQLCMD variable `AdminPasswordHash` to a BCrypt hash, then execute the script. The seed adds roles, lookup values, the baseline `admin@carrental.local` account, and educational demo data.

`RunSeed.sql` is idempotent for its sample records and does not delete rows. It is separate from `Tables/RunAll.sql`, which is destructive.

### Educational demo accounts

The demo seed creates these local-only accounts, all with the password `Demo123!`:

| Username | Role | Email |
|---|---|---|
| `demo.manager` | BranchManager | `manager@carrental.local` |
| `demo.employee` | Employee | `employee@carrental.local` |
| `demo.customer` | Customer | `customer@carrental.local` |
| `demo.customer2` | Customer | `customer2@carrental.local` |

These credentials are intentionally public for classroom and local demonstrations. **Never use them in production.** The baseline `admin@carrental.local` password is determined by the `AdminPasswordHash` supplied when the seed is run; the hash cannot be converted back into a password.

The sample dataset includes branches, employees, customers, vehicles and images, reservations with different statuses, completed and active contracts, payments, maintenance records, a review, an expired/revoked refresh-token example, and an audit event. Dates and records are illustrative rather than operational business data.

## Configure and run the API

From the repository root, set local values using User Secrets (recommended) or environment variables. Do not commit real connection strings or production secrets.

```powershell
cd CarRental.API
dotnet user-secrets set "ConnectionStrings:CarRentalDb" "Server=localhost;Database=CarRentalDB;Trusted_Connection=True;TrustServerCertificate=True"
dotnet user-secrets set "Jwt:SigningKey" "<a local random key of at least 32 UTF-8 bytes>"
dotnet run
```

The API validates the JWT signing key on startup. Configure allowed browser origins for the frontend; an empty allowlist does not permit cross-origin browser requests.

```powershell
dotnet user-secrets set "Cors:AllowedOrigins:0" "http://localhost:5173"
```

For deployment, use the hosting platform’s secret/environment configuration. Common environment variable names are:

| Setting | Environment variable |
|---|---|
| SQL connection string | `ConnectionStrings__CarRentalDb` |
| JWT signing key | `Jwt__SigningKey` |
| JWT issuer | `Jwt__Issuer` |
| JWT audience | `Jwt__Audience` |
| Browser origin allowlist | `Cors__AllowedOrigins__0` (add numbered entries for additional origins) |

The default Development launch profile opens Swagger at `/swagger`. Use **Authorize** and paste the access token returned by login; Swagger configures the Bearer prefix.

## API overview

Unless stated otherwise, API routes require a valid JWT. The fallback authorization policy requires an authenticated user, with explicit anonymous access for registration, login, refresh, logout, and health checks. Role requirements and resource ownership checks still apply to individual operations.

| Area | Routes |
|---|---|
| Authentication | `POST /api/auth/register`, `/api/auth/login`, `/api/auth/refresh`, `/api/auth/logout` |
| Catalog | `GET /api/catalog/vehicles`, `GET /api/catalog/vehicles/{vehicleId}` |
| Vehicle administration | `/api/vehicles` and `/api/vehicles/{vehicleId}/images` |
| Reservations | `POST /api/reservations`, `GET /api/reservations`, `GET /api/reservations/{reservationId}`, `POST /api/reservations/{reservationId}/cancel` |
| Staff reservation review | `GET /api/staff/reservations`, `POST /api/staff/reservations/{reservationId}/confirm`, `/reject`, `/cancel` |
| Rental contracts | `/api/contracts`, `/api/contracts/{contractId}`, `/api/contracts/{contractId}/return`, `/api/contracts/my` |
| Payments | `/api/contracts/{contractId}/payments`, `/api/contracts/{contractId}/balance`, `/api/payments/my`, `/api/payments/{paymentId}/status`, `/refund` |
| Reviews | `POST /api/contracts/{contractId}/review`, `/api/reviews/{reviewId}`, `GET /api/vehicles/{vehicleId}/reviews` |
| Branches and staff | `/api/branches`, `/api/employees`, `/api/users` |
| Fleet operations | `/api/maintenance`, `/api/lookups`, `/api/vehicles` |
| Reporting and audit | `/api/reports/*`, `GET /api/audit-logs` |

Use Swagger for request/response schemas, query parameters, and per-operation role requirements. Errors use `ProblemDetails` JSON responses; validation and business-rule failures return client errors, authentication failures return `401`, authorization failures return `403`, missing resources return `404`, and rate-limited requests return `429`.

### Authentication example

Register or log in to receive an access token and refresh token:

```http
POST /api/auth/login
Content-Type: application/json

{
  "usernameOrEmail": "demo.customer",
  "password": "Demo123!"
}
```

Send the access token on protected requests:

```http
Authorization: Bearer <access-token>
```

Access tokens are short-lived. Refresh tokens are stored as hashes, rotated on refresh, and revoked on logout; detected reuse revokes that user’s refresh sessions.

## Roles and security

- **Customer:** customer workflows, with reservations, contracts, and reviews restricted to the customer’s own records.
- **Employee:** branch operations, reservation review, contracts, payments, and maintenance within the assigned branch where applicable.
- **BranchManager:** branch management and reporting within the assigned branch where applicable.
- **Admin:** administrative operations and cross-branch access where supported.

The API validates JWT issuer, audience, signature, lifetime, and HMAC-SHA256 algorithm. Passwords use BCrypt. Authorization combines roles/policies with service-level ownership and branch checks. Authentication endpoints and payment write operations use configurable IP-based rate limits. Production responses add security headers and HSTS; HTTPS redirection is enabled.

### Health endpoints

| Endpoint | Meaning | Typical use |
|---|---|---|
| `GET /health/live` | The API process is running. It does not call SQL Server. | Liveness probe: a deployment platform can restart a process that stops responding. |
| `GET /health/ready` | The API can reach its configured SQL Server database. Returns `503` when the database check fails. | Readiness probe: a deployment platform can stop sending traffic to an instance that cannot serve requests. |

Both endpoints are anonymous by design so infrastructure can call them without a user token. A database connection working now does not make readiness redundant: it can fail later, and the readiness response lets the host detect that. The health endpoints are not a replacement for monitoring or application logs.

## Database tables

The schema covers users and roles, refresh tokens, branches, employees, customers, vehicle categories/fuel/transmission lookups, vehicles and images, reservations, rental contracts, payments, maintenance records, reviews, and audit logs. `ContractNumberSequence` is a SQL Server sequence, not a table.

SQL scripts are organized by purpose:

- `Scripts/Tables` — schema creation and destructive reset scripts.
- `Scripts/Functions` — SQL functions used for availability checks.
- `Scripts/StoredProcedures` — database operations consumed by the repositories.
- `Scripts/Seed` — baseline and educational demo records.

## Build and tests

From the repository root:

```powershell
dotnet build CarRentalSystem.sln
dotnet test CarRental.Tests/CarRental.Tests.csproj
```

Unit and API tests run without SQL Server. SQL integration tests are skipped unless `CAR_RENTAL_TEST_CONNECTION_STRING` points to a **disposable test database** with the project schema, stored procedures, and seed data installed. Never point integration tests at a database containing data you need.

## Current scope and deployment notes

- The project is educational and is not a substitute for a production security review.
- Payment operations manage local payment records; no external payment processor is integrated.
- SQL schema setup is manual; the API does not automatically apply SQL scripts.
- Configure HTTPS termination and forwarded headers to match the hosting platform before deployment.
- Keep signing keys and database credentials in User Secrets locally and in the hosting platform’s secret store after deployment.
- Demo accounts and sample data are for local learning only.
