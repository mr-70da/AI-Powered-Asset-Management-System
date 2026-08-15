# AI-Powered Asset Management System

The KINANA asset management assessment: a layered ASP.NET Core Web API
(API / Application / Domain / Infrastructure) backed by SQL Server, with JWT
authentication and role-based authorization (R1), asset management (R2),
transactional transfers with optimistic concurrency (R3), and Redis caching
(R5) — plus an Angular client (`src/Kinana.Assets.Client`) that covers login,
the asset register, detail/history, create/edit, and transfer (R6).

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

## Angular client (R6)

The client lives in `src/Kinana.Assets.Client` — a standalone-component Angular
app (v21) using the modern router, typed services, and reactive forms. The CORS
policy on the API allows only `http://localhost:4200`.

### Running it

```text
cd src/Kinana.Assets.Client
npm install
npm start            # dev server on http://localhost:4200, proxies /api to the API
```

The API base URL is configured in `src/environments/environment.ts`
(`http://localhost:5258` in development; a production build swaps the file via
`fileReplacements`). Start the API first (see "Running the API"), then sign in
with the seeded `admin` / `user` accounts.

### Request path

Components never touch `HttpClient` — they inject typed services (`AuthService`,
`AssetService`, `LookupService`) whose methods return strongly typed
observables mirroring the C# DTOs (R6.7). A single HTTP interceptor
(`auth.interceptor.ts`) attaches `Authorization: Bearer <token>` to every
outgoing request, silently refreshes the token pair once on a 401 and retries
the call, and routes 403 responses to a dedicated "Not Permitted" page (R6.3).

### Routing & guards

- `authGuard` (canActivate) protects the whole authenticated area. A visitor
  without a token is sent to `/login?returnUrl=...` and returned there after
  signing in (R6.1).
- `adminGuard` (canMatch) gates the lazy-loaded Create/Edit/Transfer feature
  chunks, so a standard `User` never even downloads those bundles (R6.2).
- `unsavedChangesGuard` (canDeactivate) runs on the Create/Edit/Transfer forms
  and asks for confirmation before leaving with a dirty form (R6.4).

### Token storage rationale (R6.8)

I stored the JWT pair in `localStorage` (via `TokenStorageService`).

- **What it exposes me to:** `localStorage` is readable by any script running on
  the origin, so a successful XSS injection could steal the tokens. This is the
  classic trade-off of persistent token storage.
- **Why I chose it:** it survives tab closes and full page reloads, so the user
  signs in once per browser session. The alternative — in-memory storage — is
  safer against XSS but loses the session on every refresh, forcing the
  refresh/redirect dance to be the primary login path and complicating the
  guards.
- **How I mitigated the XSS risk:** Angular's template bindings escape output by
  default (no `innerHTML`, no `bypassSecurityTrust*` anywhere); no third-party
  script loads on the page; and `localStorage` is the only storage surface —
  the refresh token is never sent to any origin other than our API. Crucially,
  the tokens are **only credentials for the API**, which is itself the
  authorization boundary (see the security note below); stealing a token cannot
  change what a `User` is allowed to do.
- **Bonus resilience:** a 401 triggers one single-flight refresh (concurrent
  401s share one refresh request); if the refresh token has also expired the
  interceptor signs the user out and redirects to `/login`.

### UI security boundary (R6.5)

> **The `*appAdminOnly` directive is strictly for UX purposes to prevent visual
> clutter. Actual authorization is enforced server-side via the API.**

Edit/Transfer/Retire buttons and the Create-asset flows are hidden for `User`
role with a single `*appAdminOnly` structural directive instead of scattered
`*ngIf`s. The API independently rejects any non-Admin call with a 403 — the
directive only controls what is *rendered*, never what is *allowed*.

### Forms, validation and states (R6.6)

Create/Edit/Transfer use `FormBuilder` reactive forms with client-side
validators that mirror the server rules. On a `400 Bad Request`, the interceptor
path passes the `ProblemDetails` payload to the component, which maps each
`errors[property]` entry onto the matching form control and surfaces
non-field messages in an alert. Every data-fetching view has a loading spinner,
an explicit "No assets found" empty state, and a readable error state.

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
| `POST /api/assets/{id}/transfer` | Admin | Record a transfer — transaction + optimistic concurrency (R3.3, R3.4, R3.5) |
| `GET /api/assets/{id}/transfers` | Any authenticated user | Full transfer history in chronological order (R3.2) |
| `GET /api/lookups` | Any authenticated user | Reference data (categories, types, departments, locations, employees) — cached (R5.1) |
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

