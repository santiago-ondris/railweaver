# RailWeaver — Project Kickoff

## 1. Definición canónica

**RailWeaver es un sistema industrial ferroviario, con prácticas inspiradas en la industria real, que permite a un usuario simular la construcción de ramales de carga y pasajeros —inicialmente en la provincia de Córdoba, Argentina— y todas las implicaciones derivadas de dicha simulación en el tiempo.**

RailWeaver no nace como un videojuego tradicional ni como una aplicación CRUD con temática ferroviaria.

Es un **sandbox de planificación y simulación ferroviaria**.

El usuario parte de infraestructura existente, puede proponer nuevos ramales, reutilizar o modificar trazas actuales, incorporar servicios de pasajeros y cargas, y posteriormente simular cómo funciona la red resultante en el tiempo.

La red debe comportarse como un sistema: una decisión nueva puede afectar capacidad, estaciones, cruces, horarios, talleres, señalización, infraestructura, demoras, mantenimiento y otros componentes ya existentes.

El objetivo principal es responder preguntas del tipo:

> **¿Qué pasaría si...?**

Ejemplos:

- ¿Qué pasaría si se crea un servicio de pasajeros Córdoba–Calchín reutilizando infraestructura por la que ya circulan cargas?
- ¿Qué pasa si una estación existente recibe cuatro nuevos servicios?
- ¿Conviene reutilizar una traza histórica o construir una nueva?
- ¿Qué ocurre si una línea de vía simple aumenta su frecuencia?
- ¿Dónde aparece el siguiente cuello de botella?
- ¿Qué infraestructura adicional requiere un nuevo servicio?
- ¿Cómo se propagan los retrasos?
- ¿Qué cambia si se duplica un tramo, se agrega un apartadero o se modifica el horario?
- ¿Cómo evolucionaría una red provincial construida incrementalmente durante décadas?

---

# 2. Visión del producto

RailWeaver debe permitir eventualmente un loop como:

```text
EXPLORE
   ↓
PLAN
   ↓
BUILD
   ↓
SCHEDULE
   ↓
SIMULATE
   ↓
ANALYZE
   ↓
IMPROVE
   ↺
```

La experiencia ideal comienza siendo accesible para una persona sin conocimientos ferroviarios.

Un usuario debería poder entrar y pensar:

> “Quiero un tren entre Córdoba y Carlos Paz.”

RailWeaver debe ayudarlo a descubrir progresivamente que esa decisión implica terreno, pendientes, infraestructura existente, estaciones, capacidad, frecuencias, cruces, señalización, material rodante y operación.

La complejidad debe **emerger del modelo**, no de mecánicas artificiales de videojuego.

No se deben crear problemas arbitrarios simplemente para entretener.

Ejemplo deseado:

```text
vía simple
+
dos servicios frecuentes
+
trenes con diferentes velocidades
+
horarios incompatibles
=
congestión
```

No:

```text
if userHasPlayedFor20Minutes:
    triggerRandomRailwayProblem()
```

Puede existir incertidumbre y comportamiento estocástico cuando represente fenómenos reales, pero debe ser explícito y configurable.

---

# 3. RailWeaver no tiene una condición de victoria

RailWeaver posee un espíritu de “partida” porque:

- existe un mundo persistente;
- el usuario toma decisiones;
- esas decisiones generan consecuencias;
- la red evoluciona;
- infraestructura antigua condiciona decisiones futuras;
- una solución local puede generar un problema global.

Pero no existe un objetivo universal de victoria.

No hay:

```text
YOU WIN
```

Ni una puntuación ferroviaria global arbitraria.

El sistema muestra métricas, consecuencias, restricciones y problemas.

La interpretación pertenece al usuario.

Ejemplo:

```text
Puntualidad             92%
Retraso medio         2m 14s
Tramos críticos           2
Servicios saturados       3
Capacidad talleres       71%
```

Una red con 99.9% de puntualidad pero enorme infraestructura subutilizada no es automáticamente “mejor” que otra.

---

# 4. Alcance geográfico

El primer mundo de RailWeaver será:

**Provincia de Córdoba, Argentina.**

Córdoba es un dataset y escenario inicial, no una dependencia conceptual del core.

La arquitectura debe permitir eventualmente:

```text
RailWeaver Core
      │
      ├── Córdoba
      ├── Mendoza
      ├── Argentina
      └── cualquier otra región
```

El core nunca debe asumir nombres, coordenadas, normas locales o infraestructura específica de Córdoba salvo a través de datos/configuración.

---

# 5. Filosofía de fidelidad

La intención es aproximarse progresivamente a prácticas ferroviarias reales.

Esto NO significa que RailWeaver V0 sea software certificado para operar infraestructura ferroviaria real.

RailWeaver es inicialmente:

**software de simulación, análisis y experimentación.**

No debe presentarse como:

- sistema certificado de signalling;
- sistema safety-critical apto para operación real;
- herramienta profesional validada para aprobar obras;
- reemplazo de estudios geotécnicos, ambientales o de ingeniería civil;
- sistema de control ferroviario real.

