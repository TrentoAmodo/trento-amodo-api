# CLAUDE.md

This file provides guidance to Claude Code (claude.ai/code) when working with code in this repository.

## Project state

This is a freshly scaffolded ASP.NET Core Web API (default `dotnet new webapi` template, controller-based) targeting .NET 10. There is no solution (`.sln`) file — the project is built/run directly via its `.csproj`. The only endpoint is the template's example `WeatherForecastController`; no real domain logic, database, or authentication has been added yet.

## Commands

All commands run from `src/TrentoAmodo.Api/` (or pass `--project src/TrentoAmodo.Api` from the repo root).

- Build: `dotnet build`
- Run (HTTP only, port 5121): `dotnet run --launch-profile http`
- Run (HTTPS + HTTP, ports 7288/5121): `dotnet run --launch-profile https`
- Restore packages: `dotnet restore`

There are no test projects and no lint/format configs in the repo yet.

## Architecture

- `Program.cs` — minimal hosting model entry point. Registers MVC controllers (`AddControllers`) and Microsoft's native OpenAPI generator (`AddOpenApi`/`MapOpenApi`, document exposed only in Development at `/openapi/v1.json`). Note: there is no Swagger/Swashbuckle UI — only the raw OpenAPI JSON document is served.
- `Controllers/` — one controller per route group, following standard ASP.NET Core MVC conventions (`[ApiController]`, `[Route("[controller]")]`).
- `appsettings.json` / `appsettings.Development.json` — standard layered configuration; no external services or connection strings configured.
- `Properties/launchSettings.json` — defines two launch profiles, `http` and `https`, used by `dotnet run --launch-profile <name>`.
- `TrentoAmodo.Api.http` — manual HTTP request file (usable with the VS Code/Rider HTTP client) for exercising endpoints during development.
