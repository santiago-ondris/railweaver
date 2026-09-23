# RW-006 — La red como grafo

- Status: Backlog (intención; la spec completa se escribe después de la investigación)
- Milestone / release objetivo: M1 — Primer tren → V0.6, `v0.6.0`

## Goal

Pasar de los 2.374 tramos OSM sueltos de RW-003 a una red con topología: nodos,
conexiones y continuidad por trocha. Que el usuario elija dos estaciones y vea el
camino por la red real, con su perfil longitudinal, y que los huecos de datos se
vean en lugar de ocultarse.

## Por qué ahora

Es la base de todo M1: un tren necesita un camino por la red, y un ramal nuevo
necesita un punto de la red donde empalmar. Resuelve además dos non-goals previos:
el grafo de ruteo (RW-003) y el perfil de una vía existente (RW-004).

## Investigación previa

- Cómo representa OSM la conectividad (nodos compartidos, cruces a nivel sin
  conexión, puentes) y qué tan confiable es en Córdoba.
- Aparatos de vía (agujas y cruces): qué representan y qué movimientos permiten.
- Continuidad por trocha y el caso bitrocha de Córdoba Mitre (ver `project-status.md`).

## Preguntas abiertas

- ¿El grafo se construye offline (en `tools/`, versionado) o al cargar el dataset?
- ¿Cómo se marcan y explican las desconexiones sospechosas?
