# RW-006 — La red como grafo

- Status: Completed
- Milestone / release objetivo: M1 — Primer tren → V0.6, release `v0.6.0`.

## Goal

Pasar de los 2.374 tramos OSM sueltos de RW-003 a una **red con topología**: nodos,
conexiones y continuidad por trocha. El usuario elige dos estaciones y RailWeaver
muestra el **camino por la red real**, con su longitud, los puntos donde el tren
tendría que invertir la marcha y el perfil del terreno bajo la vía. Los huecos de
los datos se muestran en una capa de diagnóstico en lugar de ocultarse. Nunca se
inventa una vía para conectar lo que OSM no conecta.

Es la base de M1: un tren necesita un camino por la red (RW-008) y un ramal nuevo
necesita un punto de la red donde empalmar (RW-010).

## Conceptos (en palabras simples)

- **Red de una trocha:** todas las vías de la misma trocha (1.000 mm o 1.676 mm)
  forman una red. Las redes de trochas distintas nunca se conectan: un tren no puede
  pasar de una a otra. Un tramo bitrocha pertenece a las dos redes (el extractor de
  RW-003 ya lo guarda como dos tramos con la misma geometría, uno por trocha).
- **Nodo:** un punto donde una vía empieza, termina o se une con otra. Dos tramos
  están unidos solo si comparten exactamente una coordenada. Si en OSM dos vías se
  cruzan sin compartir un punto (por ejemplo, un puente), no hay conexión.
- **Arista:** el pedazo de vía entre dos nodos consecutivos, sin uniones en el medio.
- **Pata:** cada una de las aristas que llegan a un nodo, vista desde el nodo.
- **Desviación:** cuánto cambia de dirección el tren al pasar por un nodo de una
  pata a otra. Seguir derecho = 0°. Volver por donde vino = 180°.
- **Regla del desvío:** el tren puede pasar de una pata a otra solo si la desviación
  es **≤ 45°**. Así, en un desvío real se puede ir del tronco a la vía directa o a la
  desviada (y volver), pero nunca de la desviada a la directa sin retroceder; y en
  un cruce en X no se puede doblar hacia la vía transversal.
- **Inversión de marcha:** cuando la regla no deja pasar de una pata a otra, pero
  existe una tercera pata que permite hacer la maniobra: el tren pasa el desvío por
  esa tercera pata, se detiene y retrocede hacia la pata que quería. Se cuenta y se
  marca en el mapa.
- **Punto de parada:** el lugar de una vía donde una estación "toca" la red. Una
  estación tiene un punto de parada en **cada** vía que pase a 150 m o menos (como
  los andenes de una estación real).

## Datos medidos antes de escribir la spec

Medidos con un prototipo descartable (Python, fuera del repo) sobre
`tracks.geojson` y `stations.geojson` del 2026-09-22. Justifican las decisiones y
sirven de referencia para la verificación (§7).

| Dato | Valor |
|---|---|
| Tramos sin trocha (`gaugeMillimetres = 0`) | 548 (339 km); 91 % son playas, desvíos industriales y apartaderos |
| Grupos de tramos sin trocha unidos a una sola red / a las dos / a ninguna | 504 tramos / 0 / 44 tramos (16 km) |
| Estaciones sin trocha declarada | 119 de 169 |
| Distancia estación–vía más cercana | todas ≤ 50 m, salvo 5 a más de 600 m (4 solo cerca de vías abandonadas) |
| Nodos donde se unen 3 vías (713) | 711 con las dos patas del mismo lado a < 30°, 1 entre 30° y 40° y 1 a 76° |
| Nodos donde se unen 4 vías | casi todos son dos desvíos juntos (patas a < 10°); ~5 son cruces en X (70°–90°) |
| Clasificación con 10 m o 30 m para medir la dirección de cada pata | idéntica |

## Scope

### 1. Investigación de dominio (resuelta)

[railway-topology-and-turnouts.md](../../research/infrastructure/railway-topology-and-turnouts.md).
La spec prevalece sobre la nota; las diferencias están en "Decisiones resueltas".

### 2. Core — `RailWeaver.Core.Infrastructure.Network` (nuevo namespace, sin E/S)

Carpeta `src/RailWeaver.Core/Infrastructure/Network/`. Todo el cálculo vive acá; la
API solo carga archivos y traduce HTTP.

Constantes públicas (clase estática `NetworkRules`):

| Constante | Valor | Qué es |
|---|---|---|
| `MaxDeflectionDegrees` | 45 | Desviación máxima para pasar de una pata a otra de corrido |
| `BearingLookaheadMeters` | 30 | Distancia a lo largo de la arista para medir la dirección de una pata |
| `StationSnapToleranceMeters` | 150 | Distancia máxima estación–vía para un punto de parada |
| `DataGapSearchRadiusMeters` | 500 | Radio para detectar un posible corte de datos |
| `BoundaryToleranceDegrees` | 1e-7 | Tolerancia para decidir que un extremo está en el borde del área de datos |

Geometría común:

- Longitud de una polilínea = suma de `ElevationProfileBuilder.GreatCircleDistanceMeters`
  entre vértices consecutivos.
- Punto a distancia `d` a lo largo de una polilínea: se recorre la polilínea y se
  interpola por gran círculo dentro del segmento que contiene `d`. La interpolación
  privada de `ElevationProfileBuilder` pasa a `Geography` como función pública
  reutilizable, sin cambiar su resultado.
