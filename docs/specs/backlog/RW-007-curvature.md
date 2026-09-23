# RW-007 — Curvas

- Status: Backlog (intención)
- Milestone / release objetivo: M1 — Primer tren → V0.7, `v0.7.0`

## Goal

Que el corredor candidato sea un trazado formado por rectas y arcos que respete un
radio mínimo (fidelidad de terreno V2, Kickoff §7), en lugar de la poligonal de la
malla de RW-005.

## Por qué ahora

Al verificar RW-005, el autor observó curvas no aptas para un proyecto ferroviario.
Además, la velocidad admisible en curva es un dato que RW-008 necesita.

## Investigación previa

- Radio mínimo según velocidad y trocha; peralte e insuficiencia de peralte.
- Curvas de transición (clotoides): si entran ahora o quedan como simplificación documentada.
- Compensación de pendiente en curva.
- Cómo suavizar sin romper el límite de pendiente (invariante de RW-005).

## Preguntas abiertas

- ¿Se mide también la curvatura de la red existente (geometría OSM) o solo la del corredor?
