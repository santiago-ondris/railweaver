# ADR-001 — Modular monolith

- Status: Accepted
- Date: 2026-09-21

## Context

El dominio ferroviario (geografía, planificación, infraestructura, operaciones, señalización, simulación, análisis) ya es suficientemente complejo. Distribuir el sistema agregaría complejidad operativa (red, consistencia, despliegue) sin resolver ningún problema actual. El proyecto lo desarrolla un equipo pequeño asistido por agentes.

## Decision

RailWeaver es un único proceso backend (ASP.NET Core) sobre un núcleo .NET (`RailWeaver.Core`) libre de dependencias de frameworks. Los bounded contexts se organizan como módulos internos: primero namespaces/carpetas dentro del core, y proyectos separados solo cuando una frontera necesite verificación del compilador o dependencias propias.

No se introducen microservices, brokers (Kafka, RabbitMQ), Redis, Kubernetes ni transacciones distribuidas sin un problema concreto documentado en un ADR.

## Consequences

- Refactors entre módulos son baratos; las fronteras se protegen con referencias de proyecto y tests de frontera (`CoreBoundaryTests`).
- La simulación batch puede ejecutarse in-process, sin UI ni red.
- Si en el futuro un componente (p. ej. un solver) necesita escalar o usar otro lenguaje, se extrae con evidencia (profiling) y un ADR.

## Alternatives considered

- **Microservices por contexto:** costo operativo alto, fronteras aún desconocidas.
- **Un proyecto por bounded context desde el día cero:** crea proyectos vacíos y fronteras especulativas.