- Rumbo: rumbo inicial de gran círculo en grados `[0, 360)`.
- Distancia de un punto `P` a una polilínea (estaciones y cortes de datos): plano
  local equirectangular centrado en `P`
  (`x = Δlon · cos(latP) · πR/180`, `y = Δlat · πR/180`, `R = 6.371.008,8 m`),
  proyección perpendicular sobre cada segmento con `t` recortado a `[0, 1]`, y el
  mínimo. La posición sobre la arista (`offset`) es la longitud de gran círculo
  hasta el inicio del segmento más `t` por la longitud de gran círculo del segmento.
  Con empate exacto de distancia se toma el menor `offset`.

#### 2.1 Trocha resuelta de cada tramo

Tipo `GaugeSource`: `Declared` (OSM declara la trocha), `InferredFromTags`
(RW-003 la dedujo de nombre u operador), `InferredFromConnection` (este paso) y
`Unknown`.

1. Tramo con trocha conocida: se conserva; `Declared` o `InferredFromTags` según
   su `GaugeInferred`.
2. Tramos sin trocha: se agrupan los que comparten al menos una coordenada
   (cualquier vértice, no solo los extremos), transitivamente.
3. Para cada grupo, se juntan las trochas de los tramos con trocha conocida que
   comparten alguna coordenada con algún tramo del grupo.
4. Si hay **exactamente una** trocha, todos los tramos del grupo la adoptan con
   `InferredFromConnection`. Si hay cero o dos, quedan `Unknown`.
5. Los tramos `Unknown` no entran a ninguna red y se reportan como diagnóstico.

Justificación: dos vías unidas por un desvío tienen la misma trocha. No se deduce
nada por cercanía, solo por unión.

#### 2.2 Construcción de cada red

Se construye una red por cada trocha conocida presente, ordenadas por ancho. Cada
red contiene **todos** los tramos de esa trocha, en cualquier estado (activa, en
desuso, abandonada): la topología describe los datos. El filtro por estado se
aplica al buscar caminos (§2.5).

1. Los tramos se ordenan por `Id` (ordinal) antes de todo: el resultado no depende
   del orden del archivo.
2. **Nodos:** una coordenada es nodo si es extremo de algún tramo de la red o si
   aparece más de una vez entre todos los tramos de la red (en tramos distintos o
   repetida dentro del mismo). La igualdad de coordenadas es exacta (sin tolerancia).
3. **Aristas:** cada tramo se corta en cada nodo interior. Cada pedazo es una arista
   con `Id = "{trackId}@{k}"` (`k` desde 1, en el sentido de la geometría), referencia
   al tramo, geometría, longitud, estado del tramo y `GaugeSource`. El sentido de
   referencia de la arista es el de la geometría: del nodo inicial al final.
4. **Patas:** cada extremo de cada arista es una pata del nodo donde termina. Su
   rumbo es el de gran círculo desde el nodo hasta el punto a
   `min(BearingLookaheadMeters, longitud)` a lo largo de la arista, alejándose del nodo.
   Una arista cerrada (mismo nodo en los dos extremos) aporta dos patas.
5. **Grado** de un nodo = cantidad de patas.
6. **Componentes:** grupos de aristas conectadas por nodos compartidos, sin mirar
   ángulos ni estados.
7. Índices deterministas: aristas ordenadas por `Id` (ordinal); nodos ordenados por
   `(latitud, longitud)`.

#### 2.3 Regla de paso por un nodo

Para dos patas `p` y `q` del mismo nodo: `desviación(p, q) = 180° − ángulo entre
sus rumbos` (ángulo en `[0°, 180°]`).

Un tren que llega al nodo por la pata `a` y sale por la pata `b` (`b ≠ a`):

1. **Paso directo** si `desviación(a, b) ≤ 45°`.
2. **Paso con inversión** si no es directo, pero existe una tercera pata `x`
   (`x ≠ a`, `x ≠ b`, disponible en la búsqueda) con `desviación(a, x) ≤ 45°` y
   `desviación(x, b) ≤ 45°`. Cuenta **una** inversión, ubicada en el nodo, y no suma
   distancia (ver Simplificaciones).
3. **Prohibido** en cualquier otro caso.

Salir por la misma pata por la que se llegó está siempre prohibido. No se invierte
la marcha en extremos de vía ni en medio de una arista.

#### 2.4 Estaciones y puntos de parada

Para una red y un conjunto de aristas disponibles, los puntos de parada de una
estación son, por cada arista disponible a `≤ 150 m` (inclusive), el punto más
cercano de esa arista (`offset` sobre la arista). Se ignora la trocha declarada de
la estación: la estación pertenece a las redes que tienen vías cerca.

#### 2.5 Búsqueda de ruta

Entrada `NetworkRouteRequest(RailwayStation Origin, RailwayStation Destination, bool IncludeDisused)`.
Validación (lanza `ArgumentException`): origen y destino con `Id` distinto.

Aristas disponibles: las de estado `Active`, más las `Disused` si `IncludeDisused`.
**Las `Abandoned` nunca** (ya no tienen rieles).

En cada red donde origen y destino tienen puntos de parada:

