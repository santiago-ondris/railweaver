# RailWeaver Status

Last updated: 2026-09-21 (v0.2.0)

## Current milestone

M0 — Real World Skeleton. V0.2 — Geographic viewer ([RW-001](../specs/completed/RW-001-geographic-viewer.md)) completada; próxima: V0.3 — Existing railway data.

## Working

- Solución .NET 10: `RailWeaver.Core`, `RailWeaver.Api` (`GET /api/health` y `GET /api/regions/{id}`), `RailWeaver.Core.Tests` (42 tests, incluye frontera del core y validación geográfica).
- Frontend React + TS + Vite en `src/web`, con visor CesiumJS, OpenStreetMap, cámara inicial y capas controlables a partir del dataset servido por la API.
- PostGIS 3.5 / PostgreSQL 17 vía `compose.yaml` (sin uso desde el código todavía).
- Documentación canónica, ADR-001…009, plantillas de specs e investigación.
- Sistema visual definido en `DESIGN.md` (ADR-009): dirección híbrida, tema claro, tokens OKLCH; todavía no aplicado a `src/web`. Marca (logo) en `assets/brand/`.

## In progress

- No hay una spec activa. RW-001 cerró con CI verde y release `v0.2.0`.

## Next

- Investigar la calidad, licencia y estrategia de extracción de datos ferroviarios de OSM para V0.3; después escribir RW-002.
- Spec de migración de `src/web` al sistema visual (tokens → CSS, fuentes autoalojadas, mapa base neutro, favicon y marca, auditorías en CI).

## Known problems

- La imagen oficial `postgis/postgis` es solo amd64: en Apple silicon corre emulada (ADR-003).
- `src/web` no sigue `DESIGN.md` (fondo oscuro, Inter, teselas OSM a color) hasta la spec de migración.
- El bundle inicial de CesiumJS es grande (aprox. 4,36 MB minificado / 1,18 MB gzip); se optimizará cuando exista una medición de carga representativa.

## Open research questions

- Fuente DEM para V0.4 (Copernicus DEM candidata; resolución, licencia, cobertura).
- Calidad y licencia de datos ferroviarios de OSM para Córdoba (V0.3).
- Aspectos de señal del RITO vigente y licencia de FC Nefa ([nota](../research/visual-identity/color-semantics-and-heritage-lettering.md)).

## Open decisions (sin ADR todavía)

- Acceso a PostGIS desde .NET (Npgsql directo, Dapper, EF Core + NetTopologySuite) — cuando haya primer requisito de persistencia.
- Framework de tests de frontend (Vitest) — cuando exista lógica de frontend.
- Licencia del proyecto.
- Hosting del repositorio remoto.

## Recent architectural decisions

- ADR-001 Modular monolith · ADR-002 .NET 10 · ADR-003 PostgreSQL/PostGIS · ADR-004 React/TS/Vite · ADR-005 CesiumJS · ADR-006 Discrete-event simulation · ADR-007 Córdoba como dataset · ADR-008 Convención de idioma · ADR-009 Sistema visual.
