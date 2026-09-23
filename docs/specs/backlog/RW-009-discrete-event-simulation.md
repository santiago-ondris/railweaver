# RW-009 — Motor de simulación por eventos y primera circulación

- Status: Backlog (intención)
- Milestone / release objetivo: M1 — Primer tren → V0.9, `v0.9.0`

## Goal

Primer motor de simulación discreta por eventos (ADR-006): reloj de simulación,
cola de eventos y una circulación de un tren sobre la red, reproducible y sin UI.
En el visor, el tren animado sobre el mapa y una línea de tiempo de eventos que
se puede reproducir.

## Por qué ahora

Es el primer paso de SIMULATE. Introduce la separación entre tiempo real, tiempo
de simulación y tiempo de render (Kickoff §11).

## Investigación previa

- Qué eventos tienen sentido con un solo tren y sin signalling (F0).
- Relojes, posiciones interpoladas y animación de entidades en CesiumJS.

## Decisiones previstas

- Framework de tests de frontend (Vitest), si la lógica de interpolación y línea de
  tiempo lo justifica. Con ADR.
