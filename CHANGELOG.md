# Changelog

Todos los cambios relevantes de RailWeaver se documentan en este archivo.

El formato se basa en [Keep a Changelog](https://keepachangelog.com/es-ES/1.1.0/) y el proyecto usa [Semantic Versioning](https://semver.org/lang/es/). Durante `0.x` no se promete estabilidad de APIs, formatos de escenario ni modelo de dominio.

## [Unreleased]

### Added

- Sistema visual en `DESIGN.md` (formato DESIGN.md de Google Labs): dirección híbrida
  "sala de control sobre mesa de dibujo", tema claro, tokens en OKLCH, tipografías
  Schibsted Grotesk, Fragment Mono y Big Shoulders Stencil, y reglas para mapa,
  gráficos, movimiento y textos.
- ADR-009 (sistema visual) y nota de investigación sobre semántica de color de la UI
  frente a la señalización ferroviaria y la tipografía histórica ferroviaria argentina.
- Marca de RailWeaver en `assets/brand/`: un cruce en un gráfico de marcha, con
  versiones normal, reducida y favicon; sus reglas de uso están en `DESIGN.md`.

## [0.2.0] - 2026-09-21

### Added

- Investigación de sistemas de referencia geográficos para RW-001: uso de WGS84 en
  fronteras, relación con POSGAR 2007/Gauss-Krüger y fuente oficial del bounding box
  inicial de Córdoba.
- Value objects geográficos `GeoCoordinate` y `GeoBoundingBox`, con validación de
  rangos WGS84, rechazo explícito del antimeridiano y pruebas unitarias de límites y
  contención.
- Dataset inicial de Córdoba con bounding box, vista de cámara y metadatos de fuente
  del IGN; endpoint genérico `GET /api/regions/{id}` que lo lee y valida antes de
  exponerlo al frontend.
- Visor geográfico CesiumJS enfocado en Córdoba, con imágenes base de OpenStreetMap,
  terreno elipsoidal, bounding box de referencia y controles accesibles para alternar
  las capas de mapa y región.

### Changed

- RW-001 adopta temporalmente las teselas raster estándar de OpenStreetMap como
  imágenes base, con reevaluación obligatoria antes de un despliegue público.
- La integración de Cesium con Vite seguirá la configuración oficial basada en
  `vite-plugin-static-copy` y `CESIUM_BASE_URL`, sin un plugin específico de Cesium.
- Los datasets de región serán leídos y validados por el backend y expuestos al
  frontend mediante la API, en lugar de ser leídos directamente por la UI.

## [0.1.0] - 2026-09-21

Primer workspace ejecutable (RW-000 — Bootstrap).

### Added

- Solución .NET 10 (`RailWeaver.slnx`): `RailWeaver.Core` (núcleo independiente de frameworks), `RailWeaver.Api` (ASP.NET Core, endpoint `GET /api/health`) y `RailWeaver.Core.Tests` (xUnit v3).
- Test de frontera que impide que `RailWeaver.Core` referencie ASP.NET Core, EF Core o Npgsql.
- Aplicación web React + TypeScript + Vite en `src/web`, con proxy `/api` hacia la API en desarrollo.
- `compose.yaml` con PostgreSQL 17 + PostGIS 3.5 para desarrollo local.
- Documentación: visión, principios, non-goals, arquitectura, ADR-001 a ADR-007, plantillas de specs e investigación, estado del proyecto.
- `AGENTS.md`, `CLAUDE.md`, `README.md`.
- CI básico en GitHub Actions (build + tests .NET, lint + build frontend, validación de compose).
