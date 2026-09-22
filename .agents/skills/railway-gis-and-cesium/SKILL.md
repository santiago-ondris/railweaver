---
name: railway-gis-and-cesium
description: >-
  Railway domain-specific guidelines covering GIS data structures, GeoJSON coordinate conventions,
  CesiumJS 3D rendering performance, and DESIGN.md token enforcement.
  Use when handling map data, track topology, CesiumJS visualization, or frontend styling.
---

# Railway GIS, CesiumJS & Design System Guidelines

Domain-specific rules and best practices for RailWeaver.

## 1. Geospatial & Coordinate Rules

### GeoJSON Axis Order Pitfall
- Standard GeoJSON coordinates are strictly:
  `[longitude, latitude]` or `[longitude, latitude, altitude]`
- **Never swap these.** Mixing them up places Argentine railway tracks in Antarctica or the Indian Ocean.
- When transforming between Cesium `Cartographic.fromDegrees(longitude, latitude)` and GeoJSON, ensure `longitude` is always the first argument.

### Projections & Distance Calculations
- Database storage and GeoJSON exchange use `WGS84` (`EPSG:4326`).
- For railway engineering calculations (track length, curve radii, grades/elevations, train braking curves), always use metric cartesian distances or projected coordinates (e.g. POSGAR 2007 / Argentina or geodesic algorithms like Vincenty/Haversine).

### Railway Graph Topology
- Tracks are graph edges; switches, buffer stops, and station platforms are graph nodes.
- Topological continuity: endpoints of adjacent track segments must share exact node coordinates within a tolerance (snap distance) to allow train routing and pathfinding algorithms to traverse blocks.

## 2. CesiumJS 3D Rendering Performance

### Primitives vs. Entities
- For small dynamic objects (e.g. a selected train icon, inspection pin), `viewer.entities.add(...)` is acceptable.
- For static or large-scale railway infrastructure (thousands of track segments, overhead catenary lines, kilometer markers), **do not use individual entities**. Use batched collections:
  - `Cesium.PolylineCollection` for track geometry lines.
  - `Cesium.PointPrimitiveCollection` for signals and switches.
  - `Cesium.GroundPolylinePrimitive` when clamping tracks strictly to 3D terrain.

### Memory & Context Management
- Always remove primitives and listeners upon component unmount:
  ```ts
  const primitives = viewer.scene.primitives.add(new Cesium.PolylineCollection());
  // On cleanup:
  viewer.scene.primitives.remove(primitives);
  ```

## 3. Design System & Tokens Guardian (ADR-009)

- All visual elements in `src/web` must strictly adhere to [`DESIGN.md`](../../DESIGN.md).
- **Prohibited:** Never use hardcoded hex codes (`#1f2937`), arbitrary RGB values, or ad-hoc font definitions in TSX or CSS files.
- **Allowed:** Always use exported CSS variables:
  - `var(--rw-color-...)`
  - `var(--rw-font-...)`
  - `var(--rw-spacing-...)`
- **Workflow after modifying visual design or DESIGN.md:**
  ```bash
  npm --prefix src/web run tokens        # exports tokens from DESIGN.md
  npm --prefix src/web run design:lint   # validates DESIGN.md against Google design linter
  ```
