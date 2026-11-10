# CLAUDE.md

This file provides guidance to Claude Code (claude.ai/code) when working with code in this repository.

## Project state

This is a freshly scaffolded ASP.NET Core Web API (default `dotnet new webapi` template, controller-based) targeting .NET 10. The only endpoint is the template's example `WeatherForecastController`; no real domain logic, database, or authentication has been added yet.

## Commands

The repo root holds `TrentoAmodo.Api.slnx`, the solution file (the new XML-based `.slnx` format, .NET 10's default). Commands can run either from the repo root against the solution, or from `src/TrentoAmodo.Api/` against the project directly.

- Build: `dotnet build` (from repo root, builds the solution) or `dotnet build` (from `src/TrentoAmodo.Api/`)
- Run (default profile, HTTPS + HTTP, ports 7288/5121): `dotnet run --project src/TrentoAmodo.Api` (from repo root) or `dotnet run` (from `src/TrentoAmodo.Api/`)
- Run (HTTP only, port 5121): add `--launch-profile http`
- Restore packages: `dotnet restore`

There are no test projects and no lint/format configs in the repo yet.

## Architecture

- `Program.cs` — minimal hosting model entry point. Registers MVC controllers (`AddControllers`) and Swashbuckle (`AddSwaggerGen`/`UseSwagger`/`UseSwaggerUI`), with the Swagger UI exposed in every environment at `/swagger`.
- `Controllers/` — one controller per route group, following standard ASP.NET Core MVC conventions (`[ApiController]`, `[Route("[controller]")]`).
- `appsettings.json` / `appsettings.Development.json` — standard layered configuration; no external services or connection strings configured.
- `Properties/launchSettings.json` — defines two launch profiles, `http` and `https`, used by `dotnet run --launch-profile <name>`.
- `TrentoAmodo.Api.http` — manual HTTP request file (usable with the VS Code/Rider HTTP client) for exercising endpoints during development.

## Conventions

- **Conventional Commits** and **Conventional Branch** naming are required for all commits and branches (e.g. `feat:`, `fix:`, `chore:`; branches like `feature/...`, `fix/...`).
- **English only**: all user-visible text — code, comments, strings, commit messages, branch names, PR descriptions, docs — must be in English, with no exceptions.
- **Never push** to a remote unless explicitly asked to in that moment; a prior push approval does not carry over.
- **Design-first**: this project targets a hexagonal architecture (ports and adapters) as much as possible. Keep domain/application logic independent of infrastructure (ASP.NET Core, EF Core, external services), expose ports from the core and implement adapters around them. Never take a shortcut without asking first.