- Estado de búsqueda = (arista, sentido). Índice de estado = `índice de arista · 2 + sentido`
  (0 = sentido de referencia, 1 = contrario).
- Inicio: desde cada punto de parada de origen, dos estados (uno por sentido), con
  costo inicial = distancia desde el punto hasta el extremo de la arista hacia el
  que avanza.
- Llegada: al entrar en una arista que tiene un punto de parada de destino, se
  registra un candidato con la distancia hasta ese punto. Si origen y destino tienen
  puntos de parada en la misma arista, el recorrido directo sobre esa arista también
  es candidato (sin inversiones).
- Transiciones según §2.3, solo entre aristas disponibles.
- **Costo lexicográfico `(inversiones, metros)`**: primero la menor cantidad de
  inversiones y, a igualdad, la menor distancia. Dijkstra con cola ordenada por
  `(inversiones, metros, índice de estado)`.
- Termina cuando el menor elemento de la cola no puede mejorar al mejor candidato.

Entre redes, gana la de menor `(inversiones, metros)`; empate → menor ancho de
trocha. A igualdad total de candidatos dentro de una red, gana el de menor
`(índice de arista, sentido)` del destino.

Resultado `NetworkRouteResult`:

- `Status`: `Found` | `Unreachable`.
- Con `Found`, `NetworkRoute`:
  - `Gauge`;
  - `Legs`: `(EdgeId, TrackId, Direction, FromOffsetMeters, ToOffsetMeters)` en orden;
  - `Geometry`: la polilínea desde el punto de parada de origen hasta el de destino,
    sin vértices consecutivos repetidos;
  - `LengthMeters` y `StraightLineDistanceMeters` (gran círculo entre las dos estaciones);
  - `Reversals`: lista de `(GeoCoordinate Location, double DistanceAlongMeters)`;
  - `DistanceByStatus`: metros por `Active` y `Disused`;
  - `InferredGaugeDistanceMeters`: metros sobre aristas `InferredFromConnection`;
  - `Segments`: tramos recorridos en orden, con tramos consecutivos del mismo
    `TrackId` fusionados: `(TrackId, Name, LineReference, Status, LengthMeters)`;
  - `OriginStop` y `DestinationStop`: `(EdgeId, TrackId, OffsetMeters, Location)`.
- Con `Unreachable`, `Reason`, evaluado en este orden:
  1. `OriginWithoutTrack`: el origen no tiene puntos de parada en ninguna red.
  2. `DestinationWithoutTrack`: ídem para el destino.
  3. `NoCommonGauge`: no hay ninguna red donde ambos tengan puntos de parada.
  4. `Disconnected`: en todas las redes comunes, ningún punto de parada de origen
     está en la misma componente que alguno de destino (componentes calculadas solo
     con las aristas disponibles).
  5. `NoFeasibleMovement`: están en la misma componente, pero la regla de paso
     impide llegar.
  Además: `OriginGauges` y `DestinationGauges` (trochas de las redes donde cada
  estación tiene puntos de parada) y `ReachableWithDisused`: si `IncludeDisused` es
  falso, se repite la búsqueda con `true` y se informa si encontraría ruta; si ya es
  verdadero, `null`.

#### 2.6 Diagnóstico de red

Se calcula una vez, sobre cada red completa (todos los estados). Tipo
`NetworkDiagnostic(Kind, int? GaugeMillimetres, GeoCoordinate Location, string? TrackId,
string? StationId, double? DistanceMeters, double? DeflectionDegrees)`.

| `Kind` | Cuándo | Datos |
|---|---|---|
| `RegionBoundary` | Nodo de grado 1 a ≤ `1e-7°` de un borde del rectángulo de la región (el extractor recorta ahí) | trocha, tramo |
| `PossibleDataGap` | Nodo de grado 1 que no es `RegionBoundary` y tiene una arista de **otra componente** de la misma red a ≤ 500 m | trocha, tramo, distancia a esa arista |
| `EndOfTrack` | Cualquier otro nodo de grado 1 | trocha, tramo |
| `SharpJoint` | Nodo de grado 2 con `desviación > 45°`: dos tramos unidos en ángulo cerrado, sin desvío para invertir | trocha, tramo (el de menor `Id`), desviación |
| `StationWithoutTrack` | Estación sin ninguna arista de ninguna red a ≤ 150 m | estación, distancia a la vía más cercana de cualquier red |
| `TrackWithoutGauge` | Tramo `Unknown` tras §2.1 | tramo; ubicación = primer vértice |

Orden de la lista: por `Kind` (orden de la tabla), luego trocha, luego latitud y longitud.

Resumen por red (`NetworkSummary`): trocha, nodos, aristas, longitud total,
componentes y longitud de la componente mayor.

#### 2.7 Punto de entrada y tipos

- `RailwayNetworkBuilder.Build(IReadOnlyList<TrackSegment> tracks, IReadOnlyList<RailwayStation> stations, GeoBoundingBox regionBounds)`
  → `RailwayTopology`.
- `RailwayTopology`: `Networks` (una `RailwayNetwork` por trocha), `GaugeResolutions`
  (por tramo: trocha resuelta y `GaugeSource`), `Summaries` y `Diagnostics`.
  Es inmutable y segura para leer desde varios hilos.
