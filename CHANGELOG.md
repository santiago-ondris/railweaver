# Changelog

Todos los cambios relevantes de RailWeaver se documentan en este archivo.

El formato se basa en [Keep a Changelog](https://keepachangelog.com/es-ES/1.1.0/) y el proyecto usa [Semantic Versioning](https://semver.org/lang/es/). Durante `0.x` no se promete estabilidad de APIs, formatos de escenario ni modelo de dominio.

## [Unreleased]

## [0.4.0] - 2026-09-22

### Added

- Fuente de elevación y perfiles deterministas en el core, con muestreo cada 30 m,
  distancia de gran círculo, pendientes en ‰ y propagación estricta de NoData.
- Receta reproducible para descargar Copernicus DEM GLO-30 y codificarlo en una
  grilla local por bloques zlib, sin raster en git ni dependencias nativas en runtime.
- Endpoints de cota, perfil longitudinal y heightmaps Cesium, con caché acotada,
  límites de solicitud y degradación explícita cuando falta el dataset.
- Relieve 3D conmutable, infraestructura apoyada al terreno, cota por clic y
  herramienta de perfil con métricas y gráfico SVG en el dock de análisis.

## [0.3.0] - 2026-09-22

### Added

- Modelo de infraestructura ferroviaria en `RailWeaver.Core.Infrastructure`: trochas,
  estados operacionales, usos, tramos y estaciones con trazabilidad de inferencias.
- Pipeline offline reproducible para extraer la red ferroviaria de Córdoba desde
  OpenStreetMap, archivar la respuesta cruda de Overpass y generar GeoJSON validado.
- Dataset versionado bajo ODbL 1.0 con 2.374 tramos, 169 estaciones, metadatos de fuente
  y evidencia de cómo OSM representa la doble trocha de Córdoba capital.
- Endpoint `GET /api/regions/{id}/railway`, validado mediante pruebas HTTP.
- Capa Cesium de infraestructura ferroviaria existente, control de visibilidad,
  marcadores de estaciones y panel de metadatos con procedencia de la trocha.

## [0.2.1] - 2026-09-22

### Added

- `src/web` adopta el sistema visual de `DESIGN.md` (RW-002):
  - shell claro con barra superior, riel de capas y barra de estado;
  - tokens generados con `npm run tokens`;
  - Schibsted Grotesk, Fragment Mono y Big Shoulders Stencil autoalojadas con Fontsource;
  - marca y favicon;
  - mapa base OpenStreetMap neutralizado;
  - errores con el patrón de alertas.
- El CI valida `DESIGN.md` con el lint oficial y comprueba que los tokens generados estén al día.
- Sistema visual en `DESIGN.md` (formato DESIGN.md de Google Labs): dirección híbrida
  "sala de control sobre mesa de dibujo", tema claro, tokens en OKLCH, tipografías
  Schibsted Grotesk, Fragment Mono y Big Shoulders Stencil, y reglas para mapa,
  gráficos, movimiento y textos.
- ADR-009 (sistema visual) y nota de investigación sobre semántica de color de la UI
  frente a la señalización ferroviaria y la tipografía histórica ferroviaria argentina.
- Marca de RailWeaver en `assets/brand/`: un cruce en un gráfico de marcha, con
  versiones normal, reducida y favicon; sus reglas de uso están en `DESIGN.md`.

### Changed

- La atribución de OpenStreetMap se muestra siempre en pantalla, no solo detrás de "Data attribution".
- El contorno de la región de referencia pasa de ámbar (color de advertencia) a la tinta de infraestructura.

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