## Asset transfers (R3)

### Transactional and immutable (R3.1, R3.3)

A transfer records the previous and new employee / department / location, the
transfer date, the reason, and who performed it. The `AssetTransfers` row is
append-only — we never edit or delete it, so the history is immutable. Both the
asset assignment change and the history row are written inside one explicit
database transaction (`BeginTransactionAsync` / `CommitAsync`), so a failure at
any point leaves the asset untouched and no partial history entry behind.

### Concurrency (R3.5)

Every `Asset` row carries a SQL Server `rowversion` column that EF Core maps with
`.IsRowVersion()`. When the client loads an asset it receives the version, and must
send it back in `POST /api/assets/{id}/transfer`. EF Core uses that version in the
`UPDATE ... WHERE` clause; if the row changed since it was loaded, zero rows are
affected and a `409 Conflict` ("concurrency conflict") is returned instead of
silently overwriting someone else's change.

### History order (R3.2)

`GET /api/assets/{id}/transfers` returns the full history sorted oldest-first so
the "life story" of an asset reads chronologically. (The prompt sketch showed a
descending sort; chronological ascending was chosen deliberately so the detail
view and the history endpoint agree and the story reads top to bottom.)

### Permission note

The prompt suggested decorating both endpoints with `[Authorize(Roles = "Admin")]`,
but the requirement's permission matrix allows *any* authenticated user to *view*
transfer history and reserves *creating* transfers for Admins. The implementation
follows the matrix: `POST .../transfer` is Admin-only, `GET .../transfers` is open
to any authenticated user. If history should be Admin-only instead, that is a
one-line change on the endpoint.

## Redis caching (R5)

Reference data, asset details, and paginated asset lists are cached in Redis using
a cache-aside strategy (read-through on miss, explicit invalidation on write).

### Starting Redis (R5.5, R5.7)

A `docker-compose.yml` at the repo root starts both SQL Server (1433) and Redis
(6379). Start Redis with:

```text
docker compose up -d
```

Or Redis alone:

```text
docker compose up -d redis
```

The API connects to `localhost:6379` by default; override via the `CacheSettings`
section in `appsettings.json` (ConnectionString, GlobalPrefix, TTLs).

### What gets cached and how (R5.1, R5.2, R5.4)

Cache-aside: on a cache miss the service reads from SQL Server and writes the
serialized result to Redis with a TTL; on a hit the cached copy is returned
without touching the database.

| Cache | Key | TTL |
|-------|-----|-----|
| Reference data (lookups) | `KinanaAssets:Lookups:All` | 60 min |
| Asset detail | `KinanaAssets:{Role}:Asset_{id}` | 15 min |
| Asset list (page/filters/sort) | `KinanaAssets:{Role}:Assets_...` | 15 min |

List keys encode the full query shape — page, page size, search text, and every
filter, so distinct queries never collide. Role is derived from what the caller is
authorized to see: `Admin` (includes purchase cost) or `User` (cost-free). The
same query made by an Admin and a User therefore uses different keys and the
User-facing list can never leak cost fields from the Admin cache (R5.4).

### Invalidation strategy (R5.3)

Every write path — create, update, retire, and transfer — evicts the affected
entries by prefix after the transaction commits:

- `KinanaAssets:*:Asset_{id}` — every detail entry for that asset, any role
- `KinanaAssets:*:Assets_*` — every cached list, any role (the trailing `*` is
  required because list keys encode the full query shape after the `Assets_` prefix)

Lists are invalidated as a whole because a new or edited asset can change the
result set of any filtered query; the next request rebuilds the page from SQL
Server.

### Graceful degradation (R5.6)

Caching is best-effort. If Redis is unreachable (stopped container, bad
connection string, network blip) the API keeps serving from SQL Server and
reconnects automatically once Redis is back; no request fails because of the
cache. Writes always go to SQL Server first, so the cache can never lose data —
at worst it serves stale data for up to its TTL.
