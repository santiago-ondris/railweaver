# ADR-006 — Discrete-event simulation como paradigma inicial

- Status: Accepted
- Date: 2026-09-21

## Context

La operación ferroviaria se describe naturalmente como eventos (salida, entrada a bloque, llegada, pedido y bloqueo de ruta). Un loop de "tick" continuo, como en un videojuego, desperdicia cómputo y acopla simulación y render.

## Decision

La simulación seguirá **Discrete-Event Simulation**: una cola de prioridad de eventos ordenados por tiempo de simulación; el loop toma el evento más temprano, avanza el reloj, aplica el evento, actualiza el estado y agenda eventos futuros.

- Se distinguen **tiempo real**, **tiempo de simulación** y **tiempo de render**.
- Modos previstos: **determinista** (mismo input = mismo resultado), **estocástico** (distribuciones explícitas y seed reproducible) y **escenario** (eventos definidos por el usuario).
- Event sourcing se usará donde aporte (trazas, replay, causalidad), no como estrategia global de persistencia.
- La simulación batch debe poder ejecutarse sin UI.

Esta es una dirección: el motor no existe en v0.1.0 y se diseñará con su propia spec e investigación.

## Consequences

- La visualización interpola entre estados; no fuerza la frecuencia de la simulación.
- Fenómenos continuos (dinámica de tracción) se aproximarán con eventos calculados analíticamente cuando se llegue a ese nivel de fidelidad.

## Alternatives considered

- **Time-stepped (tick fijo):** simple pero costoso y ligado al render.
- **Híbrido continuo/discreto:** posible más adelante si la fidelidad de rolling stock lo exige.