La fidelidad se construirá incrementalmente.

## Regla fundamental

> **No modelar nada que no podamos explicar.**

Para toda abstracción importante debemos poder responder:

1. ¿Qué entidad o fenómeno real representa?
2. ¿Qué problema ferroviario resuelve?
3. ¿Qué fuentes respaldan nuestro entendimiento?
4. ¿Qué simplificaciones estamos haciendo?
5. ¿Qué NO estamos simulando todavía?

No se debe incorporar comportamiento ferroviario porque “parece lógico”.

---

# 6. Desarrollo guiado por investigación de dominio

RailWeaver es simultáneamente:

- un proyecto de software;
- un proyecto de aprendizaje ferroviario;
- un proyecto de simulación;
- un proyecto GIS;
- un laboratorio de algoritmos.

Antes de implementar conceptos ferroviarios relevantes debe existir investigación mínima.

Ejemplo:

```text
docs/research/signalling/interlocking.md
```

Debe contener idealmente:

```text
Question
Sources
Real-world behavior
CordobaRail/RailWeaver abstraction
Safety invariants
Simplifications
Unknowns
Confidence
```

Fuentes preferidas:

- documentación ferroviaria oficial;
- CNRT / normativa argentina;
- organismos ferroviarios;
- EULYNX;
- railML;
- ERA;
- documentación académica/industrial;
- libros y papers especializados.

Fuentes secundarias pueden ayudar a comprender conceptos, pero no deben ser la única base de reglas críticas.

---

# 7. Niveles de fidelidad

No intentar implementar toda la ingeniería ferroviaria en V1.

Cada dominio debe poder evolucionar por niveles.

Ejemplo conceptual para signalling:

```text
F0
Sin signalling real.
Solo evitamos ocupaciones incompatibles básicas.

F1
Fixed blocks.

F2
Señales derivadas de ocupación de bloques.

F3
Routes + switches + movimientos conflictivos.

F4
Interlocking.

F5
Sistemas reales específicos, fallas y operación degradada.
```

Ejemplo para rolling stock:

```text
V1
length
maxSpeed
acceleration
brakingRate

V2
mass
tractive effort
gradient effects

V3
traction curves
consist composition
braking characteristics

V4
real rolling-stock profiles
```

Ejemplo para terreno:

```text
V1
elevation
maximum gradient

V2
curve radius
basic earthworks approximation

V3
bridges
tunnels
water crossings

V4
geology
hydrology
environmental constraints
```

RailWeaver debe ser siempre honesto respecto al nivel de fidelidad utilizado.

---

# 8. Dominio conceptual inicial

No todo debe implementarse inmediatamente.

Este mapa sirve para conocer áreas que eventualmente pueden existir.

```text
RAILWAY SYSTEM

Infrastructure
├── Track
├── Track Segment
├── Junction
├── Switch / Point
├── Station
├── Platform
├── Passing Loop
├── Depot
├── Workshop
├── Bridge
├── Tunnel
├── Level Crossing
└── Signalling Equipment

Operations
├── Service
├── Timetable
├── Stop
├── Frequency
├── Priority
├── Dispatching
├── Capacity
└── Incident

Signalling / Safety
├── Block
├── Signal
├── Route
├── Point
├── Interlocking
├── Train Detection
└── Movement Authority

Rolling Stock
├── Train
├── Consist
├── Length
├── Mass
├── Power
├── Max Speed
├── Acceleration
└── Braking

Planning
├── Terrain
├── Alignment
├── Gradient
├── Curvature
├── Tunnel
├── Bridge
├── Earthworks
├── Existing Infrastructure
└── Candidate Corridor

Simulation
├── Simulation Clock
├── Event Queue
├── Runtime State
├── Deterministic Scenario
├── Stochastic Scenario
└── Replay

Analysis
├── Capacity
├── Delay
├── Utilization
├── Bottlenecks
├── Reliability
├── Root Cause
└── Scenario Comparison
```

---

# 9. Separar infraestructura de servicios

Concepto fundamental:

**una línea de servicio no es la infraestructura.**

No modelar:

```text
Córdoba-SantiagoTempleLine
Córdoba-CalchínLine
```

como redes independientes.

Modelar:

```text
Infrastructure Network
├── track segments
├── junctions
├── stations
├── signals
├── depots
└── workshops
```

y por separado:

```text
Services
├── Freight Service A
├── Regional Passenger Service B
└── Express Service C
```

Varios servicios pueden reutilizar la misma infraestructura.

Ejemplo:

```text
TrackSegment TS-182

Used by:
- Córdoba → Santiago Temple freight
- Córdoba → Calchín regional
- Córdoba → Arroyito express
```

Esto permite que cualquier modificación o incidente afecte automáticamente a todos los servicios dependientes.

---

# 10. Evolución incremental del mundo

RailWeaver debe poder representar path dependency.

La red no se genera siempre desde cero.

Ejemplo:

```text
Stage 1
Córdoba → Santiago Temple cargo

Stage 2
Córdoba → Calchín passenger

Stage 3
Calchín → San Francisco

Stage 4
Córdoba → Jesús María

Stage 5
Freight bypass Córdoba

Stage 6
New Córdoba Central

Stage 7
Double track east corridor
```

