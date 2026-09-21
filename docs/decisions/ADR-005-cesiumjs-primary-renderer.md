# ADR-005 — CesiumJS como renderer geoespacial principal

- Status: Accepted
- Date: 2026-09-21

## Context

RailWeaver necesita geografía real, terreno, GeoJSON, modelos glTF y, en el futuro, visualización dinámica en el tiempo (trenes moviéndose).

## Decision

**CesiumJS** es el renderer geoespacial principal. Se integra en RW-001 (V0.2 — Geographic viewer), no antes.

- Cesium no conoce el simulation engine: recibe estado/snapshots ya calculados y puede interpolar entre ellos para render.
- MapLibre puede evaluarse más adelante para vistas 2D específicas.
- QGIS se usa como herramienta externa de inspección de datos, no como dependencia de runtime.

## Consequences

- El bundle del frontend crecerá significativamente al integrar Cesium; hay que resolver cómo servir sus assets estáticos con Vite (pregunta abierta en RW-001).
- Las imágenes/terreno por defecto de Cesium usan Cesium ion (requiere token). Qué proveedor de imágenes usar queda abierto en RW-001.

## Alternatives considered

- **MapLibre GL / deck.gl:** excelentes en 2D/2.5D, menos adecuados para terreno 3D y tiempo dinámico.
- **Three.js propio:** demasiado trabajo de infraestructura geoespacial.
