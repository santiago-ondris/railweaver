# RailWeaver Status

Last updated: 2026-09-21 (v0.1.0)

## Current milestone

M0 — Real World Skeleton. V0.1 (Workspace) completada; próxima: V0.2 — Geographic viewer ([RW-001](../specs/active/RW-001-geographic-viewer.md)).

## Working

- Solución .NET 10: `RailWeaver.Core`, `RailWeaver.Api` (`GET /api/health`), `RailWeaver.Core.Tests` (2 tests, incluye frontera del core).
- Frontend React + TS + Vite en `src/web`, consulta la API vía proxy.
- PostGIS 3.5 / PostgreSQL 17 vía `compose.yaml` (sin uso desde el código todavía).
- Documentación canónica, ADR-001…007, plantillas de specs e investigación.

## In progress

- RW-001 — Geographic viewer: investigación de sistemas de referencia completada;
  WGS84/EPSG:4326 confirmado para las fronteras y bounding box inicial de Córdoba
  derivado de la capa oficial del IGN. OpenStreetMap elegido como proveedor de
  imágenes base para V0.2. Implementación todavía no iniciada.

## Next

- RW-001 — Geographic viewer (CesiumJS, región Córdoba como dato, modelo de coordenadas, control de capas).

## Known problems

- La imagen oficial `postgis/postgis` es solo amd64: en Apple silicon corre emulada (ADR-003).
- El repositorio no tiene remoto: el workflow de CI (`.github/workflows/ci.yml`) todavía no se ejecutó en GitHub.
- El .NET SDK en la máquina de desarrollo se instaló en `~/.dotnet` (script oficial); hay que agregarlo al `PATH` del shell.

## Open research questions

- Fuente DEM para V0.4 (Copernicus DEM candidata; resolución, licencia, cobertura).
- Calidad y licencia de datos ferroviarios de OSM para Córdoba (V0.3).

## Open decisions (sin ADR todavía)

- Proveedor de imágenes base para Cesium (ion vs. OSM) — RW-001.
- Integración de assets de Cesium con Vite — RW-001.
- Acceso a PostGIS desde .NET (Npgsql directo, Dapper, EF Core + NetTopologySuite) — cuando haya primer requisito de persistencia.
- Framework de tests de frontend (Vitest) — cuando exista lógica de frontend.
- Licencia del proyecto.
- Hosting del repositorio remoto.

## Recent architectural decisions

- ADR-001 Modular monolith · ADR-002 .NET 10 · ADR-003 PostgreSQL/PostGIS · ADR-004 React/TS/Vite · ADR-005 CesiumJS · ADR-006 Discrete-event simulation · ADR-007 Córdoba como dataset · ADR-008 Convención de idioma.
