# RW-010 — Construir: ramal unido a la red

- Status: Backlog (intención)
- Milestone / release objetivo: M1 — Primer tren → V0.10, `v0.10.0`. Cierra M1.

## Goal

Convertir un corredor candidato en infraestructura propuesta, unida a la red
existente mediante un empalme, y hacer circular un tren por ella. Cierra
`PLAN → BUILD → SIMULATE` y abre la path dependency (Kickoff §10): infraestructura
existente y propuesta, distinguidas.

## Por qué ahora

Es el objetivo visible de M1: el tren recorre un ramal diseñado por el usuario.

## Investigación previa

- Empalmes y desvíos: geometría mínima, velocidad por la vía desviada.
- Estados de infraestructura (existente, propuesta, en construcción).

## Decisiones previstas

- Persistencia de la red propuesta: archivos o sesión, o PostGIS. Si hace falta
  PostGIS, ADR sobre el acceso desde .NET (Npgsql, Dapper o EF Core +
  NetTopologySuite) y proyecto `RailWeaver.Persistence` separado del core.
