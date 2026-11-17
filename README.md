# Shipment Tracker API

![CI](https://github.com/AngelinAkshitha/shipment-tracker-api/actions/workflows/ci.yml/badge.svg)

A small REST API for tracking shipments, built with ASP.NET Core 8 (C#). Every push runs the tests, builds a Docker image and checks the running container is healthy, using GitHub Actions.

## Stack

- **API:** ASP.NET Core 8 minimal API, C#
- **Tests:** xUnit, with `WebApplicationFactory` for in-memory HTTP tests
- **Container:** multi-stage Dockerfile (SDK image to build, small ASP.NET runtime image to run, non-root user)
- **CI/CD:** GitHub Actions: restore, build, test, then Docker build and a container health check

## Endpoints

| Method | Route | What it does |
|---|---|---|
| GET | `/health` | Health check used by the container smoke test |
| GET | `/shipments` | List all shipments |
| GET | `/shipments/{id}` | Get one shipment, or 404 |
| POST | `/shipments` | Create a shipment (`origin` and `destination` required, else 400) |
| PATCH | `/shipments/{id}/status` | Change status: `Booked`, `InTransit`, `AtPort`, `Delivered`. A delivered shipment cannot change (409) |

## Run it

```bash
# Locally
dotnet run --project src/ShipmentTracker.Api

# Tests
dotnet test

# Docker
docker build -t shipment-tracker-api .
docker run -p 8080:8080 shipment-tracker-api
curl http://localhost:8080/health
```

Example request:

```bash
curl -X POST http://localhost:8080/shipments \
  -H "Content-Type: application/json" \
  -d '{"origin":"Melbourne","destination":"Singapore"}'
```

## Design notes

- Data is kept in memory in a thread-safe `ConcurrentDictionary`, so the project runs with no database setup.
- Business rules (valid statuses, delivered shipments are final) live in `ShipmentStore`, separate from the HTTP layer, so they are unit-tested directly.
- The Docker image runs as the non-root `app` user that ships with the .NET 8 images.

## Next steps

- Swap the in-memory store for SQL Server with Entity Framework Core
- Push the image to a registry and deploy to Azure App Service from the pipeline
