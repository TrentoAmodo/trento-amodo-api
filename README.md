# TrentoAmodo API

ASP.NET Core Web API for the TrentoAmodo project.

## Links

- Deliverables: [D1](https://docs.google.com/document/d/1uFCczYFwUQgwpmSV7Mbmf4OZb5k9oVVl/edit)
- Jira board: [TA](https://t55trentoamodo.atlassian.net/jira/software/projects/TA/boards/35)
- GitHub organization: [TrentoAmodo](https://github.com/TrentoAmodo)

## Local services

Local development dependencies (Postgres, and future services such as cache or search) run via Docker Compose.

1. Copy `.env.example` to `.env` and adjust the values (at minimum, set a real `POSTGRES_PASSWORD`). If port `5432` is already taken on your machine, set `POSTGRES_PORT` to a free one instead (e.g. `5433`).
2. Start the services: `docker compose up -d`
3. Stop them: `docker compose down` (add `-v` to also delete the Postgres data volume)

## Database (EF Core)

The API connects to Postgres through `TrentoAmodo.Infrastructure` (the persistence adapter), using EF Core with a code-first workflow.

1. Restore the local tool (`dotnet-ef`): `dotnet tool restore`
2. Set your local connection string via .NET User Secrets (matches the values in your `.env`), from `src/TrentoAmodo.Api`:
   ```
   dotnet user-secrets set "ConnectionStrings:Default" "Host=localhost;Port=5433;Database=trento_amodo;Username=trento_amodo;Password=<your-password>"
   ```
3. Apply migrations to your local database: `dotnet tool run dotnet-ef database update --project src/TrentoAmodo.Infrastructure --startup-project src/TrentoAmodo.Api`

After changing an entity, add a new migration with:
```
dotnet tool run dotnet-ef migrations add <Name> --project src/TrentoAmodo.Infrastructure --startup-project src/TrentoAmodo.Api --output-dir Persistence/Migrations
```

### Checking DB connectivity

`GET /health` runs a real query against the database and returns `Healthy`/`Unhealthy`. Use it to verify the DB connection both locally and in staging, with the same tool.

See `CLAUDE.md` for project conventions and local development commands.
