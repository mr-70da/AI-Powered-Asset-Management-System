# AI-Powered Asset Management System

Backend for the KINANA asset management assessment: a layered ASP.NET Core Web API
(API / Application / Domain / Infrastructure) backed by SQL Server, with JWT
authentication and role-based authorization (R1) and asset management (R2).

## Credentials (R1.5)

The database is seeded with two login accounts (see `database/scripts/002_seed.sql`).
Passwords are stored as salted BCrypt hashes only — never as plain text (R1.2).

| Role  | Username | Password   |
|-------|----------|------------|
| Admin | `admin`  | `Admin@1234` |
| User  | `user`   | `User@1234`  |

Sign in via `POST /api/auth/login`, which returns a JWT access token (30-minute
lifetime) and a refresh token (7-day lifetime). The access token carries the user
id (`nameidentifier`), username (`name`) and role (`role`) claims; roles are
enforced server-side with `[Authorize]`, never from a client-supplied field (R1.3).

## Running the API

1. Apply the SQL scripts to an empty database in order: `001_schema.sql`, then
   `002_seed.sql`.
2. Set the JWT signing key. In `src/Kinana.Assets.Api`, run
   `dotnet user-secrets set "Jwt:SigningKey" "<a key of at least 32 characters>"`,
   or set the `KASM__Jwt__SigningKey` environment variable. Never commit a real key.
3. `dotnet run --project src/Kinana.Assets.Api` (SQL Server connection string lives
   in `appsettings.Development.json` / `appsettings.json`).

A ready-made `.http` request collection (`Kinana.Assets.Api.http`) demonstrates
login, profile, and health flows.

## Endpoints implemented so far

| Endpoint | Access | Notes |
|----------|--------|-------|
| `POST /api/auth/login` | Anonymous | Issues access + refresh tokens (R1.1) |
| `POST /api/auth/refresh` | Anonymous + valid refresh token | Rotates the token pair (R1.6) |
| `GET /api/auth/me` | Any authenticated user | Current user's profile and role (R1.6) |
| `GET /api/users`, `POST /api/users`, `PUT /api/users/{id}/role`, `PUT /api/users/{id}/status` | Admin | User administration (R1.7) |
| `GET /api/assets` | Any authenticated user | Paged, filtered, sorted asset list (R2.1, R2.2) |
| `GET /api/assets/{id}` | Any authenticated user | Asset detail incl. current assignment and transfer history (R2.3) |
| `POST /api/assets`, `PUT /api/assets/{id}` | Admin | Create / edit asset (R2.4) |
| `POST /api/assets/{id}/retire` | Admin | Retire an asset — soft delete (R2.5) |
| `GET /health` | Anonymous | Liveness check |

Every other endpoint requires a valid token; a missing/invalid token returns `401`
and an authenticated user lacking a role returns `403` (R1.4). Errors are returned
as `application/problem+json` via a single exception-handling middleware.

## Asset management (R2)

### Why assets are retired, not hard-deleted (R2.5)

Assets are never removed with `_context.Assets.Remove()`. Retiring an asset sets its
`Status` to `Retired` instead. The reason is historical accuracy: an asset that is
no longer in use still appears in its past transfer history, and audit fields
(created/modified by whom) must remain traceable. Deleting the row would orphan the
`AssetTransfers` history and destroy the audit trail, so a retired asset stays in
the register with a `Retired` status and no longer participates in transfers.

### Field-level authorization for purchase cost (R2.6)

`PurchaseCost` is restricted to Admins. For a `User` role caller the value is forced
to `null` inside the data-access projection — it never leaves the database layer in
a `User` response, rather than merely being hidden in the UI. The same flag guards
list and detail responses, and cache keys (once caching lands) must incorporate the
role so an Admin response is never served to a `User`.
