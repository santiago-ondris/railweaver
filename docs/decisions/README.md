# Architecture Decision Records

Una decisión importante que no está acá no existe. No reabrir un ADR aceptado sin un motivo concreto; si cambia, crear un ADR nuevo que lo reemplace (`Superseded by ADR-NNN`).

| ADR | Decisión | Estado |
|---|---|---|
| [ADR-001](ADR-001-modular-monolith.md) | Modular monolith | Accepted |
| [ADR-002](ADR-002-dotnet-10.md) | C# + .NET 10 LTS, ASP.NET Core como capa delgada, xUnit | Accepted |
| [ADR-003](ADR-003-postgresql-postgis.md) | PostgreSQL + PostGIS | Accepted |
| [ADR-004](ADR-004-react-typescript-vite.md) | React + TypeScript + Vite | Accepted |
| [ADR-005](ADR-005-cesiumjs-primary-renderer.md) | CesiumJS como renderer geoespacial principal | Accepted |
| [ADR-006](ADR-006-discrete-event-simulation.md) | Discrete-event simulation como paradigma inicial | Accepted |
| [ADR-007](ADR-007-cordoba-first-dataset.md) | Córdoba como primer dataset, no dependencia del core | Accepted |
| [ADR-008](ADR-008-language-conventions.md) | Castellano para documentación, inglés para todo lo demás | Accepted |
| [ADR-009](ADR-009-visual-system.md) | Sistema visual: dirección híbrida, tema claro y `DESIGN.md` | Accepted |

## Plantilla

```markdown
# ADR-NNN — Título

- Status: Proposed | Accepted | Superseded by ADR-NNN
- Date: YYYY-MM-DD

## Context
## Decision
## Consequences
## Alternatives considered
```
