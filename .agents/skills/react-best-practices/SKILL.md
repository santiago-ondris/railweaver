---
name: react-best-practices
description: >-
  Frontend engineering best practices for React 19, TypeScript, and Vite.
  Use when designing, building, or refactoring components in src/web, especially
  when handling high-frequency state updates or integrating 3D viewports.
---

# React 19 & Web Architecture Guidelines

Best practices tailored for the `src/web` application in RailWeaver.

## Core Rules for React 19 & Vite

### 1. Viewport & Canvas Decoupling
- RailWeaver uses CesiumJS for 3D geospatial rendering. **Do not** synchronize high-frequency simulation ticks or camera matrices into React component state (`useState`). Doing so triggers full-tree re-renders at 60 FPS and destroys performance.
- Isolate the Cesium `Viewer` instance inside a dedicated ref (`useRef<Cesium.Viewer | null>(null)`).
- Let Cesium handle its own internal animation and render loop. Feed simulation updates directly to Cesium primitives/entities via imperative controllers, and only bridge low-frequency state (e.g. selected entity ID, pause/play status, active scenario) into React.

### 2. State & Effect Hygiene
- **Avoid redundant `useEffect` for derived data:** Calculate derived values synchronously during render or with `useMemo` when computationally expensive.
- **Do not use effects for event-driven actions:** User clicks, form submissions, or button actions should trigger event handler functions directly, not set a state flag that triggers a `useEffect`.
- **Clean up resources:** Any subscriptions, event listeners, or Cesium scene event callbacks must be explicitly removed in `useEffect` cleanup return functions.

### 3. Component Architecture
- Separate UI into:
  - **Dumb/Presentational Panels:** Inspect station stats, view train info, timeline scrubber.
  - **Viewport Controllers:** Hook-based or singleton adapters bridging the API/WebSocket and Cesium without re-rendering panels.
- Keep components small, cohesive, and typed with strict TypeScript interfaces.

### 4. Verification & Quality
Always check both Oxlint and the TypeScript build:
```bash
npm --prefix src/web run lint
npm --prefix src/web run build
```