- `RailwayNetwork`: `Gauge`, `Nodes` (`NetworkNode`: coordenada y patas), `Edges`
  (`NetworkEdge`, §2.2) y componente de cada arista.
- `NetworkRouteFinder(RailwayTopology topology)` con
  `NetworkRouteResult Find(NetworkRouteRequest request, CancellationToken cancellationToken)`.
  Clases concretas, sin interfaces.
- `TrackSegment` y `RailwayStation` no cambian.

#### 2.8 Invariantes (verificadas por tests)

1. **Trocha:** una ruta usa aristas de una sola red. Ninguna transición conecta
   redes distintas.
2. **Paso por nodos:** toda transición de una ruta es directa (≤ 45°) o una
   inversión contada y ubicada según §2.3. Nunca un giro de más de 45° de corrido.
3. **Cruces en X:** en un cruce a 90° no hay transición hacia la vía transversal.
4. **Estados:** ninguna ruta usa aristas `Abandoned`; usa `Disused` solo con
   `IncludeDisused`.
5. **No se inventa infraestructura:** ninguna arista existe sin un tramo de OSM
   detrás; nunca se unen vías que no comparten una coordenada.
6. **Determinismo:** misma entrada, en cualquier orden, produce la misma topología,
   los mismos diagnósticos y la misma ruta, en la misma plataforma.
7. El core no hace E/S (`CoreBoundaryTests`).

### 3. API

#### 3.1 Carga

- `RailwayFileStore` agrega un método que devuelve el dataset como objetos de dominio
  (`TrackSegment`, `RailwayStation`) con la misma validación de hoy. El contrato de
  `GET /api/regions/{id}/railway` no cambia.
- Nuevo `RailwayNetworkStore` (singleton, carpeta `RailWeaver.Api/Network/`): construye
  la `RailwayTopology` de una región la primera vez que se pide y la guarda en memoria
  (una construcción por región aunque lleguen pedidos simultáneos). Los bounds salen
  de `data/regions/{id}.json`. Cambiar los archivos requiere reiniciar la API.
- Contratos en `RailWeaver.Api/Network/NetworkContracts.cs`. Los endpoints solo
  traducen HTTP ↔ core.
- Redondeo solo en la respuesta: metros a 0,01 y grados a 0,1.

#### 3.2 `GET /api/regions/{id}/network`

- 404 si la región no existe. 200:

```json
{
  "networks": [
    {
      "gauge": { "widthMillimetres": 1000, "kind": "Metre" },
      "nodes": 0, "edges": 0, "lengthMeters": 0,
      "components": 0, "largestComponentLengthMeters": 0
    }
  ],
  "inferredGauges": [{ "trackId": "way/1", "widthMillimetres": 1676 }],
  "diagnostics": [
    {
      "kind": "possible_data_gap",
      "gaugeMillimetres": 1676,
      "location": { "latitude": 0, "longitude": 0 },
      "trackId": "way/1",
      "stationId": null,
      "distanceMeters": 42.1,
      "deflectionDegrees": null
    }
  ]
}
```

`inferredGauges` lista solo los tramos `InferredFromConnection`. `kind` en
snake_case: `region_boundary`, `possible_data_gap`, `end_of_track`, `sharp_joint`,
`station_without_track`, `track_without_gauge`.

#### 3.3 `POST /api/regions/{id}/network/routes`

Cuerpo:

```json
{ "originStationId": "node/1", "destinationStationId": "node/2", "includeDisused": false }
```

- 404 si la región no existe.
- 400 con `message` si falta un campo, si un id no es una estación de la región o
  si origen y destino son la misma estación.
- 200 en los dos estados. "No hay ruta" es una respuesta válida, no un error.

Con `found`:

```json
{
  "status": "found",
  "includeDisused": false,
  "route": {
    "gauge": { "widthMillimetres": 1676, "kind": "Broad" },
    "geometry": [{ "latitude": 0, "longitude": 0 }],
    "lengthMeters": 0,
    "straightLineDistanceMeters": 0,
    "reversals": [{ "location": { "latitude": 0, "longitude": 0 }, "distanceAlongMeters": 0 }],
    "distanceByStatus": { "active": 0, "disused": 0 },
    "inferredGaugeDistanceMeters": 0,
    "segments": [{ "trackId": "way/1", "name": null, "lineReference": null, "status": "Active", "lengthMeters": 0 }],
    "originStop": { "trackId": "way/1", "location": { "latitude": 0, "longitude": 0 } },
    "destinationStop": { "trackId": "way/2", "location": { "latitude": 0, "longitude": 0 } }
  },
  "profile": null,
  "profileUnavailableReason": "elevation_unavailable"
}
```

- `profile`: el formato `ProfileResponse` de RW-004, construido con
  `ElevationProfileBuilder` sobre `route.geometry` (muestras cada 30 m).
- `profileUnavailableReason`: `null` si hay perfil; `elevation_unavailable` si
  falta el DEM (la ruta se devuelve igual, sin 503); `too_many_samples` si la
  estimación supera 20.000 muestras (el mismo tope de RW-004).

Con `unreachable`:

```json
{
  "status": "unreachable",
  "includeDisused": false,
  "reason": "origin_without_track | destination_without_track | no_common_gauge | disconnected | no_feasible_movement",
  "originGauges": [1000],
  "destinationGauges": [1676],
  "reachableWithDisused": true
}
```