La infraestructura de Stage 7 debe conservar la historia de las decisiones anteriores.

Decisiones viejas pueden producir:

- estaciones difíciles de ampliar;
- cuellos de botella;
- trazados heredados;
- infraestructura subutilizada;
- talleres ubicados en lugares poco óptimos;
- oportunidades de reutilización;
- necesidades de bypass;
- necesidad de señalización más avanzada.

---

# 11. Simulación temporal

RailWeaver debe usar como paradigma inicial:

**Discrete-Event Simulation.**

No asumir que todo debe actualizarse constantemente como un videojuego.

Conceptualmente:

```text
PriorityQueue<Event>
```

Ejemplo:

```text
08:30:00 TrainDeparted
08:31:42 TrainEnteredBlock
08:32:03 TrainArrivedAtStation
08:32:20 RouteRequested
08:32:21 RouteLocked
```

Loop:

```text
pop earliest event
↓
advance simulation clock
↓
apply event
↓
update world state
↓
generate future events
↓
repeat
```

Distinguir siempre:

```text
REAL TIME
tiempo que tarda la computadora

SIMULATION TIME
tiempo dentro del escenario

RENDER TIME
frecuencia con que se actualiza la UI
```

La visualización puede interpolar movimientos entre estados de simulación.

---

# 12. Modos de simulación futuros

El diseño debe permitir eventualmente:

## Deterministic

Mismo input + misma configuración = mismo resultado.

Uso:

- debugging;
- comparación de infraestructura;
- tests;
- reproducibilidad.

## Stochastic

Fenómenos realistas con distribuciones probabilísticas.

Ejemplos:

- dwell time;
- retrasos menores;
- fallos;
- variabilidad de demanda.

Debe utilizar seed reproducible.

## Scenario

Eventos definidos explícitamente.

Ejemplos:

- vía cerrada;
- plataforma fuera de servicio;
- señal averiada;
- tormenta;
- tren detenido;
- alta demanda.

---

# 13. Explicabilidad

RailWeaver no debe limitarse a mostrar:

```text
⚠ Delay
```

Debe intentar responder:

```text
¿Qué pasó?
¿Por qué pasó?
¿Qué fue causa y qué fue consecuencia?
¿Qué recursos estaban saturados?
¿Qué alternativas existen?
```

Ejemplo:

```text
Train R203 delayed +11m

Root cause:
R101 departed +2m

Propagation:
missed crossing slot
↓
waited for F102
↓
arrived late at Alta Córdoba
↓
platform occupied
↓
additional +3m
```

Debe ser posible eventualmente representar una cadena causal y replay del escenario.

---

# 14. Experiencia del usuario

El usuario objetivo inicial NO debe necesitar conocimientos ferroviarios.

Modo accesible:

```text
Capacidad buena
Capacidad limitada
Congestión

"Los trenes no tienen suficientes lugares para cruzarse."
```

Modo técnico futuro:

```text
Block occupancy
Headway
Route conflicts
Signal aspects
Interlocking state
Junction utilization
```

Mismo simulation core; distinto nivel de exposición.

---

# 15. Route / Tramo Generator

Un subsistema central futuro será el generador de corredores ferroviarios.

Input conceptual:

```text
Origin
Destination
Terrain
ExistingInfrastructure
Constraints
```

Debe considerar progresivamente:

- elevation;
- slope;
- maximum railway gradient;
- curvature;
- existing rail;
- urban areas;
- rivers;
- tunnels;
- bridges;
- protected areas;
- geology where data exists;
- environmental constraints;
- construction uncertainty.

Output:

```text
CandidateRoute[]
```

Cada candidato debería eventualmente exponer:

```text
Length
Maximum gradient
Minimum curve radius
Tunnel length
Bridge length
Existing infrastructure reuse
Approximate construction difficulty
Travel time estimate
Confidence / missing data
```

Importante:

RailWeaver puede hacer análisis preliminar.

No reemplaza estudios reales de:

- geotecnia;
- hidrología;
- impacto ambiental;
- estructuras;
- expropiación;
- ingeniería civil.

Debe expresar incertidumbre.

Ejemplo:

```text
Terrain confidence        HIGH
Hydrology confidence      MEDIUM
Geotechnical confidence   LOW
Environmental confidence  LOW
```

---

# 16. Visualización

RailWeaver debe ser visualmente satisfactorio.

El lenguaje visual (paleta, tipografía, semántica de color, cartografía y gráficos) está definido en `DESIGN.md` ([ADR-009](docs/decisions/ADR-009-visual-system.md)): una sala de control clara sobre una mesa de dibujo técnico, donde lo normal es gris y el color señala desvíos y servicios.

La interfaz principal futura debe permitir:

- mapa 3D/2.5D;
- terreno real;
- vías existentes;
- candidate routes;
- estaciones;
- trenes;
- puentes;
- túneles;
- junctions;
- capas analíticas.

Vistas importantes futuras:

## Geographic / Planning view

