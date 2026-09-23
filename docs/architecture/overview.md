# Arquitectura — overview

Estado: v0.6.0 (red ferroviaria como grafo). Contexto conceptual completo: [Kickoff](../../RailWeaver_Project_Kickoff.md) §18–23.

## Forma general

Modular monolith ([ADR-001](../decisions/ADR-001-modular-monolith.md)).

```text
 src/web  (React + TS + Vite; CesiumJS en RW-001)
    │  HTTP /api
    ▼
 RailWeaver.Api  (ASP.NET Core, capa delgada) ◀── data/regions (datasets versionados)
    │  referencia de proyecto
    ▼
 RailWeaver.Core  (dominio, simulación, planificación — sin dependencias de frameworks)

 PostgreSQL + PostGIS  (compose.yaml; todavía sin uso desde el código)
```

## Proyectos actuales

| Proyecto | Rol | Puede depender de |
|---|---|---|
| `src/RailWeaver.Core` | Lógica de RailWeaver. Expone identidad/versión, value objects geográficos WGS84, elevación/perfiles, infraestructura ferroviaria, corredores candidatos en `Planning` y topología/rutas en `Infrastructure.Network`. | BCL de .NET |
| `src/RailWeaver.Api` | Traduce HTTP ↔ core. Expone health, regiones, ferrocarriles, cotas, perfiles, heightmaps, corredores y red/rutas; lee la grilla DEM por bloques y guarda una topología por región en memoria. | Core, ASP.NET Core |
| `tests/RailWeaver.Core.Tests` | Unit tests del core, datasets y tests de frontera. | Core, xUnit v3 |
| `tests/RailWeaver.Api.Tests` | Tests HTTP de los endpoints y sus contratos. | API, ASP.NET Core testing, xUnit v3 |
| `src/web` | UI React y visor CesiumJS; su lenguaje visual lo define [`DESIGN.md`](../../DESIGN.md) ([ADR-009](../decisions/ADR-009-visual-system.md)). Obtiene regiones, infraestructura, terreno y rutas vía API y solicita teselas de OpenStreetMap directamente desde el navegador. Se organiza en módulos (`regions`, `railways`, `elevation`, `planning`, `network`), más `app`, `viewer` y `shared` ([ADR-010](../decisions/ADR-010-frontend-structure-and-formatting.md)). | API por HTTP, CesiumJS, OpenStreetMap |

El DEM de cada región es un artefacto local fuera de git. La receta offline usa GDAL
en Docker; en runtime la API solo usa `ZLibStream` de la BCL, descomprime los bloques
necesarios y conserva como máximo 64 (~16 MB) en memoria. Cotas y pendientes se
calculan únicamente en backend; el heightmap es una proyección visual del mismo dato.
`Planning.CorridorFinder` consulta ese mismo DEM mediante `IElevationSource`, aplica
el límite de pendiente entre vértices de la línea de vía y devuelve un resultado
explícito si no hay camino. El navegador solo solicita y representa el resultado.
`Infrastructure.Network.RailwayNetworkBuilder` segmenta los GeoJSON de vía por
coordenadas compartidas, resuelve trochas sin dato por conexión y calcula diagnósticos.
`NetworkRouteFinder` busca sobre aristas dirigidas con costo (inversiones, metros),
según la regla angular de [RW-006](../specs/completed/RW-006-railway-graph.md). La API
construye una topología por región al primer pedido, sin persistir un grafo separado.

## Reglas de dependencia

- `RailWeaver.Core` no referencia ASP.NET Core, EF Core, Npgsql, ni ningún renderer. Lo verifica `CoreBoundaryTests`.
- PostgreSQL/PostGIS no contiene reglas ferroviarias.
- CesiumJS no conoce el simulation engine.
- La UI no modifica directamente estado de switches/signals.
- Los adapters (PostGIS, OSM, DEM) implementan abstracciones definidas por el core; no contaminan el dominio.
- Los datasets de región se versionan en `data/regions`; la API los lee desde archivos copiados al output y valida sus coordenadas e infraestructura con el core.

## Cómo crecerá

Los bounded contexts conceptuales (Geography, Planning, Infrastructure, Operations, Simulation, Signalling, Analysis) empiezan como **namespaces/carpetas dentro de `RailWeaver.Core`** cuando exista código real para ellos. No se crean carpetas ni proyectos vacíos.

Un contexto se separa en su propio proyecto cuando necesite frontera verificada por el compilador (por ejemplo, impedir que Signalling dependa de Analysis) o dependencias propias. El primer adapter con dependencias externas (p. ej. acceso a PostGIS) irá en un proyecto separado (`RailWeaver.Persistence` o similar) para que el core siga libre de drivers. Cada separación se registra en este documento.

## Tiempo

Se distinguen siempre tiempo real, tiempo de simulación y tiempo de render ([ADR-006](../decisions/ADR-006-discrete-event-simulation.md)). La simulación batch debe poder ejecutarse sin UI.