- Se pasa `HttpContext.RequestAborted` al core.

### 4. Frontend (según `DESIGN.md`, textos en español)

Módulo nuevo `src/web/src/network/` (alineado con `RailWeaver.Api/Network`, ADR-010):
cliente de API, herramienta, capa de ruta, capa de diagnóstico y detalle.

#### 4.1 Herramienta "Ruta"

- Botón "Ruta" en Herramientas, junto a "Perfil" y "Corredor". Las tres son
  excluyentes: activar una cancela las otras.
- No requiere el relieve. Si `GET /network` falla, el botón queda deshabilitado con
  el detalle "Requiere la red ferroviaria".

#### 4.2 Panel de tarea "Ruta por la red"

A la izquierda del mapa, como el de Corredor (máximo un tercio del ancho). En orden:

1. **Origen** y **Destino.** Cada uno es un campo de texto con lista de sugerencias:
   filtra estaciones cuyo nombre contiene lo escrito, sin distinguir mayúsculas ni
   acentos; muestra hasta 8, en orden alfabético (`es-AR`), con su tipo (Estación /
   Apeadero). Elegir una sugerencia fija la estación.
2. Casilla **"Incluir vías en desuso"**, desmarcada por defecto. Nota debajo: "Las
   vías abandonadas nunca se usan: ya no tienen rieles."
3. Botón primario **"Buscar ruta"**, habilitado con origen y destino fijados y
   distintos. Botón secundario **"Cancelar"**.

Si origen y destino son la misma estación: "Origen y destino deben ser estaciones
distintas", junto al campo Destino.

#### 4.3 Interacción en el mapa

- Con la herramienta activa, un clic sobre una estación fija el Origen si está
  vacío; si no, el Destino si está vacío; si ambos están fijados, fija un Origen
  nuevo y borra el Destino (como en RW-005).
- Un clic fuera de una estación no hace nada. El panel indica: "Elegí una estación
  en el mapa o escribí su nombre".
- Esc cancela la herramienta y borra la selección.

#### 4.4 Mientras busca

"Buscar ruta" queda deshabilitado y el panel muestra "Buscando ruta…". "Cancelar"
aborta el pedido (`AbortController`).

#### 4.5 Resultado `found`

- El panel de tarea se cierra.
- La ruta se dibuja sólida en `ink`, más ancha que las vías existentes, con casing
  `map-ground` y `clampToGround`. Hay una ruta a la vez: la nueva reemplaza a la
  anterior. Origen y destino llevan rótulos "Origen" y "Destino".
- Cada inversión se marca con el triángulo `warning` y el rótulo "Invierte la marcha".
- **Panel de detalle:**
  - Identificador stencil `R-NN` (contador de sesión desde `R-01`, no se persiste).
  - Tipo "Ruta por la red existente". Procedencia: "Trocha 1.676 mm · solo vías
    activas" o "· vías activas y en desuso".
  - Métricas: longitud, distancia directa, inversiones de marcha, por vías activas,
    por vías en desuso (solo si se incluyeron), con trocha deducida, y "Líneas
    recorridas": nombres o referencias distintos en orden de paso (los tramos sin
    nombre ni referencia no se listan).
  - Si hay inversiones, la métrica va en `warning` con su triángulo y se agrega una
    alerta `warning`:
    - **Título:** "La ruta exige invertir la marcha N vez/veces".
    - **Por qué:** "En estos puntos las vías forman un ángulo que el tren no puede
      tomar de corrido" + coordenadas en la cara de datos.
    - **Consecuencia:** "El tren debe pasar el desvío, detenerse y retroceder. La
      longitud no incluye esa maniobra."
  - Nota: "Camino sobre la geometría de OpenStreetMap. Sin velocidades ni tiempos
    de viaje."
  - Acción secundaria "Quitar ruta".
- **Dock de análisis:** perfil del terreno a lo largo de la ruta (terreno como área
  `border-subtle`, igual que RW-004), con título "Terreno bajo la ruta" y la nota
  "Cota del terreno, no de la vía: puentes y cortes aparecen como saltos". Cada
  inversión es una línea vertical rotulada. Sin perfil: "Perfil no disponible: falta
  el dataset de relieve" o "Perfil no disponible: la ruta es demasiado larga".
- Clic sobre la ruta → vuelve a mostrar su detalle.

#### 4.6 Resultado `unreachable`

Alerta `alarm` (cuadrado) en el panel de tarea, que queda abierto para ajustar:

| Motivo | Título | Por qué | Qué probar |
|---|---|---|---|
| `origin_without_track` / `destination_without_track` | "{Estación} no tiene vías utilizables a menos de 150 m" | Solo desuso desactivado: "Las vías cercanas pueden estar en desuso o abandonadas." Si no: "No hay vías cerca en los datos." | Otra estación |
| `no_common_gauge` | "{Origen} y {Destino} están en redes de trocha distinta" | "{Origen}: trocha X mm. {Destino}: trocha Y mm. Un tren no puede pasar de una trocha a otra." | Estaciones de la misma red |
| `disconnected` | "No hay continuidad de vía entre {Origen} y {Destino}" | "La red está cortada entre ambas. Puede ser un corte real o un hueco de los datos de OSM." | Ver la capa "Diagnóstico de red" |
| `no_feasible_movement` | "Las vías se conectan, pero un tren no puede recorrerlas" | "En el camino hay uniones con ángulos mayores a 45° sin desvío para invertir la marcha." | Ver "Unión en ángulo cerrado" en "Diagnóstico de red" |

