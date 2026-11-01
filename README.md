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

No application code has been generated yet.