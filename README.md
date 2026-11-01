## Architecture and reviewer startup requirements

The planned MVP targets ASP.NET Core 8 Web API on `net8.0`, EF Core 8, SQLite,
and ASP.NET Core Identity. Angular is pinned to CLI 21.2.16 with standalone
components and TypeScript 5.9.x; the dependency lockfile will pin the exact
Angular-compatible TypeScript patch version.

Verified development environment:

- Node.js 24.13.0
- npm 11.6.2
- .NET SDK 8.0.427 and 10.0.401 installed
- ASP.NET Core 8 runtime 8.0.33

The future `scripts\start-review.ps1` command must explicitly target `net8.0`
for restore, build, migration, test, and run operations. It must not rely on
the installed .NET 10 SDK to infer the target framework. The command will
initialize a writable local SQLite database, apply EF Core migrations, seed
deterministic local-only demo data, and start the reviewable application.

## Local review environment (Story 1.1)

Prerequisites: Windows, .NET SDK 8.x, Node.js 24.x and npm. From the repository
root run:

```powershell
powershell -ExecutionPolicy Bypass -File scripts\start-review.ps1
```

The command restores and builds the net8.0 API, installs the pinned Angular
21 dependencies, builds the production shell, applies SQLite migrations,
seeds deterministic local demo data, verifies health and starts the API at
`http://localhost:5080`. Use `-NoLaunch` to verify without starting it.

The seeded review accounts all use the local-only password `Review123!`:

- `reviewer@equipment.local` (Manager/Admin)
- `ali@equipment.local` (Field Staff)
- `saima@equipment.local` (Office Staff)
- `hamza@equipment.local` (Approver)
The database is `src\EquipmentTracking.Api\data\equipment-tracking.db`; delete
that file to reset it. This story intentionally provides only the runtime
foundation and seed data; later workflow features are not available yet.

## Sign-in (Story 1.2)

The local shell signs in through `POST /api/auth/login` and keeps the
same-origin Identity cookie. The current session is available from
`GET /api/auth/me`; use `POST /api/auth/logout` to invalidate it. Invalid
credentials return a generic `401`, and protected operations enforce roles on
the server. The four accounts above are the complete local demo identity set;
their password is only for local review and must not be reused elsewhere.