# RailWeaver Status

Last updated: 2026-09-22 (v0.4.0)

## Current milestone

M0 — Real World Skeleton. V0.4 — Terrain ([RW-004](../specs/completed/RW-004-terrain-and-elevation.md)) completada.

## Working

- Solución .NET 10: `RailWeaver.Core`, `RailWeaver.Api` (health, regiones,
  infraestructura ferroviaria, elevación, perfiles y terreno), `RailWeaver.Core.Tests`
  y `RailWeaver.Api.Tests` (69 tests, incluye frontera del core, formatos, datasets y endpoints).
- Frontend React + TS + Vite en `src/web`, con visor CesiumJS, OpenStreetMap neutralizado, cámara inicial, red ferroviaria de Córdoba y capas controlables a partir de datasets servidos por la API. Sigue `DESIGN.md`: tokens generados, fuentes autoalojadas, marca y favicon.
- Dataset ferroviario OSM de Córdoba versionado: 2.374 tramos, 169 estaciones, respuesta cruda de Overpass, metadatos y licencia ODbL 1.0; extractor offline reproducible en `tools/`.
- PostGIS 3.5 / PostgreSQL 17 vía `compose.yaml` (sin uso desde el código todavía).
- Documentación canónica, ADR-001…009, plantillas de specs e investigación.
- Sistema visual definido en `DESIGN.md` (ADR-009): dirección híbrida, tema claro, tokens OKLCH; aplicado a `src/web` en RW-002. Marca (logo) en `assets/brand/`.
- Elevación determinista en el core; formato DEM por bloques zlib y caché acotada en
  la API; endpoints de cota, perfil y terreno; relieve Cesium, cota por clic y perfil
  longitudinal en el frontend. Los 69 tests, lint y build pasan sin descargar el DEM.

## Next

- Implementar V0.5 — Candidate corridor prototype según la spec activa
  [RW-005](../specs/active/RW-005-candidate-corridor.md), basada en
  [railway-gradients-and-corridor-routing.md](../research/terrain-routing/railway-gradients-and-corridor-routing.md).
  Cierra el vertical slice de M0.

## Known problems

- La imagen oficial `postgis/postgis` es solo amd64: en Apple silicon corre emulada (ADR-003).
- El mapa base es OSM neutralizado, no un mapa base propio (RW-002, decisión 3). El contorno de región puede mostrar cortes mínimos a algunas distancias.
- El bundle inicial de CesiumJS es grande (aprox. 4,36 MB minificado / 1,18 MB gzip); se optimizará cuando exista una medición de carga representativa.
- OSM representa 40 `way` con `gauge=1000;1676`; alrededor de Córdoba Mitre hay evidencia bitrocha, pero el etiquetado actual no forma una continuidad bitrocha completa hasta Alta Córdoba.

## Open research questions

- Aspectos de señal del RITO vigente y licencia de FC Nefa ([nota](../research/visual-identity/color-semantics-and-heritage-lettering.md)).

## Open decisions (sin ADR todavía)

- Acceso a PostGIS desde .NET (Npgsql directo, Dapper, EF Core + NetTopologySuite) — cuando haya primer requisito de persistencia.
- Framework de tests de frontend (Vitest) — cuando exista lógica de frontend.
- Licencia del proyecto.
- Hosting del repositorio remoto.

## Recent architectural decisions

- ADR-001 Modular monolith · ADR-002 .NET 10 · ADR-003 PostgreSQL/PostGIS · ADR-004 React/TS/Vite · ADR-005 CesiumJS · ADR-006 Discrete-event simulation · ADR-007 Córdoba como dataset · ADR-008 Convención de idioma · ADR-009 Sistema visual.
