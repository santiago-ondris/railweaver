# RailWeaver

**Plan. Build. Simulate. Understand.**

Sandbox de planificación y simulación ferroviaria. Primer mundo: provincia de Córdoba, Argentina.

> Software de simulación, análisis y experimentación. No es un sistema certificado de señalización ni una herramienta para operar o aprobar infraestructura real. Ver [non-goals](docs/vision/non-goals.md).

Estado actual y próximos pasos: [docs/status/project-status.md](docs/status/project-status.md).

## Requisitos

- .NET SDK 10 (ver `global.json`)
- Node.js 20.19+ o 22.12+ (CI usa 24)
- Docker con Docker Compose (solo para PostGIS)

## Estructura

```text
src/RailWeaver.Core     núcleo: dominio, simulación, planificación (sin dependencias de frameworks)
src/RailWeaver.Api      API HTTP delgada (ASP.NET Core)
src/web                 frontend React + TypeScript + Vite
tests/                  tests .NET (xUnit v3)
docs/                   visión, arquitectura, ADRs, specs, investigación, estado (usable como vault de Obsidian)
compose.yaml            PostgreSQL + PostGIS para desarrollo
```

## Comandos

```bash
dotnet build
dotnet test
docker compose up -d --wait
dotnet run --project src/RailWeaver.Api
npm --prefix src/web install
npm --prefix src/web run dev
```

La API escucha en `http://localhost:5080`; el frontend en `http://localhost:5173` y redirige `/api` a la API.

Frontend: `npm --prefix src/web run lint` y `npm --prefix src/web run build`.

## Documentación

- [Kickoff del proyecto](RailWeaver_Project_Kickoff.md) — cómo entendemos RailWeaver hoy.
- [Visión](docs/vision/product-vision.md) · [Principios](docs/vision/principles.md) · [Non-goals](docs/vision/non-goals.md)
- [Arquitectura](docs/architecture/overview.md) · [ADRs](docs/decisions/README.md)
- [Specs](docs/specs/README.md) · [Investigación](docs/research/README.md)
- [CHANGELOG](CHANGELOG.md)
