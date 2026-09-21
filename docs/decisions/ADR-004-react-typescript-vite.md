# ADR-004 — React + TypeScript + Vite

- Status: Accepted
- Date: 2026-09-21

## Context

La UI necesita visualización geoespacial, vistas esquemáticas y paneles de análisis. El frontend debe ser deliberadamente convencional: la innovación está en el dominio, no en el stack web.

## Decision

- **React + TypeScript + Vite**, en `src/web`, generado con la plantilla oficial `react-ts` de Vite.
- Lint con **oxlint** (incluido por la plantilla).
- En desarrollo, Vite hace proxy de `/api` a `http://localhost:5080` (RailWeaver.Api), evitando configurar CORS.
- Sin router, state manager, librería de componentes ni framework de tests hasta que un requisito concreto lo pida. Vitest se agregará cuando exista lógica de frontend que testear.

## Consequences

- El frontend no contiene reglas de dominio ni invariantes.
- La UI no está dockerizada durante el desarrollo.

## Alternatives considered

- **Next.js / frameworks SSR:** innecesarios para una app de simulación cliente.
- **Angular, Svelte, Vue:** válidos; React tiene el ecosistema más amplio para Cesium y visualización.
