# ADR-007 — Córdoba como primer dataset, no dependencia del core

- Status: Accepted
- Date: 2026-09-21

## Context

El primer mundo de RailWeaver es la provincia de Córdoba, Argentina. La arquitectura debe permitir otras regiones (Mendoza, Argentina completa, cualquier otra).

## Decision

Córdoba es un **dataset y escenario inicial**. El core nunca asume nombres, coordenadas, normas locales ni infraestructura de Córdoba salvo a través de datos o configuración.

- Coordenadas geográficas en WGS84 (EPSG:4326) en las fronteras del sistema; proyecciones locales como detalle de adapters/datos.
- Reglas o normas locales (p. ej. CNRT) se modelan como configuración/perfiles, no como constantes del core.

## Consequences

- Tests del core usan datos sintéticos o fixtures explícitos, no supuestos de Córdoba.
- Cada dataset documenta fuente, licencia, fecha y confianza.

## Alternatives considered

- **Hardcodear Córdoba para ir más rápido:** barato hoy, caro de revertir cuando el mundo crezca.