Tipo GIS / Google Earth.

Capas:

- terrain;
- elevation;
- slope;
- population centers;
- existing railway;
- proposed infrastructure;
- environmental constraints;
- construction difficulty.

## Vertical profile

Perfil longitudinal de terreno + alineamiento ferroviario.

## Operations view

Diagrama esquemático ferroviario:

```text
CÓRDOBA      STATION X      DESTINATION

━━━●━━━━━━━━●━━━━━━━━━━●━━━
    T101 ►
             ◄ T202
```

## Replay / Timeline

Permitir inspeccionar eventos y propagación de problemas.

---

# 17. Performance philosophy

RailWeaver puede volverse intensivo, pero no debe copiar ingenuamente el modelo de un city builder.

Reglas:

> **No simular continuamente lo que puede representarse como evento.**

> **No renderizar con detalle lo que el usuario no está mirando.**

Separar:

- simulation performance;
- GIS processing;
- rendering;
- persistence.

Batch simulation debe poder ejecutarse sin UI.

Ejemplo futuro:

```text
railweaver simulate scenario.json --24-hours
```

Output:

```text
Train movements: ...
Average delay: ...
Conflicts: ...
Max junction utilization: ...
```

---

# 18. Arquitectura inicial

RailWeaver comienza como:

**Modular Monolith.**

NO microservices inicialmente.

La complejidad del dominio ya es suficiente.

No introducir:

- Kafka;
- RabbitMQ;
- Kubernetes;
- Redis;
- microservices;
- distributed transactions;

sin un problema concreto que los justifique.

Principio:

> **Polyglot and distributed only when justified.**

---

# 19. Bounded contexts / módulos conceptuales

Estructura inicial aproximada:

```text
RailWeaver

├── Domain
├── Geography
├── Planning
├── Infrastructure
├── Operations
├── Simulation
├── Signalling
├── Analysis
├── Persistence
└── Visualization
```

No todos tienen que existir en V0.

Responsabilidades conceptuales:

## Planning

Propone infraestructura.

Vocabulario:

```text
CandidateCorridor
Alignment
TerrainCost
GradientProfile
```

## Infrastructure

Representa red construida/existente.

```text
TrackSegment
Station
Platform
Junction
Switch
Tunnel
Bridge
```

## Operations

Define cómo se utiliza infraestructura.

```text
Service
Timetable
Stop
Frequency
Priority
```

## Simulation

Posee runtime state y tiempo.

```text
SimulationClock
EventQueue
WorldState
Scenario
```

## Signalling

Reglas de movimiento y seguridad ferroviaria.

```text
Block
Signal
Route
Point
Interlocking
```

## Analysis

Datos derivados.

```text
Delay
Capacity
Utilization
Bottleneck
ScenarioComparison
```

---

# 20. Arquitectura técnica

Conceptualmente:

```text
                    Web UI
             React + TypeScript
                    │
                    ▼
               ASP.NET API
                    │
                    ▼
        ┌─────────────────────────┐
        │      APPLICATION        │
        └────────────┬────────────┘
                     │
        ┌────────────┴─────────────┐
        │                          │
        ▼                          ▼
 Railway Domain              Simulation Core
        │                          │
        ├─────────────┬────────────┤
        ▼             ▼            ▼
    Planning      Signalling    Operations
        │
        ▼
 Geography / spatial abstractions
        │
        ▼
       Adapters
   PostGIS / OSM / DEM
```

Reglas:

- Cesium NO conoce el simulation engine.
- PostgreSQL/PostGIS NO contiene reglas ferroviarias.
- ASP.NET NO decide lógica ferroviaria.
- UI NO modifica directamente estado interno de switches/signals.
- Infrastructure adapters NO deben contaminar el domain core.

---

# 21. No forzar todo a CRUD

Evitar reducir el proyecto a:

```text
Controller
Service
Repository
Entity
```

RailWeaver utilizará estructuras más apropiadas cuando corresponda:

- graphs;
- grids;
- rasters;
- state machines;
- priority queues;
- spatial indexes;
- immutable snapshots;
- time series;
- optimization models;
- event streams.

Ejemplos:

```text
RailwayGraph
TopologyIndex
BlockOccupancyMap
RouteConflictMatrix
EventQueue
SpatialIndex
```

---

# 22. State machines

Modelar estados explícitamente cuando sea apropiado.

Ejemplo:

```text
Switch

NORMAL
REVERSE
MOVING
FAILED
LOCKED
```

Ejemplo:

```text
Route

REQUESTED
SETTING
LOCKED
OCCUPIED
RELEASING
FREE
```

Evitar combinaciones ambiguas de boolean flags.

---

# 23. Invariantes

Las reglas críticas deben expresarse como invariantes de dominio.

Ejemplos futuros:

```text
Two conflicting routes cannot be locked simultaneously.

A switch cannot move while locked by an active route.

A train cannot enter an occupied block unless explicitly permitted
by the implemented signalling model.

A train cannot occupy disconnected track sections.
```

Las invariantes no pertenecen al frontend.

---

# 24. Testing philosophy

