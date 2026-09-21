# RW-000 — Bootstrap RailWeaver

- Status: Completed (v0.1.0, 2026-09-21)
- Milestone: V0.1 — Workspace

## Goal

Crear el workspace inicial en una carpeta vacía: coherente, ejecutable y testeable, sin lógica ferroviaria.

## Scope

Estructura del repositorio, solución .NET, proyectos mínimos justificados, app React + TS + Vite, Docker Compose con PostGIS, estructura `docs/`, `AGENTS.md`, `CLAUDE.md`, estado del proyecto, ADRs de decisiones ya tomadas, README, CHANGELOG, CI básico, tag `v0.1.0`.

## Non-goals

Lógica ferroviaria, CesiumJS, conexión a la base, datos de Córdoba, schema de base.

## Outputs

| Entregable | Resultado |
|---|---|
| Solución .NET | `RailWeaver.slnx` con `RailWeaver.Core`, `RailWeaver.Api`, `RailWeaver.Core.Tests` |
| Proyectos justificados | Core (frontera independiente de frameworks), Api (host HTTP), Core.Tests (tests + frontera). Nada más. |
| Frontend | `src/web`, muestra el estado de `GET /api/health` vía proxy de Vite |
| Compose | `compose.yaml` → `postgis/postgis:17-3.5` con healthcheck |
| Docs | `docs/vision`, `docs/architecture`, `docs/decisions` (ADR-001…007), `docs/specs`, `docs/research`, `docs/status` |
| CI | `.github/workflows/ci.yml` |

## Acceptance criteria (verificados 2026-09-21)

- [x] `dotnet build` sin warnings ni errores.
- [x] `dotnet test` pasa (2 tests: versión SemVer, frontera del core).
- [x] `npm run lint` y `npm run build` en `src/web` pasan.
- [x] `docker compose up -d --wait` levanta PostGIS healthy; `postgis_full_version()` responde (PostGIS 3.5.2, GEOS, PROJ).
- [x] `GET /api/health` responde `{"status":"ok","name":"RailWeaver","version":"0.1.0"}` directo y vía proxy de Vite; la página lo muestra.
- [ ] CI ejecutado en GitHub: pendiente, el repositorio todavía no tiene remoto.

## Decisiones tomadas durante la implementación

- xUnit v3 + Microsoft.Testing.Platform en lugar de xUnit v2 (ADR-002).
- PostGIS oficial forzado a `linux/amd64` (ADR-003).
- Un solo proyecto core; los bounded contexts nacen como namespaces cuando haya código (overview de arquitectura).
