# RailWeaver Status

Last updated: 2026-09-21 (v0.1.0; RW-001 en curso)

## Current milestone

M0 — Real World Skeleton. V0.1 (Workspace) completada; V0.2 — Geographic viewer ([RW-001](../specs/active/RW-001-geographic-viewer.md)) en verificación final.

## Working

- Solución .NET 10: `RailWeaver.Core`, `RailWeaver.Api` (`GET /api/health` y `GET /api/regions/{id}`), `RailWeaver.Core.Tests` (42 tests, incluye frontera del core y validación geográfica).
- Frontend React + TS + Vite en `src/web`, con visor CesiumJS, OpenStreetMap, cámara inicial y capas controlables a partir del dataset servido por la API.
- PostGIS 3.5 / PostgreSQL 17 vía `compose.yaml` (sin uso desde el código todavía).
- Documentación canónica, ADR-001…007, plantillas de specs e investigación.

## In progress

- RW-001 — Geographic viewer: investigación de sistemas de referencia completada;
  WGS84/EPSG:4326 confirmado para las fronteras y bounding box inicial de Córdoba
  derivado de la capa oficial del IGN. OpenStreetMap elegido como proveedor de
  imágenes base y configuración oficial con `vite-plugin-static-copy` elegida para
  integrar los assets de Cesium con Vite. `GeoCoordinate` y `GeoBoundingBox`
  implementados en el core con validación, antimeridiano rechazado explícitamente y
  cobertura de límites/`Contains` mediante tests. Dataset de Córdoba incorporado con
  metadatos del IGN; la API lo lee, valida y expone en `GET /api/regions/cordoba`.
  El viewer Cesium ya muestra OpenStreetMap sobre el elipsoide, posiciona la cámara
  desde esos datos, dibuja la bounding box y permite alternar ambas capas. Lint,
  build y tests locales pasan; queda cerrar la spec y preparar `v0.2.0`.

## Next

- Cerrar RW-001: ejecutar CI en un remoto, mover la spec a completadas y preparar el tag `v0.2.0`.

## Known problems

- La imagen oficial `postgis/postgis` es solo amd64: en Apple silicon corre emulada (ADR-003).
- El repositorio no tiene remoto: el workflow de CI (`.github/workflows/ci.yml`) todavía no se ejecutó en GitHub.
- El bundle inicial de CesiumJS es grande (aprox. 4,36 MB minificado / 1,18 MB gzip); se optimizará cuando exista una medición de carga representativa.

## Open research questions

- Fuente DEM para V0.4 (Copernicus DEM candidata; resolución, licencia, cobertura).
- Calidad y licencia de datos ferroviarios de OSM para Córdoba (V0.3).

## Open decisions (sin ADR todavía)

- Acceso a PostGIS desde .NET (Npgsql directo, Dapper, EF Core + NetTopologySuite) — cuando haya primer requisito de persistencia.
- Framework de tests de frontend (Vitest) — cuando exista lógica de frontend.
- Licencia del proyecto.
- Hosting del repositorio remoto.

## Recent architectural decisions

- ADR-001 Modular monolith · ADR-002 .NET 10 · ADR-003 PostgreSQL/PostGIS · ADR-004 React/TS/Vite · ADR-005 CesiumJS · ADR-006 Discrete-event simulation · ADR-007 Córdoba como dataset · ADR-008 Convención de idioma.