Testing debe evolucionar junto al dominio.

Inicial:

- unit tests;
- integration tests;
- deterministic scenario tests.

Después:

- property-based testing;
- invariant testing;
- simulation replay;
- randomized scenario generation;
- performance benchmarks.

Ejemplo de property:

```text
For every generated valid scenario:

never allow two incompatible routes to be established simultaneously.
```

---

# 25. Stack inicial aprobado

## Core / Backend

**C# + .NET 10 LTS**

Motivos:

- strong typing;
- performance suficiente;
- excelente tooling;
- ecosistema maduro;
- adecuado para domain modeling;
- adecuado para simulation engines;
- permite optimización antes de recurrir a native code.

## API

**ASP.NET Core**

Debe ser una capa fina.

## Database

**PostgreSQL + PostGIS**

Para:

- geometry;
- spatial queries;
- infrastructure persistence;
- geographic data;
- intersections;
- distances;
- spatial indexing.

## Frontend

**React + TypeScript + Vite**

Mantener frontend tecnológico deliberadamente convencional.

## Geospatial visualization

**CesiumJS**

Inicialmente principal renderer geoespacial.

Debe soportar:

- terrain;
- real geography;
- GeoJSON;
- glTF;
- future time-dynamic visualization.

MapLibre puede evaluarse en el futuro para vistas 2D específicas.

## GIS desktop tooling

**QGIS**

Herramienta externa para:

- inspeccionar datasets;
- transformar formatos;
- validar información;
- experimentar análisis GIS.

No es dependencia runtime.

## Geospatial libraries

Introducir cuando haya necesidad concreta:

```text
GDAL
PROJ
GEOS
```

## Data sources

Inicialmente:

```text
OpenStreetMap
Digital Elevation Model
```

DEM candidate inicial:

- Copernicus DEM u otra fuente adecuada evaluada durante implementación.

## Testing

**xUnit v3** sobre Microsoft.Testing.Platform (ver ADR-002).

Property-based testing posteriormente.

## Development infrastructure

**Docker Compose**

Inicialmente solamente para servicios requeridos, principalmente:

```text
PostgreSQL + PostGIS
```

No dockerizar innecesariamente frontend/backend durante desarrollo local.

---

# 26. Lenguajes

## C#

Lenguaje principal del producto.

## TypeScript

Frontend y visualización web.

## Python

Permitido desde V0 como herramienta de:

- research;
- notebooks;
- GIS experiments;
- algorithm prototyping;
- data analysis;
- Monte Carlo analysis;
- visualization of research results.

No usar Python como segundo backend simplemente porque sea cómodo.

Ejemplo:

```text
Python
→ prototype algorithm

C#
→ production implementation
```

## Rust

No inicial.

Candidato futuro para:

- high-performance solver;
- spatial processing;
- native simulation kernels.

Solo introducir con evidencia de profiling o necesidad clara.

## C++

No inicial.

Candidato futuro para:

- native optimization;
- numerical kernel;
- specialized integration.

No reescribir componentes en C++ por prestigio o anticipación.

## Go

Técnicamente válido, sin rol actual.

No introducir sin necesidad concreta.

## Java

Técnicamente válido como alternativa a C#.

No tiene rol dentro del stack actual.

---

# 26.1 Versionado y releases

RailWeaver utiliza **Semantic Versioning (SemVer)**:

```text
MAJOR.MINOR.PATCH
```

Ejemplos:

```text
v0.1.0
v0.2.0
v0.2.1
v1.0.0
```

Mientras el proyecto esté en `0.x`, se considera en evolución activa y no se promete estabilidad total de APIs, formatos de escenarios o modelos de dominio.

Convención durante `0.x`:

```text
MINOR
Nueva capacidad significativa y utilizable.

PATCH
Correcciones, mejoras internas, documentación o cambios que no agregan
una nueva capacidad significativa para el usuario.
```

Ejemplos conceptuales:

```text
v0.1.0
Primer workspace ejecutable (V0.1).

v0.2.0
Geographic viewer: mapa geográfico base (V0.2).

v0.3.0
Infraestructura ferroviaria existente visible (V0.3).

v0.4.0
Terrain/DEM + elevation profile (V0.4).

v0.4.1
Corrección de cálculo de elevación sin nueva capacidad funcional.

v0.5.0
Primer candidate corridor generado (V0.5).
```

Las etapas V0.x de la sección 39 se corresponden con los releases `v0.x.0` (tras `v0.9.0` sigue `v0.10.0`).

`1.0.0` NO significa que RailWeaver haya simulado todo el dominio ferroviario.

Significa que existe una primera plataforma públicamente coherente, documentada y suficientemente estable como para que escenarios y workflows principales puedan considerarse una base soportada.

Cada release debe:

- tener un Git tag `vX.Y.Z`;
- actualizar `CHANGELOG.md`;
- mantener consistencia entre versión, funcionalidades realmente disponibles y documentación;
- evitar incrementar versiones por trabajo incompleto o puramente experimental no integrado.

