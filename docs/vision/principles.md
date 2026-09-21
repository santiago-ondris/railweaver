# Principios

Fuente extensa: [Kickoff](../../RailWeaver_Project_Kickoff.md) §5–7, §17–24, §36–37.

## Dominio

1. **No modelar nada que no podamos explicar.** Toda abstracción importante responde: qué representa, qué problema resuelve, qué fuentes la respaldan, qué simplifica, qué no simula todavía.
2. **Investigación antes de implementación** para conceptos ferroviarios relevantes (`docs/research/`).
3. **Fidelidad por niveles** (F0…F5, V1…V4). Siempre honestos sobre el nivel usado.
4. **Invariantes en el dominio**, nunca en el frontend. Estados explícitos (state machines) en lugar de combinaciones de flags.
5. **Reglas de seguridad deterministas.** Nunca dependen de AI ni de modelos probabilísticos.

## Arquitectura

6. **Modular monolith** ([ADR-001](../decisions/ADR-001-modular-monolith.md)). Polyglot y distribuido solo cuando esté justificado.
7. **El core no conoce** API, base de datos, renderer ni UI.
8. **No forzar todo a CRUD.** Grafos, colas de prioridad, rasters, índices espaciales, snapshots inmutables cuando correspondan.
9. **Discrete-event simulation** como paradigma inicial ([ADR-006](../decisions/ADR-006-discrete-event-simulation.md)). No simular continuamente lo que puede ser un evento; no renderizar con detalle lo que nadie mira.
10. **Event sourcing solo donde aporte** (replay, trazas, causalidad), no como persistencia global.
11. **Datos regionales como datos** ([ADR-007](../decisions/ADR-007-cordoba-first-dataset.md)).

## Proceso

12. **Agregar dependencias solo ante un requisito concreto.**
13. **El repositorio es la memoria compartida**; los agentes son trabajadores descartables. Una decisión que vive solo en un chat está perdida.
14. **Specs versionadas** sobre prompts largos; **ADRs** para decisiones importantes.
15. **Vertical slices pequeños + buenos límites + supuestos documentados** sobre arquitectura especulativa.
