# Non-goals

Fuente extensa: [Kickoff](../../RailWeaver_Project_Kickoff.md) §3, §5, §15, §36, §38, §40.

## RailWeaver no es

- un sistema certificado de signalling;
- un sistema safety-critical apto para operación real;
- una herramienta profesional validada para aprobar obras;
- un reemplazo de estudios geotécnicos, hidrológicos, ambientales, estructurales, de expropiación o de ingeniería civil;
- un sistema de control ferroviario real;
- un juego con condición de victoria o puntuación global.

## Fuera de alcance por ahora

Sin un requisito concreto que lo justifique (y un ADR cuando corresponda), no introducir:

- microservices, Kafka, RabbitMQ, Redis, Kubernetes, transacciones distribuidas;
- CQRS o event sourcing globales;
- módulos nativos Rust/C++;
- AI en runtime (incluido Jev);
- railML, GTFS.

Durante M1 (Primer tren) se simula un solo tren con material rodante V1. No se implementan varios trenes ni capacidad, signalling, bloques, interlocking, timetables, dispatching, talleres, simulación de cargas, demanda de pasajeros, fallas, escenarios estocásticos, túneles, puentes, costos de suelo ni optimización avanzada.