No introducir un sistema de versionado paralelo diferente de SemVer salvo que exista una necesidad técnica concreta para datasets, schemas o formatos de escenario. Si en el futuro esos artefactos necesitan su propio versionado, documentarlo mediante ADR.

---

# 27. AI dentro de RailWeaver

Las reglas ferroviarias deterministas y safety-related NO deben depender de modelos probabilísticos.

No usar AI para decidir:

```text
Can this signal clear?
Can this route be locked?
Can this train enter this occupied block?
```

Estas respuestas pertenecen a reglas deterministas.

AI puede evaluarse posteriormente para:

- dispatching suggestions;
- scenario classification;
- explanation;
- development tooling;
- result triage.

## Jev

No forma parte de V0.

Posible futuro uso:

```text
structured simulation state
↓
typed probabilistic decision
↓
dispatch recommendation
```

Debe implementarse detrás de abstracciones.

Ejemplo futuro:

```text
IDispatchDecisionPolicy

RuleBasedDispatcher
OptimizationDispatcher
JevDispatcher
HumanDispatcher
```

RailWeaver debe funcionar si Jev desaparece.

---

# 28. AI como herramienta de desarrollo

Herramientas disponibles:

- Codex
- Claude Code
- Antigravity
- ChatGPT
- Obsidian

Regla central:

> **El repository es la memoria compartida. Los agentes son trabajadores descartables.**

No depender de memoria individual de conversaciones.

Una decisión importante que solamente vive en un chat se considera perdida.

---

# 29. Estructura documental propuesta

```text
RailWeaver/
│
├── AGENTS.md
├── CLAUDE.md
├── DESIGN.md            sistema visual para agentes (ADR-009)
│
├── docs/
│   ├── vision/
│   │   ├── product-vision.md
│   │   ├── principles.md
│   │   └── non-goals.md
│   │
│   ├── architecture/
│   │   ├── overview.md
│   │   ├── bounded-contexts.md
│   │   ├── simulation-engine.md
│   │   └── data-flow.md
│   │
│   ├── domain/
│   │   ├── glossary.md
│   │   ├── infrastructure.md
│   │   ├── operations.md
│   │   ├── signalling.md
│   │   └── rolling-stock.md
│   │
│   ├── research/
│   │   ├── terrain-routing/
│   │   ├── signalling/
│   │   ├── operations/
│   │   └── rolling-stock/
│   │
│   ├── decisions/
│   │
│   ├── specs/
│   │   ├── active/
│   │   ├── backlog/
│   │   └── completed/
│   │
│   └── status/
│       └── project-status.md
│
├── src/
│   ├── RailWeaver.Core/     núcleo sin dependencias de frameworks
│   ├── RailWeaver.Api/      API ASP.NET Core delgada
│   └── web/                 frontend React + TypeScript + Vite
├── tests/
├── research/
│   └── notebooks/
└── tools/
```

`docs/` puede utilizarse como Obsidian vault.

Las carpetas se crean cuando tienen contenido real, no por anticipado. La estructura vigente está descripta en `docs/architecture/overview.md`.

Evitar una segunda wiki que duplique conocimiento.

---

# 30. Agent context strategy

## AGENTS.md

Debe ser corto.

Ejemplo futuro:

```text
# RailWeaver

RailWeaver is a railway planning and simulation sandbox.

Before substantial work read:

- docs/vision/product-vision.md
- docs/architecture/overview.md
- docs/status/project-status.md

Rules:

- Never invent railway behavior.
- Document domain assumptions.
- Keep simulation core independent from visualization.
- Deterministic safety rules cannot depend on AI.
- Architectural decisions require ADRs.
- Avoid speculative infrastructure.
- Update project-status.md after substantial work.
```

Puede existir AGENTS.md específico dentro de módulos.

Ejemplo:

```text
src/Signalling/AGENTS.md
```

## CLAUDE.md

Debe importar/referenciar documentación canónica y evitar duplicar grandes bloques.

---

# 31. Specs sobre prompts largos

Trabajo significativo debe partir idealmente de una spec versionada.

Ejemplo:

```text
docs/specs/active/RW-004-terrain-route-generator.md
```

Formato:

```text
# RW-004 Terrain Route Generator

Goal

Scope

Non-goals

Inputs

Outputs

Acceptance criteria

Relevant domain docs

Relevant architecture

Tests

Open questions
```

Cualquier agente debe poder tomar una spec y empezar a trabajar sin una explicación oral extensa.

---

# 32. Multi-agent development

No asignar permanentemente:

```text
Codex = backend
Claude = research
Antigravity = frontend
```

Asignar según tarea y conveniencia.

Para mantener la agilidad del proyecto indie, **no hay burocracia de pull requests ni reviews obligatorias entre modelos**. El autor implementa con un agente, corre las pruebas y mergea directo a `main`.

La review con otro modelo es una herramienta opcional bajo demanda: si en una feature compleja o delicada el autor quiere una segunda mirada, puede abrir una sesión de review con otro agente antes de cerrar.

---

# 33. Git worktrees para agentes paralelos

Cuando haya trabajo paralelo:

