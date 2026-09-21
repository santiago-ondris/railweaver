# Changelog

Todos los cambios relevantes de RailWeaver se documentan en este archivo.

El formato se basa en [Keep a Changelog](https://keepachangelog.com/es-ES/1.1.0/) y el proyecto usa [Semantic Versioning](https://semver.org/lang/es/). Durante `0.x` no se promete estabilidad de APIs, formatos de escenario ni modelo de dominio.

## [Unreleased]

### Added

- Investigación de sistemas de referencia geográficos para RW-001: uso de WGS84 en
  fronteras, relación con POSGAR 2007/Gauss-Krüger y fuente oficial del bounding box
  inicial de Córdoba.

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
