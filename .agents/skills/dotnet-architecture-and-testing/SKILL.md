---
name: dotnet-architecture-and-testing
description: >-
  Best practices for .NET backend development, clean architecture boundary protection,
  C# idioms, and deterministic unit testing. Use when working on RailWeaver.Core,
  RailWeaver.Api, or tests.
---

# .NET Architecture & Testing Guidelines

This skill guides the design, implementation, and verification of C# code within RailWeaver.

## Architecture Boundaries (ADR-001)

### 1. `RailWeaver.Core` Purity
- **Framework-Independent:** `RailWeaver.Core` must remain pure domain, simulation, and planning logic.
- **Strictly Prohibited in Core:**
  - `Microsoft.AspNetCore.*` (No HTTP context, controllers, or Web API dependencies).
  - Database drivers or ORMs (`Npgsql`, `Microsoft.EntityFrameworkCore`, etc.).
  - UI or rendering packages (Cesium, graphic drivers, etc.).
- **Enforcement:** The boundary is enforced by `tests/RailWeaver.Core.Tests/CoreBoundaryTests.cs`. Always run `dotnet test` to confirm no unwanted assemblies leak into Core.

### 2. `RailWeaver.Api` Thin Layer
- `RailWeaver.Api` is strictly an HTTP and serialization adapter.
- Never write railway domain rules, simulation calculations, or network routing algorithms directly in controller or endpoint classes. Delegate all domain processing to `RailWeaver.Core`.

## Modern C# Idioms & Simulation Rules

1. **Immutability First:**
   - Prefer `readonly record struct` or `record` for spatial coordinates, track coordinates (e.g. meter-markers, chainage), timetable snapshots, and simulation events.
   - Use primary constructors where they simplify initialization.
2. **Deterministic Simulation:**
   - Simulation state transitions must be deterministic functions of current state + inputs + elapsed delta time (tick-based).
   - Never rely on `DateTime.Now`, unseeded random generators, or asynchronous timers inside the simulation loop. Pass timestamps or ticks explicitly.
3. **Explicit Domain Errors:**
   - Avoid throwing raw system exceptions for standard railway domain validation (e.g. signal violations, track block occupied). Use typed domain results or specific domain exception types.

## Testing Guidelines

- **Unit Tests over Mocks:** Because `RailWeaver.Core` is framework-independent, domain logic should be tested directly with real POCOs rather than mocking frameworks.
- **Parametrized Tests:** Use xUnit `[Theory]` and `[InlineData]` to validate boundary conditions (e.g. minimum curve radius, braking curves, track switch branches).
- **Verification Commands:**
  ```bash
  dotnet build
  dotnet test
  ```