```text
RailWeaver/
RailWeaver-worktrees/
    RW-031/
    RW-032/
    RW-033/
```

Branches:

```text
feature/RW-031-elevation-profile
research/RW-032-fixed-block-signalling
feature/RW-033-map-renderer
```

Evitar dos agentes modificando simultáneamente la misma working tree.

---

# 34. Project status

Mantener:

```text
docs/status/project-status.md
```

Formato sugerido:

```text
# RailWeaver Status

Last updated:

## Current milestone

## Working

## In progress

## Next

## Known problems

## Open research questions

## Recent architectural decisions
```

Una sesión nueva debe poder reconstruir el contexto leyendo:

1. product vision;
2. architecture overview;
3. project status;
4. spec activa.

---

# 35. Architecture Decision Records

Decisiones importantes deben persistirse.

Ejemplo:

```text
docs/decisions/ADR-001-discrete-event-simulation.md
docs/decisions/ADR-002-postgis.md
docs/decisions/ADR-003-cesium-primary-renderer.md
```

Formato:

```text
Context
Decision
Consequences
Alternatives considered
Status
```

---

# 36. Principios anti-complejidad

No introducir infraestructura porque “eventualmente puede servir”.

Regla:

> **Add dependencies only when a concrete requirement justifies them.**

Evitar inicialmente:

```text
microservices
Kafka
RabbitMQ
Redis
Kubernetes
global CQRS
global event sourcing
Rust/C++ native modules
Jev
railML
GTFS
```

Algunos podrán aparecer.

Ninguno es necesario para demostrar V0.

---

# 37. Event sourcing

Los eventos son naturales para la simulación:

```text
TrainDeparted
TrainEnteredBlock
RouteLocked
SwitchMoved
DelayDetected
```

Pero NO asumir global event sourcing desde el inicio.

Puede utilizarse donde aporte valor:

- replay;
- simulation trace;
- causality;
- debugging.

No convertirlo automáticamente en estrategia de persistencia de toda la aplicación.

---

# 38. Milestones

Cada milestone tiene un objetivo visible expresable en una frase. Se planifican como máximo los próximos ~5 pasos: solo la spec siguiente se escribe completa; las demás son intenciones cortas en `docs/specs/backlog/`, que se revisan al cerrar cada spec.

## M0 — Real World Skeleton (completada, `v0.5.0`)

Demostró que RailWeaver puede representar un fragmento del mundo ferroviario sobre geografía real:

```text
real Córdoba data → map → existing railway geometry
→ select origin/destination → basic candidate corridor → elevation profile
```

## M1 — Primer tren (activa)

Objetivo:

**diseñar un ramal, conectarlo a la red existente y ver a un tren recorrerlo, con un tiempo de viaje que se pueda explicar.**

Cierra por primera vez el loop `PLAN → BUILD → SIMULATE`.

Vertical slice deseado:

```text
existing railway as a graph
↓
realistic alignment (curves)
↓
train + explainable running time
↓
discrete-event simulation of one train
↓
build: candidate corridor joined to the network
```

Esta milestone NO necesita:

- varios trenes ni capacidad;
- signalling, bloques ni interlocking;
- timetables ni dispatching;
- demanda de pasajeros ni logística de cargas;
- depósitos ni talleres;
- fallas ni escenarios estocásticos;
- túneles, puentes ni costos de suelo;
- Jev.

---

# 39. Etapas V0

Cada etapa `V0.x` corresponde a un release `v0.x.0` (§26.1).

## M0 (completadas)

- V0.1 — Workspace ([RW-000](docs/specs/completed/RW-000-bootstrap.md)).
- V0.2 — Geographic viewer ([RW-001](docs/specs/completed/RW-001-geographic-viewer.md)); `v0.2.1` aplica el sistema visual ([RW-002](docs/specs/completed/RW-002-web-visual-migration.md)).
- V0.3 — Existing railway data ([RW-003](docs/specs/completed/RW-003-existing-railway-data.md)).
- V0.4 — Terrain ([RW-004](docs/specs/completed/RW-004-terrain-and-elevation.md)).
- V0.5 — Candidate corridor prototype ([RW-005](docs/specs/completed/RW-005-candidate-corridor.md)).

## M1 (en curso)

Orden orientativo; cada etapa empieza con una sesión de investigación.

- **V0.6 — Railway graph ([RW-006](docs/specs/completed/RW-006-railway-graph.md), completada).** Topología de la red existente a partir de los tramos OSM: nodos, conexiones y continuidad por trocha. Camino entre estaciones por la red real y su perfil. Huecos de datos visibles.
- **V0.7 — Curvature (RW-007).** Radio mínimo y trazado del corredor en rectas y arcos (fidelidad de terreno V2).
- **V0.8 — Train and running time (RW-008).** Material rodante V1 y cálculo determinista del tiempo de recorrido, con límites de velocidad por curva.
- **V0.9 — Discrete-event simulation (RW-009).** Reloj, cola de eventos y una circulación con línea de tiempo reproducible ([ADR-006](docs/decisions/ADR-006-discrete-event-simulation.md)).
- **V0.10 — Build (RW-010).** Un corredor candidato pasa a infraestructura propuesta, unida a la red mediante un empalme. Cierra M1.