Si `reachableWithDisused` es verdadero, la alerta agrega un chip de acción
(diamante) "Hay ruta si se incluyen vías en desuso"; al pulsarlo se marca la
casilla y se repite la búsqueda.

Errores: 400 muestra el `message`; error de red o 5xx muestra una alerta con el
mensaje de la API.

#### 4.7 Capa "Diagnóstico de red"

- Nueva entrada en la lista de capas, **apagada por defecto**. Detalle: "{n} posibles
  cortes" (cantidad de `possible_data_gap`).
- Marcas (color + forma + palabras):
  - Posible corte de datos, Unión en ángulo cerrado y Estación sin vía: triángulo `warning`.
  - Fin de vía y Borde del área de datos: círculo con contorno `ink-subtle`.
  - Vía sin trocha: el tramo se redibuja en `warning` con el triángulo junto a su
    primer vértice.
- Clic en una marca → panel de detalle "Diagnóstico de red" con el nombre del tipo,
  la trocha, las coordenadas y este texto:

| Tipo | Texto |
|---|---|
| Posible corte de datos | "La vía termina a {d} m de otra parte de la red de trocha {X} mm, sin unirse. Puede ser un corte real o un error de mapeo en OSM." |
| Fin de vía | "La vía termina acá. No hay otra parte de la red a menos de 500 m." |
| Borde del área de datos | "La vía sigue fuera del área de datos de Córdoba: el dataset se recorta en ese límite." |
| Unión en ángulo cerrado | "Dos tramos se unen con una desviación de {x}°, mayor que 45°. Un tren no puede pasar de uno al otro de corrido y no hay desvío para invertir la marcha." |
| Estación sin vía | "No hay vías a menos de 150 m. La más cercana está a {d} m." |
| Vía sin trocha | "OSM no indica la trocha y la vía no está unida a ninguna red, así que no se puede deducir." |

#### 4.8 Detalle de tramo existente

La fila "Trocha" del panel de tramo de RW-003 agrega el origen del dato:
"1.676 mm · deducida por conexión" (`inferredGauges`) o "1.676 mm · deducida por
nombre u operador" (`gaugeInferred` de RW-003). Un tramo en `inferredGauges` deja de
mostrarse como "Sin dato".

## Non-goals

- Estado de las agujas, enclavamientos y señales (RW-009 y posteriores).
- Velocidades, tiempos de viaje, material rodante y largo del tren (RW-008).
- Radios de curva y velocidad en desvíos (RW-007).
- Rutas entre puntos libres del mapa, puntos intermedios, alternativas o comparación de rutas.
- Editar, corregir o completar la red; unir huecos automáticamente.
- Volver a extraer OSM con etiquetas de nodos (`railway=switch`, `railway_crossing`,
  `buffer_stop`). Por eso no se distingue una topera real de otro fin de vía.
- Pendiente de la vía existente (rasante). El perfil es del terreno.
- Persistencia (archivos de grafo, PostGIS). El grafo vive en memoria.
- Tests de frontend.

## Simplificaciones

- La inversión de marcha no suma la distancia que el tren recorre para pasar el
  desvío, ni verifica que la tercera pata sea tan larga como el tren. RW-008 la
  revisará con el largo del tren.
- La dirección de cada pata se mide a 30 m del nodo. Es una aproximación del ángulo
  del aparato de vía; la geometría fina queda para RW-007.
- Un cruce en X con un ángulo menor a 45° no se distingue de dos desvíos en el mismo
  punto y se trata como conectado. En los datos hay como mucho dos nodos en esa
  situación (los de 4 patas con patas del mismo lado entre 20° y 40°).
- La unión se decide por coordenada exacta porque `tracks.geojson` no guarda ids de
  nodo OSM. En el extracto actual ningún par de nodos distintos de las vías comparte
  coordenadas, así que el resultado es el mismo que usar los ids.
- La trocha de las estaciones no se usa: 119 de 169 no la tienen.

## Inputs

- `data/regions/cordoba/tracks.geojson`, `stations.geojson` y `data/regions/cordoba.json` (RW-003).
- DEM de RW-004 (opcional; solo para el perfil).
- Origen, destino y la opción de vías en desuso elegidos por el usuario.

## Outputs

- `RailWeaver.Core.Infrastructure.Network`: `NetworkRules`, `GaugeSource`,
  `RailwayNetworkBuilder`, `RailwayTopology`, `RailwayNetwork`, `NetworkNode`,
  `NetworkEdge`, `NetworkSummary`, `NetworkDiagnostic`, `NetworkRouteFinder`,
  `NetworkRouteRequest`, `NetworkRouteResult` y `NetworkRoute`.
- Endpoints `GET /api/regions/{id}/network` y `POST /api/regions/{id}/network/routes`.
- Herramienta "Ruta", capa "Diagnóstico de red" y trocha deducida en el detalle de tramo.
- Docs: `project-status.md`, `architecture/overview.md` (namespace Network), `CHANGELOG.md`,
  README (uso de la herramienta), kickoff revisado, y nota en la investigación
  enlazando a esta spec.

