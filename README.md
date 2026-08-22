# luftborn-technical-test

ASP.NET Core (.NET 10) Web API for a songs/playlists catalog, backed by SQL Server (EF Core).
The whole stack is containerized: one command starts both the API and a SQL Server database.

Full endpoint documentation: [docs/API.md](docs/API.md)

---

## Requirements

- [Docker Desktop](https://www.docker.com/products/docker-desktop/) (includes Docker Compose) — Windows, macOS, or Linux
- No .NET SDK or SQL Server installation needed

## Run with Docker (recommended)

1. Clone the repository:

   ```bash
   git clone https://github.com/OmarNaru1110/luftborn-technical-test.git
   cd luftborn-technical-test
   ```

2. From the repository root, start the stack:

   ```bash
   docker compose up --build
   ```

What happens:

1. Builds the API image from the `Dockerfile`
2. Starts a SQL Server 2022 container with a persistent data volume
3. Waits until SQL Server is healthy, then starts the API
4. The API applies EF Core migrations and seeds sample songs automatically on first run

When it's up:

| Service    | URL                                        |
| ---------- | ------------------------------------------ |
| Swagger UI | http://localhost:8080/swagger              |
| OpenAPI spec | http://localhost:8080/openapi/v1.json    |
| API        | http://localhost:8080/api/...              |

Quick smoke test:

```bash
curl http://localhost:8080/api/songs
```

Stop everything (keeps database data):

```bash
docker compose down
```

Stop and delete the database volume as well:

```bash
docker compose down -v
```

### Custom SA password

The default SQL Server `sa` password is `Your_strong_P@ssw0rd`. To use your own, create a `.env` file in the repository root (next to `docker-compose.yml`):

```bash
MSSQL_SA_PASSWORD=My_Other_P@ssw0rd
```

The password must satisfy SQL Server complexity rules (min 8 chars, uppercase, lowercase, digit, symbol).

> The SQL Server container is also reachable from the host on port `1533` (`localhost,1533`), handy for SSMS/Azure Data Studio. If that port is taken, change the `ports` mapping of the `sqlserver` service in `docker-compose.yml` — the API reaches it through the internal Docker network anyway.

## Run without Docker (local development)

Requirements: .NET SDK 10 and a reachable SQL Server instance.

1. Set the connection string (environment variable or `dotnet user-secrets`):

   ```bash
   # PowerShell
   $env:SQLServer_ConnectionString = "Server=localhost;Database=LuftbornDb;Trusted_Connection=True;TrustServerCertificate=True;MultipleActiveResultSets=True"
   ```

   Alternatively, copy `API/API/.env.example` to `API/API/.env` and fill in your connection string (loaded automatically by DotNetEnv).

2. Run the API (migrations + seed run automatically):

   ```bash
   dotnet run --project API/API
   ```

3. Open http://localhost:5129/swagger (HTTP profile) or https://localhost:7065/swagger.

Run the unit tests:

```bash
dotnet test UnitTests
```

## Project structure

```
API/      ASP.NET Core Web API (controllers, services, Program.cs)
CORE/     Domain/application services
DATA/     EF Core DbContext, models, migrations (SQL Server)
UnitTests NUnit tests
docs/     API documentation and diagrams
```
