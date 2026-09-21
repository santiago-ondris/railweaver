# ADR-002 — C# + .NET 10 LTS

- Status: Accepted
- Date: 2026-09-21

## Context

El producto necesita tipado fuerte para modelar dominio, rendimiento suficiente para un motor de simulación, buen tooling y un ecosistema maduro. Se requiere una API HTTP para el frontend.

## Decision

- Lenguaje principal: **C#** sobre **.NET 10 (LTS)**. El SDK se fija en `global.json` (`10.0.100`, `rollForward: latestFeature`).
- API: **ASP.NET Core** (minimal APIs) como capa delgada, sin lógica ferroviaria.
- Tests: **xUnit v3** ejecutado con **Microsoft.Testing.Platform** (`global.json` → `test.runner`), el runner nativo de `dotnet test` en .NET 10. No se usan `Microsoft.NET.Test.Sdk` ni `xunit.runner.visualstudio`.
- Propiedades comunes (versión SemVer, target framework, nullable, warnings como errores) en `Directory.Build.props`.
- Formato de solución: `RailWeaver.slnx`.

Python queda permitido para investigación/prototipos (notebooks, GIS, Monte Carlo), no como segundo backend. Rust/C++ solo con evidencia de profiling.

## Consequences

- Warnings como errores desde el inicio mantienen el código limpio mientras es pequeño.
- Los IDEs necesitan soporte de Microsoft.Testing.Platform (Visual Studio, Rider y C# Dev Kit recientes lo tienen).

## Alternatives considered

- **Java:** técnicamente válido; sin ventaja sobre C# para este equipo.
- **Go:** sin rol actual.
- **Rust/C++ como core:** prematuro; se reserva para kernels numéricos justificados.
- **xUnit v2 (plantilla por defecto):** en modo mantenimiento; v3 es la línea activa.