## Acceptance criteria

- [x] `RailwayNetworkBuilder` implementa §2.1–2.2 y §2.6 tal como están escritos.
- [x] `NetworkRouteFinder` implementa §2.3–2.5: puntos de parada múltiples, filtro por
      estado, costo `(inversiones, metros)`, desempates y motivos en su orden.
- [x] Tests sintéticos del core (§Tests) en verde, incluidas las invariantes de §2.8.
- [x] Con el dataset real de Córdoba (versionado, corre en CI), tests del core o de
      la API verifican estos casos. Distancias con tolerancia de ±1 %; la ubicación
      de cada inversión, a menos de 1 km de la indicada:

| Caso | Desuso | Resultado esperado (prototipo) |
|---|---|---|
| Córdoba → Villa María | no | `found`, 1.676 mm, 0 inversiones, ≈ 141,6 km |
| Villa María → Río Cuarto | no | `found`, 1.676 mm, 0 inversiones, ≈ 131,6 km |
| Córdoba → Río Cuarto | no | `found`, 1.676 mm, 1 inversión cerca de Villa María (−32,4092; −63,2478), ≈ 272,4 km |
| Alta Córdoba → Cosquín | no | `found`, 1.000 mm, 0 inversiones, ≈ 56,9 km |
| Córdoba → Alta Córdoba | no | `found`, 1.000 mm, 1 inversión en el empalme al norte de Alta Córdoba (−31,3878; −64,1919), ≈ 6,4 km |
| Córdoba → Cruz del Eje | no | `unreachable`, `disconnected`, `reachableWithDisused = true` |
| Córdoba → Cruz del Eje | sí | `found`, 1.000 mm, 0 inversiones, ≈ 153,0 km |
| Córdoba → Deán Funes | sí | `found`, 1.000 mm, 0 inversiones, ≈ 218,0 km |
| Cosquín → Villa María | no | `unreachable`, `no_common_gauge` |

- [x] Con el dataset real, el diagnóstico se registra en esta spec. Referencia del
      prototipo; una diferencia mayor a 2 unidades en un conteo se explica antes de
      cerrar:

| Dato | Métrica (1.000 mm) | Ancha (1.676 mm) |
|---|---|---|
| Aristas | 1.168 | 2.785 |
| Componentes | 11 | 28 |
| Extremos: borde / posible corte / fin de vía | 10 / 7 / 132 | 26 / 96 / 297 |
| Uniones en ángulo cerrado | 4 | 3 |

  Además: 504 tramos con trocha deducida por conexión, 44 tramos sin trocha y
  1 estación sin vía ("Estación del Monoriel").
- [x] Endpoints según §3: 200, 400 y 404, con `found` y cada motivo de
      `unreachable` cubiertos por tests de API; `profileUnavailableReason =
      elevation_unavailable` sin DEM.
- [x] Tiempo, medido en la máquina de desarrollo y registrado: construcción de la
      topología ≤ 1 s; cada caso de la tabla ≤ 1 s sin perfil y ≤ 5 s con perfil y DEM.
- [x] En el visor: herramienta "Ruta" con panel, sugerencias, clic en estaciones,
      búsqueda cancelable, ruta dibujada con inversiones, detalle `R-NN`, perfil en el
      dock, alertas por motivo con el chip de desuso, capa "Diagnóstico de red" con
      detalle por marca, y trocha deducida en el detalle de tramo. Todo con tokens de
      `DESIGN.md`. El autor confirma el funcionamiento; los agentes no hacen
      verificación visual y entregan una lista específica de puntos a revisar.
- [x] `CoreBoundaryTests` en verde. `dotnet test`, `npm --prefix src/web run lint`,
      `format:check` y `build` pasan localmente y en CI sin el DEM.
- [x] `project-status.md`, `architecture/overview.md`, `CHANGELOG.md` y README
      actualizados; kickoff revisado; tag `v0.6.0`.

### Resultado de implementación (2026-09-22)

Con el dataset versionado de Córdoba, las dos redes suman **1.168 aristas / 11
componentes** (métrica) y **2.785 aristas / 28 componentes** (ancha). El diagnóstico
coincide exactamente con la referencia de la tabla: métrica **10 / 7 / 132**
extremos y **4** uniones cerradas; ancha **26 / 96 / 297** extremos y **3** uniones
cerradas. Hay **504** tramos con trocha deducida por conexión, **44** sin trocha y
**1** estación sin vía. Los nueve recorridos de la tabla pasan con ±1 % en distancia;
las dos inversiones verificadas están a menos de 1 km de la posición de referencia.

Los tests de API verifican construcción en **≤ 1 s** desde el primer pedido y
las rutas con perfil en **≤ 5 s**. En esta máquina, el primer caso con DEM tardó
**1,84 s** incluyendo HTTP, construcción inicial y perfil. `dotnet test` ejecutó
**128 tests** sin errores; lint y build del frontend también pasaron. El autor
confirmó el funcionamiento de la interfaz el 2026-09-22 (sin verificación visual
de agentes).

El CI del commit `edb9956` en `main` terminó en verde.

## Relevant domain docs

- [railway-topology-and-turnouts.md](../../research/infrastructure/railway-topology-and-turnouts.md)
- [cordoba-railway-data.md](../../research/infrastructure/cordoba-railway-data.md) (extracción, bitrocha)
- Kickoff §38–39 (M1, V0.6).

