# Visión de producto

Fuente extensa: [Kickoff](../../RailWeaver_Project_Kickoff.md) §1–4 y §14–16.

**RailWeaver es un sistema industrial ferroviario, con prácticas inspiradas en la industria real, que permite a un usuario simular la construcción de ramales de carga y pasajeros —inicialmente en la provincia de Córdoba, Argentina— y todas las implicaciones derivadas de dicha simulación en el tiempo.**

No es un videojuego tradicional ni un CRUD con temática ferroviaria: es un **sandbox de planificación y simulación** que responde preguntas del tipo *¿qué pasaría si…?*

## Loop de uso

```text
EXPLORE → PLAN → BUILD → SCHEDULE → SIMULATE → ANALYZE → IMPROVE ↺
```

## Rasgos esenciales

- **La red se comporta como un sistema.** Una decisión nueva afecta capacidad, estaciones, cruces, horarios, señalización, mantenimiento.
- **La complejidad emerge del modelo**, nunca de eventos artificiales para entretener. La aleatoriedad solo existe cuando representa un fenómeno real, y es explícita y con seed reproducible.
- **Sin condición de victoria.** El sistema muestra métricas, consecuencias y restricciones; la interpretación es del usuario.
- **Path dependency.** El mundo es persistente y evoluciona por etapas; decisiones viejas condicionan las nuevas.
- **Infraestructura ≠ servicios.** Varios servicios comparten la misma infraestructura.
- **Explicabilidad.** Más que "hay demora": qué pasó, por qué, qué fue causa y qué consecuencia.
- **Accesible primero.** Un usuario sin conocimientos ferroviarios debe poder empezar; el modo técnico expone el mismo core con más detalle.

## Objetivo visible actual

Diseñar un ramal, conectarlo a la red existente y ver a un tren recorrerlo, con un tiempo de viaje que se pueda explicar (milestone M1 — Primer tren).

El objetivo de M0 — Real World Skeleton (abrir un mapa real de Córdoba, seleccionar dos puntos y generar un corredor ferroviario básico que conozca la elevación del terreno) se cumplió en `v0.5.0`.