Decisiones tecnológicas previstas, a tomar con ADR solo si el requisito aparece: acceso a PostGIS desde .NET (RW-010) y framework de tests de frontend (RW-009).

---

# 40. Qué NO hacer todavía

No:

- planificar en detalle más allá de la spec siguiente;
- implementar interlocking ni estudiar ETCS completo;
- hacer modelos 3D detallados de trenes;
- crear economy/game mechanics;
- modelar pasajeros individuales;
- implementar freight logistics completas;
- diseñar microservices;
- agregar AI runtime;
- implementar C++;
- diseñar todo el schema final de database;
- crear bounded contexts vacíos.

Cada spec debe reducir incertidumbre y producir algo visible.

---

# 41. Objetivos visibles

El primer momento “RailWeaver existe” (M0, cumplido en `v0.5.0`) fue:

> **Abrir un mapa real de Córdoba, seleccionar dos puntos y generar/visualizar un corredor ferroviario básico que conozca la elevación del terreno.**

El de M1 será:

> **Diseñar un ramal, conectarlo a la red existente y ver a un tren recorrerlo, con un tiempo de viaje que se pueda explicar.**

No tiene que ser todavía una operación ferroviaria profesional.

Debe ser:

- reproducible;
- explicable;
- testeable;
- visual;
- construido sobre arquitectura que podamos evolucionar.

---

# 42. Desarrollo futuro esperado

Orden conceptual aproximado:

```text
Geography / terrain
        ↓
Existing infrastructure
        ↓
Route planning
        ↓
Railway graph
        ↓
Basic train movement
        ↓
Discrete-event operations
        ↓
Capacity
        ↓
Blocks
        ↓
Signals
        ↓
Junction routes
        ↓
Interlocking
        ↓
Dispatching
        ↓
Rolling stock fidelity
        ↓
Depots / workshops
        ↓
Stochastic scenarios
        ↓
Large network simulation
```

Este orden puede cambiar cuando aprendamos más del dominio.

---

# 43. Definition of Done para features de dominio

Una feature ferroviaria importante no está terminada únicamente cuando “funciona”.

Debe idealmente incluir:

```text
[ ] domain research
[ ] assumptions documented
[ ] domain model
[ ] tests
[ ] invariants where relevant
[ ] user-visible explanation
[ ] limitations documented
[ ] project status updated
```

---

# 44. Reglas para agentes

Antes de escribir código:

1. Leer `AGENTS.md`, la visión, la arquitectura, `docs/status/project-status.md` y la spec activa.
2. Identificar decisiones que ya están tomadas (ADRs) y NO reabrirlas sin motivo.
3. Identificar incógnitas técnicas reales y registrarlas en la spec o en `docs/research/`.
4. Trabajar a partir de una spec en `docs/specs/active/`.
5. No intentar avanzar múltiples milestones simultáneamente.

El agente debe preferir:

```text
small vertical slice
+
excellent boundaries
+
documented assumptions
```

sobre:

```text
large speculative architecture
```

## El kickoff es un documento vivo

Este archivo NO debe conservarse congelado como una fotografía del día cero.

Al finalizar cada sesión de trabajo sustancial, el agente debe revisar si este kickoff necesita actualización.

Actualizarlo solamente cuando haya cambiado información de nivel proyecto, por ejemplo:

- visión o alcance;
- stack;
- principios arquitectónicos;
- bounded contexts;
- estrategia de simulación;
- convenciones de desarrollo;
- política de versionado;
- workflows con agentes;
- decisiones que invaliden instrucciones anteriores.

No convertir este documento en un diario de desarrollo.

El progreso cotidiano pertenece principalmente a:

```text
docs/status/project-status.md
docs/specs/
docs/decisions/
CHANGELOG.md
```

El kickoff debe representar **cómo entendemos RailWeaver hoy**, no acumular históricamente todas las instrucciones que alguna vez fueron válidas.

Regla:

> **Si una sección transitoria dejó de ser cierta o útil, actualizarla, reemplazarla o eliminarla.**

Antes de cerrar una sesión sustancial, revisar:

```text
1. ¿Cambió algo que deba quedar en el kickoff?
2. ¿Alguna instrucción del kickoff quedó obsoleta?
3. ¿Hay una decisión que debería estar en un ADR?
4. ¿project-status.md refleja el estado real?
5. ¿CHANGELOG.md corresponde actualizarlo?
```

---

# 45. Principio final

RailWeaver es un proyecto de largo plazo.

Su éxito no depende de implementar rápidamente todas las funcionalidades imaginadas.

Depende de que podamos agregar complejidad ferroviaria durante años sin perder:

- comprensión;
- fidelidad;
- modularidad;
- reproducibilidad;
- observabilidad;
- capacidad de explicar el sistema.

La ambición es grande deliberadamente.

La implementación debe ser incremental deliberadamente.

---

# RailWeaver

**Plan. Build. Simulate. Understand.**

Initial world:

**Córdoba, Argentina**