## Relevant architecture

- [Overview](../../architecture/overview.md): el core construye la red y busca rutas;
  la API carga archivos, guarda la topología en memoria y traduce HTTP; el navegador
  solo dibuja.
- Network como namespace dentro de `RailWeaver.Core.Infrastructure`, sin proyecto nuevo.
- [ADR-006](../../decisions/ADR-006-discrete-event-simulation.md): determinismo.
- [ADR-010](../../decisions/ADR-010-frontend-structure-and-formatting.md): módulo `network` en el frontend.
- [DESIGN.md](../../../DESIGN.md) › Layout, Components (detalle, alertas, chip de acción),
  Cartography (líneas y marcas de desvío) y Charts (perfil).

## Tests

- **Core, con redes sintéticas en memoria** (coordenadas inventadas, metros conocidos):
  - Unión en T: el tramo pasante se corta en dos aristas; grados de nodo correctos.
  - Desvío: tronco → directa y tronco → desviada permitidos (y a la inversa);
    desviada → directa solo con inversión si hay tercera pata, prohibido sin ella.
  - Cruce en X a 90°: sin transición a la vía transversal.
  - Triángulo de vía: ruta sin inversión por el lado que corresponde.
  - Inversión: contada, ubicada en el nodo y preferida solo si no hay ruta sin inversión.
  - Trochas: métrica y ancha nunca se conectan; un tramo bitrocha está en ambas redes;
    inferencia con una trocha, con dos (queda `Unknown`) y sin ninguna.
  - Estados: nunca `Abandoned`; `Disused` solo con la opción; `reachableWithDisused`.
  - Estaciones: puntos de parada múltiples, tolerancia de 150 m inclusiva, estación
    en dos redes, estación sin vía.
  - Cada motivo de `unreachable`, en su orden de evaluación.
  - Cada tipo de diagnóstico.
  - Determinismo: los tramos en otro orden producen la misma topología, diagnósticos y ruta.
  - Origen = destino → `ArgumentException`.
- **Dataset real:** los casos y conteos de Acceptance criteria.
- **API:** los dos endpoints con el dataset real (sin DEM en CI).
- **Frontera del core:** `CoreBoundaryTests` sin cambios.

## Decisiones resueltas antes de implementar

1. **Tramos sin trocha heredan la de la red a la que están unidos** (decidido con el
   usuario). Recupera 504 de 548 tramos; los 44 restantes se muestran como "Vía sin
   trocha". Se marca como deducida.
2. **Vías activas por defecto, en desuso opcional, abandonadas nunca** (decidido con
   el usuario). Ejemplo: Córdoba → Cruz del Eje solo existe con vías en desuso.
3. **Inversiones permitidas, contadas y marcadas; se prefiere siempre la ruta con
   menos inversiones** (decidido con el usuario). Ejemplo: Córdoba → Río Cuarto
   invierte en Villa María.
4. **Huecos en una capa "Diagnóstico de red" apagada por defecto, más la
   explicación cuando una ruta falla** (decidido con el usuario).
5. **El grafo se construye en el core al cargar el dataset** (Unknown 1 de la nota,
   opción B): el GeoJSON sigue siendo la única fuente; construirlo tarda
   milisegundos. No se agrega un archivo de grafo versionado.
6. **Unión por coordenada exacta, no por id de nodo OSM:** `tracks.geojson` no
   guarda ids de nodo. Dos vías que se cruzan sin compartir un punto (puentes,
   pasos a distinto nivel) quedan separadas, que es lo correcto.
7. **Una red por trocha en lugar de aristas bitrocha:** el extractor ya separa las
   vías bitrocha en un tramo por trocha, así que cada red es independiente y no hace
   falta una regla de compatibilidad.
8. **Regla única de 45° en lugar de reglas especiales por tipo de nodo:** medido en
   Córdoba, separa limpio los desvíos (patas < 30°) de los cruces en X (70°–90°).
9. **Estación enganchada a todas sus vías cercanas, no solo a la más cercana:** con
   una sola vía, Córdoba → Alta Córdoba caía en una vía sin salida hacia el destino.
10. **Diferencias con la nota de investigación:**
    - no se clasifican "toperas legítimas" (no hay etiquetas de nodo);
    - "posible corte" = extremo a ≤ 500 m de **otra componente** de la misma red, no
      de cualquier vía activa (un apartadero siempre termina junto a su propia línea);
    - se agrega `SharpJoint`: uniones de dos tramos a más de 45°;
    - no hay `NearestReachablePoint`; lo reemplazan el motivo, la pista de vías en
      desuso y la capa de diagnóstico;
    - la inversión de marcha deja de ser siempre prohibida y pasa a ser contada;
    - namespace `Infrastructure.Network` en lugar de `Topology`, siguiendo los
      contextos del Kickoff;
    - Unknown 2 (acceso a Córdoba Mitre): en OSM, Córdoba → Cosquín tiene
      continuidad métrica sin inversión (≈ 60 km); Córdoba → Alta Córdoba exige
      una inversión.
11. **Determinismo por plataforma**, como en RW-005 (decisión 9).

## Open questions

Ninguna. Toda decisión nueva durante la implementación se registra acá antes de implementarla.
