# RW-005 — Candidate corridor prototype

- Status: Completed
- Milestone / release objetivo: V0.5 — Candidate corridor prototype → release `v0.5.0`. Cierra el vertical slice de M0 (mapa → vías → origen/destino → corredor → elevación).

## Goal

Que el usuario marque dos puntos en el mapa de Córdoba, elija una pendiente máxima y RailWeaver genere un **corredor candidato**: un trazado sobre el terreno real que nunca supera esa pendiente, con su perfil longitudinal y métricas básicas. Si no existe ningún trazado posible, el sistema lo dice claramente y no inventa uno. Es el primer momento "RailWeaver existe" ([Kickoff](../../../RailWeaver_Project_Kickoff.md) §41).

El algoritmo es simple a propósito. Importan más la corrección, el determinismo y poder explicar el resultado que la sofisticación (Kickoff §39).

## Conceptos (en palabras simples)

- **Malla de búsqueda:** una grilla de puntos separados entre 250 m y 1 km sobre el área entre el origen y el destino. El corredor es una cadena de puntos de esa malla.
- **Línea de vía:** entre dos puntos consecutivos, la vía sube o baja en línea recta. Su pendiente es el desnivel entre los dos puntos dividido por la distancia. **El límite de pendiente se aplica siempre a la línea de vía**, en ambos sentidos (rampa y pendiente).
- **Terreno:** el relieve real muestreado cada 30 m, como en RW-004. Puede tener subidas locales más fuertes que el límite (árboles, edificios, barrancas chicas). La diferencia entre terreno y línea de vía es el corte o relleno implícito que habría que hacer. Se informa, pero no se calcula como obra.

## Scope

### 1. Investigación de dominio (resuelta)

[railway-gradients-and-corridor-routing.md](../../research/terrain-routing/railway-gradients-and-corridor-routing.md). La spec prevalece sobre el boceto de código de la nota; las diferencias están en "Decisiones resueltas".

### 2. Core — `RailWeaver.Core.Planning` (nuevo namespace, sin E/S)

Tipos:

- `CorridorRequest(GeoCoordinate Origin, GeoCoordinate Destination, double MaxGradientPermille, GeoBoundingBox SearchLimit)`. `SearchLimit` es el área máxima donde se puede buscar; la API pasa la cobertura del DEM.
- `CorridorFinder(IElevationSource elevationSource)` con `CorridorResult Find(CorridorRequest request, CancellationToken cancellationToken)`. Clase concreta; no se crea interfaz.
- `CorridorResult`: `Status` (`Found` | `NoFeasiblePath` | `EndpointWithoutElevation`), `CandidateCorridor? Corridor` (solo con `Found`) y `CorridorSearchInfo Search`.
- `CorridorSearchInfo`: paso de malla en metros, columnas, filas, nodos explorados y área de búsqueda (`GeoBoundingBox`).
- `CandidateCorridor`: `Alignment` (lista de `GeoCoordinate`), `TrackProfile` (lista de `TrackProfilePoint`), `TerrainProfile` (`ElevationProfile` de RW-004) y `CorridorMetrics`.
- `TrackProfilePoint(double DistanceMeters, GeoCoordinate Coordinate, double ElevationMeters, double? GradientPermille)`. La pendiente es la del tramo que termina en ese punto, con signo en el sentido origen → destino; el primer punto no tiene.
- `CorridorMetrics`: ver §2.7.

Validación de entrada (lanza `ArgumentException`; la API la traduce a 400):

- `MaxGradientPermille` finito y entre **1 y 40 ‰** inclusive.
- Origen y destino dentro de `SearchLimit`.
- Distancia en línea recta entre origen y destino de **al menos 1 km**.

#### 2.1 Área de búsqueda

1. `D` = distancia de gran círculo entre origen y destino (`ElevationProfileBuilder.GreatCircleDistanceMeters`).
2. Margen `b = max(0,25 · D, 10.000 m)`.
3. Latitud de referencia `φc` = promedio de las latitudes de origen y destino. Metros por grado: `mLat = R · π / 180` (con `R` = 6.371.008,8 m) y `mLon = mLat · cos(φc)`.
4. Rectángulo = el que contiene origen y destino, expandido `b / mLat` grados al norte y al sur y `b / mLon` grados al este y al oeste.
5. Se recorta a `SearchLimit`.

#### 2.2 Malla

- Paso `s`: el **menor** de {250, 500, 1.000} m tal que `columnas × filas ≤ 1.000.000`. Con la cobertura de Córdoba, 1.000 m siempre entra.
- `Δlat = s / mLat` y `Δlon = s / mLon`. El nodo (fila `r`, columna `c`) está en `(south + r·Δlat, west + c·Δlon)`, con `columnas = ⌊ancho° / Δlon⌋ + 1` y `filas = ⌊alto° / Δlat⌋ + 1`. Índice del nodo: `r · columnas + c`.
- Cota de cada nodo: `IElevationSource` (interpolación bilineal de RW-004). **Un nodo sin dato es intransitable.** Nunca se reemplaza por 0.
- Conectividad de **16 vecinos**: los 8 adyacentes más los 8 saltos de caballo `(±1, ±2)` y `(±2, ±1)`. Reduce el zigzag de 45°/90° sin suavizado posterior.
- **Origen y destino** son dos nodos extra, con índices `N` y `N + 1` (`N` = total de nodos de malla). Cada uno se conecta con las 4 esquinas de la celda de malla que lo contiene. Si está a menos de 1 m de una esquina, esa esquina se usa directamente como el punto (desplazamiento ≤ 1 m, despreciable).
- Si el origen o el destino no tienen cota → `EndpointWithoutElevation`, sin buscar.

#### 2.3 Aristas y restricción dura

Para una arista `u → v` con distancia de gran círculo `d` y cotas `H`:

- `pendiente = |H(v) − H(u)| / d · 1000` (‰).
- Si `pendiente > MaxGradientPermille`, la arista **no existe**. Se compara el valor sin redondear y sin tolerancia.
- Costo: `d · (1 + β · (pendiente / MaxGradientPermille)²)`, con **β = 1** fijo (constante pública `CorridorFinder.GradientPenalty`). Así, un tramo al límite cuesta el doble que uno llano: el trazado prefiere valles y rodeos razonables, pero no se desvía sin motivo.

#### 2.4 Búsqueda

- **A\*** con heurística = distancia de gran círculo al destino. Es admisible y consistente porque el costo nunca es menor que la distancia.
- Cola de prioridad ordenada por `(f, índice de nodo)` ascendente. El desempate por índice hace que el resultado no dependa del orden interno de la cola.
- Termina cuando se extrae el destino (`Found`) o cuando la cola queda vacía (`NoFeasiblePath`).
- Revisa el `CancellationToken` al menos cada 10.000 nodos extraídos.
- "Nodos explorados" = cantidad de nodos extraídos de la cola.

#### 2.5 Trazado

- `Alignment` = origen, nodos del camino y destino, en orden y **sin simplificar**. Unir vértices alineados cambiaría la línea de vía y podría romper el límite.
- `TrackProfile`: un punto por vértice, con distancia acumulada de gran círculo, cota del nodo (la vía pasa a nivel del terreno en cada vértice) y pendiente con signo del tramo.

#### 2.6 Perfil de terreno

- `TerrainProfile` = `ElevationProfileBuilder.Build(Alignment)`: muestras cada 30 m, vértices incluidos. Si aparecen muestras sin dato entre vértices, quedan como hueco, igual que en RW-004.
- La cota de la línea de vía en cada muestra se obtiene interpolando linealmente por distancia entre los vértices del `TrackProfile`.

#### 2.7 Métricas (`CorridorMetrics`)

| Métrica | Definición |
|---|---|
| `LengthMeters` | Suma de los tramos del trazado |
| `StraightLineDistanceMeters` | `D` |
| `Sinuosity` | `LengthMeters / StraightLineDistanceMeters` |
| `MaxGradientPermille` | Mayor pendiente absoluta de la línea de vía |
| `MaxGradientLimitPermille` | El límite pedido (para mostrarlos juntos) |
| `AscentMeters` / `DescentMeters` | Subida y bajada acumuladas de la línea de vía, en sentido origen → destino |
| `MinElevationMeters` / `MaxElevationMeters` | Cota mínima y máxima de la línea de vía |
| `DistanceByGradientBand` | Metros de vía en cada cuarto del límite: `[0 %, 25 %]`, `(25 %, 50 %]`, `(50 %, 75 %]` y `(75 %, 100 %]` del límite, según la pendiente absoluta de cada tramo. La suma es igual a `LengthMeters` |
| `MaxCutMeters` | Mayor `terreno − vía` sobre las muestras con dato (corte implícito, ≥ 0) |
| `MaxFillMeters` | Mayor `vía − terreno` sobre las muestras con dato (relleno implícito, ≥ 0) |

#### 2.8 Invariantes (verificadas por tests)

1. Ningún tramo de la línea de vía supera `MaxGradientPermille`.
2. Si no hay camino, el resultado es `NoFeasiblePath` y no trae corredor. Nunca se relaja el límite ni se devuelve un trazado parcial.
3. La misma entrada con el mismo `IElevationSource` produce el mismo resultado, bit a bit, en la misma plataforma.
4. El core no hace E/S ni referencia frameworks (`CoreBoundaryTests`).

### 3. API — `POST /api/regions/{id}/corridors`

Cuerpo:

```json
{
  "origin": { "latitude": -31.4167, "longitude": -64.1833 },
  "destination": { "latitude": -30.7261, "longitude": -64.8047 },
  "maxGradientPermille": 15
}
```

Respuestas:

- **404** si la región no existe.
- **503** si falta el dataset de elevación, con el mismo mensaje de RW-004.
- **400** con `message` si:
  - falta un campo o una coordenada es inválida;
  - la pendiente está fuera de 1–40 ‰;
  - origen o destino están fuera de la bounding box de la región (`cordoba.json`);
  - están a menos de 1 km entre sí.
- **200** en los tres estados de dominio. "No hay camino" es una respuesta válida, no un error:

```json
{
  "status": "found | no_feasible_path | endpoint_without_elevation",
  "search": {
    "gridStepMeters": 250, "gridColumns": 412, "gridRows": 380, "exploredNodes": 51234,
    "bounds": { "west": 0, "south": 0, "east": 0, "north": 0 }
  },
  "corridor": null
}
```

Con `found`, `corridor` contiene:

- `alignment`: `[{ latitude, longitude }]`;
- `trackProfile`: `[{ distanceMeters, latitude, longitude, elevationMeters, gradientPermille }]`;
- `terrainProfile`: el mismo formato de `ProfileResponse` de RW-004;
- `metrics`: las métricas de §2.7 en camelCase. `distanceByGradientBand` es una lista de `{ fromPercentOfLimit, toPercentOfLimit, meters }`.

Otras reglas:

- `SearchLimit` = bounds de la grilla DEM cargada (`ElevationGridFile.West/South/East/North`).
- Redondeo solo en la respuesta: metros a 0,01, ‰ a 0,1 y sinuosidad a 0,001. El core trabaja sin redondear.
- Se pasa `HttpContext.RequestAborted` al core: si el navegador cancela, la búsqueda se detiene.
- Contratos en `RailWeaver.Api/Planning/CorridorContracts.cs`. El endpoint solo traduce HTTP ↔ core; no decide lógica ferroviaria.

### 4. Frontend (según `DESIGN.md`, textos en español)

Código nuevo en módulos propios (`corridors.ts` para el cliente de API y `CorridorTool.tsx` o equivalente para la UI), para no seguir agrandando `GeographicViewer.tsx`.

#### 4.1 Herramienta "Corredor"

- Botón "Corredor" en la sección Herramientas del riel lateral, junto a "Perfil".
- Las dos herramientas son excluyentes: activar una cancela la otra.
- Si el relieve no está disponible, el botón queda deshabilitado con el detalle "Requiere el dataset de relieve".

#### 4.2 Panel de tarea temporal "Corredor candidato"

Se abre al activar la herramienta, a la izquierda del mapa junto al riel, y ocupa como máximo un tercio del ancho (DESIGN.md › Layout). Contiene, en este orden:

1. **Pendiente máxima.** Cuatro botones de referencia (`button-secondary` con `aria-pressed`):
   - Carga 10 ‰
   - Mixta 15 ‰ (seleccionado por defecto)
   - Pasajeros 25 ‰
   - Montaña 35 ‰

   Debajo, un campo numérico en ‰ (1–40, paso 0,1) que refleja el botón elegido y acepta cualquier valor. Si el valor no coincide con un botón, ninguno queda marcado. Debajo, la nota: "Valores de referencia, a validar."
2. **Origen** y **Destino**: filas con estado ("Marcá un punto en el mapa" o coordenadas en la cara de datos).
3. Botón primario **"Generar corredor"**, habilitado solo con origen, destino y una pendiente válida. Botón secundario **"Cancelar"**.

Validaciones en el panel, antes de llamar a la API: pendiente fuera de rango, puntos fuera de la región y puntos a menos de 1 km. Cada una muestra un mensaje específico junto al campo.

#### 4.3 Interacción en el mapa

- Con la herramienta activa, el primer clic fija el origen y el segundo el destino.
- Un tercer clic empieza de nuevo: fija un origen nuevo y borra el destino.
- Esc cancela la herramienta y borra los puntos.
- Origen y destino se marcan en `primary`, con rótulos "Origen" y "Destino" en la fuente de UI.

#### 4.4 Mientras calcula

- "Generar corredor" queda deshabilitado y el panel muestra "Calculando corredor…".
- "Cancelar" aborta el pedido (`AbortController`).

#### 4.5 Resultado `found`

- El panel de tarea se cierra.
- El corredor se dibuja en `primary`, sólido, con casing `map-ground` y `clampToGround` (DESIGN.md › Cartography). Reemplaza al corredor anterior, porque hay uno solo a la vez.
- **Panel de detalle:**
  - Identificador en stencil `C-NN`: número de sesión que empieza en `C-01` y sube con cada corredor generado. No se persiste.
  - Tipo "Corredor candidato" y procedencia: "Límite 15,0 ‰ · malla 250 m".
  - Lista de métricas: longitud, distancia directa, sinuosidad, pendiente máxima junto al límite (`14,8 ‰ ≤ 15,0 ‰`), subida, bajada, cota mínima y máxima, corte y relleno implícitos máximos, metros por banda de pendiente y nodos explorados.
  - Nota de supuestos: "Sin túneles, puentes, curvas ni costos de suelo. Límite de referencia, a validar."
  - Acción secundaria "Quitar corredor".
- **Dock de análisis:**
  - perfil longitudinal SVG con el terreno como área `border-subtle` y la línea de vía en `primary` (DESIGN.md › Charts);
  - la anotación `máx. 14,8 ‰ ≤ límite 15,0 ‰` en la cara de datos;
  - huecos sin dato rotulados, como en RW-004.
  - No hay bandas `warning`: por construcción, la vía nunca excede el límite.
- Clic sobre la línea del corredor → vuelve a mostrar su detalle. Clic en otro lugar → inspección normal de RW-004.

#### 4.6 Resultado `no_feasible_path`

Alerta `alarm` (cuadrado) en el panel de tarea, que queda abierto para ajustar:

- **Título:** "No existe un corredor con pendiente ≤ X ‰ entre estos puntos".
- **Por qué:** el relieve exige pendientes mayores en toda el área de búsqueda. Esta versión no usa túneles ni puentes.
- **Qué probar:** subir el límite o mover los puntos.

Se muestran además el paso de malla y los nodos explorados.

#### 4.7 Otros estados

- `endpoint_without_elevation`: alerta `alarm` que dice que el punto no tiene dato de relieve y hay que moverlo.
- 400: se muestra el `message` de la API.
- Error de red o 503: alerta con el comando de descarga, como en RW-004.

## Non-goals

- Túneles, puentes, cortes y terraplenes como obras (solo se informa el corte y relleno implícito).
- Curvas: radio mínimo, clotoides y peralte; compensación de pendiente por curva (AREMA).
- Suavizado o simplificación del trazado.
- Costos de suelo: zonas urbanas, ríos, rutas, áreas protegidas, geología.
- Reutilización de vías existentes, o "imán" a estaciones.
- Límite de 2,5 ‰ en estaciones y playas de los extremos.
- Varios candidatos o alternativas por corredor; comparación de corredores.
- Cálculo de la pendiente mínima con la que sí habría camino.
- Persistencia de corredores (PostGIS), exportación, edición manual del trazado.
- Pendiente gobernante por tonelaje, tiempo de viaje o material rodante.
- Paso de malla, β o área de búsqueda configurables por el usuario.
- Tests de frontend.

## Inputs

- Dataset DEM de RW-004 (`data/regions/cordoba/elevation/elevation.rwe`) y `data/regions/cordoba.json`.
- Origen, destino y pendiente máxima elegidos por el usuario.

## Outputs

- `RailWeaver.Core.Planning`: `CorridorRequest`, `CorridorFinder`, `CorridorResult`, `CorridorSearchInfo`, `CandidateCorridor`, `TrackProfilePoint`, `CorridorMetrics`.
- Endpoint `POST /api/regions/{id}/corridors` y sus contratos.
- Herramienta "Corredor" con panel de tarea, dibujo en el mapa, detalle y perfil en el dock.
- Docs: `project-status.md`, `architecture/overview.md` (namespace Planning), `CHANGELOG.md`, README (uso de la herramienta), kickoff revisado.

## Acceptance criteria

- [x] `CorridorFinder` implementa §2.1–2.7 tal como están escritos: área, paso de malla por presupuesto de nodos, 16 vecinos, conexión de extremos, restricción dura, costo con β = 1, A* con desempate `(f, índice)` y métricas.
- [x] Invariante de pendiente: en todos los tests con resultado `Found`, cada tramo del `TrackProfile` cumple `|pendiente| ≤ límite`.
- [x] Plano llano sintético con extremos a ≥ 20 km → `Found` con sinuosidad ≤ 1,05. El tope teórico de la malla de 16 vecinos es ≈ 1,03; el resto es margen para las conexiones de los extremos.
- [x] Plano inclinado más empinado que el límite en línea recta, pero con desarrollo lateral posible → `Found`, trazado más largo que la línea recta y límite respetado.
- [x] Cordón continuo intransitable → `NoFeasiblePath` sin corredor. El mismo cordón con un paso → el trazado cruza por el paso.
- [x] Una barrera de NoData se trata como intransitable. Un extremo sin dato → `EndpointWithoutElevation`.
- [x] Selección del paso: un caso de test elige 250 m, otro 500 m y otro 1.000 m según el presupuesto de 1.000.000 de nodos.
- [x] Dos ejecuciones con la misma entrada dan resultados idénticos (test de determinismo). Hay un test de desempate con dos caminos de igual costo.
- [x] Validaciones del core (1–40 ‰, dentro del límite, ≥ 1 km) con tests.
- [x] El endpoint responde 200 (los tres estados), 400, 404 y 503 según §3, con tests de API sobre una grilla de prueba chica.
- [x] El redondeo de la respuesta es el de §3.
- [x] En el visor: herramienta "Corredor", panel de tarea con botones de referencia y campo numérico, marcado de origen y destino, cálculo cancelable, corredor dibujado, detalle con métricas y supuestos, perfil de terreno y vía en el dock, alerta de "no existe corredor" con qué/por qué/qué probar. Todo con tokens de DESIGN.md. El autor confirmó el funcionamiento y el trazado visible en el mapa; no se hizo verificación visual por agentes.
- [x] Verificación manual con el dataset real, registrada en esta spec con resultado, paso de malla, nodos explorados y tiempo medido:
  1. Llanura, Villa María → Río Cuarto con 10 ‰: se espera `Found` con sinuosidad ≤ 1,15.
  2. Córdoba → Cruz del Eje (Sierras Chicas; el Ramal A1 real llega a ~25 ‰) con 10 ‰ y con 25 ‰: se registra y explica.
  3. Córdoba → Mina Clavero (Altas Cumbres) con 10 ‰: se registra y explica.
- [x] Tiempo: cada caso manual responde en **≤ 20 s** en la máquina de desarrollo. Si no, se optimiza sin cambiar la semántica (por ejemplo, precalculando las cotas de los nodos en el orden de los bloques del DEM) antes de cerrar la spec.
- [x] `CoreBoundaryTests` en verde.
- [x] `dotnet test`, `npm --prefix src/web run lint` y `npm --prefix src/web run build` pasan localmente sin el dataset (95 tests); CI en verde sin el dataset.
- [ ] `project-status.md`, `architecture/overview.md`, `CHANGELOG.md` y README actualizados; kickoff revisado; M0 marcado como completa; tag `v0.5.0`.

### Registro de verificación con dataset real (2026-09-22)

DEM local de Córdoba (`elevation.rwe`, ignorado por git). Medición HTTP desde la
máquina de desarrollo, con la API en Debug y `Invoke-RestMethod`; el tiempo incluye
el request completo. Las coordenadas de Villa María, Río Cuarto, Córdoba y Cruz del
Eje provienen de `stations.geojson`; Mina Clavero se ubicó en
[GeoNames](https://www.geonames.org/3844229/mina-clavero.html).

| Caso | Resultado | Paso de malla | Nodos explorados | Tiempo | Observaciones |
|---|---|---|---|---|---|
| Villa María → Río Cuarto, 10 ‰ | `Found` | 250 m | 217.632 | 1,45 s | Sinuosidad 1,029 ≤ 1,15; 133,52 km |
| Córdoba → Cruz del Eje, 10 ‰ | `NoFeasiblePath` | 250 m | 139.260 | 0,75 s | No hay camino en la malla dentro del área de búsqueda con ese límite; no prueba imposibilidad física |
| Córdoba → Cruz del Eje, 25 ‰ | `Found` | 250 m | 254.548 | 1,50 s | Sinuosidad 1,473; 144,85 km; solución matemática sin criterios de curvas, obras ni uso de suelo |
| Córdoba → Mina Clavero, 10 ‰ | `NoFeasiblePath` | 250 m | 1 | 0,01 s | El origen no tiene una conexión transitable hacia la malla con ese límite; no prueba imposibilidad física |

El autor confirmó en la aplicación que ve origen, destino, trazado y datos técnicos.
Observó curvas no aptas para un proyecto ferroviario; radio mínimo y suavizado
están expresamente fuera de alcance de RW-005. La lista visual queda para futuras
revisiones humanas; los agentes no hacen verificaciones visuales.

Lista visual para el autor:

1. Confirmar que **Corredor** se habilita con relieve y muestra el detalle de dependencia cuando falta el DEM.
2. Revisar el panel de tarea: botones de referencia, valor libre, validaciones, estados de origen/destino y cancelación con botón o Esc.
3. Marcar tres puntos y comprobar que el tercero reinicia la selección.
4. Generar un corredor y revisar línea sólida con casing, rótulos, detalle `C-NN`, métricas, supuestos y perfil de terreno/vía con huecos rotulados.
5. Probar un caso sin camino y un extremo sin cota; confirmar la alerta y que permite ajustar parámetros.
6. Seleccionar el corredor y luego otro lugar del mapa; revisar el retorno al detalle y a la inspección normal.

## Relevant domain docs

- [railway-gradients-and-corridor-routing.md](../../research/terrain-routing/railway-gradients-and-corridor-routing.md)
- [digital-elevation-models.md](../../research/geography/digital-elevation-models.md)
- Kickoff §15 (Route / Tramo Generator), §38–39 (M0, V0.5) y §41.

## Relevant architecture

- [Overview](../../architecture/overview.md): el core define y ejecuta la búsqueda con `IElevationSource`. La API solo traduce HTTP y aporta el DEM y la cobertura. El navegador no calcula nada: solo dibuja.
- Planning como namespace dentro de `RailWeaver.Core`, sin proyecto nuevo (overview › Cómo crecerá).
- [DESIGN.md](../../../DESIGN.md) › Layout (panel de tarea temporal, dock), Components (detalle, alertas, botones), Cartography (infraestructura propuesta) y Charts (perfil).
- [ADR-006](../../decisions/ADR-006-discrete-event-simulation.md): determinismo.

## Tests

- **Core**, con grillas sintéticas en memoria (`IElevationSource` de prueba):
  - plano llano, plano inclinado, cordón con y sin paso, NoData;
  - selección de paso de malla, conexión de extremos y desempate;
  - métricas (bandas que suman la longitud, corte y relleno sobre un caso de valores conocidos);
  - invariante de pendiente, determinismo y validaciones.
- **API**: endpoint con la grilla de prueba del proyecto de tests (se amplía si hace falta). Casos 200 × 3 estados, 400, 404 y 503.
- **Frontera del core**: `CoreBoundaryTests` sin cambios.
- **Dataset real**: verificación manual registrada, porque CI no descarga el DEM.

## Decisiones resueltas antes de implementar

1. **Pendiente por tipo de tren + número libre** (decidido con el usuario). Los botones salen de la tabla de la nota de investigación: carga 10, mixta 15, pasajeros 25 y montaña 35 ‰. Los límites de 35 ‰ y 2,5 ‰ de TSI INF son verificables. Las citas argentinas (RITO/NTVO) de la nota no indican documento ni página, así que, igual que en RW-004 (decisión 6), los valores se muestran como "referencia, a validar" y no como norma.
2. **El límite se aplica a la línea de vía entre puntos de malla** (decidido con el usuario). Exigirlo cada 30 m sobre el terreno haría casi todo inviable por el ruido del DSM (árboles, edificios). La diferencia con el terreno se informa como corte y relleno implícitos, sin modelarlos como obra.
3. **Origen y destino por clic libre** (decidido con el usuario). El imán a estaciones queda fuera.
4. **Sin camino → aviso claro, sin buscar la pendiente mínima posible** (decidido con el usuario). Calcularla multiplicaría el tiempo de búsqueda.
5. **Paso de malla automático** (250/500/1.000 m con tope de 1.000.000 de nodos), no configurable. Mantiene el tiempo acotado en cualquier distancia dentro de Córdoba. Un paso grueso puede no ver gargantas angostas (Unknown 1 de la nota): la verificación manual lo registra.
6. **16 vecinos y trazado sin simplificar.** Simplificar o suavizar cambiaría la línea de vía y podría romper el límite. El suavizado queda para cuando existan curvas.
7. **β = 1 fijo.** No es un dato ferroviario: es un parámetro de preferencia (un tramo al límite cuesta el doble que uno llano), documentado como tal.
8. **Diferencias con el boceto de la nota:**
   - se usa `IElevationSource` (el nombre real de RW-004, no `IElevationService`);
   - `CorridorFinder` es una clase concreta, sin interfaz;
   - la heurística es la distancia de gran círculo en 2D;
   - el corredor no lleva `Id` en el core;
   - el namespace es `Planning`, como en Kickoff §21, y no `Corridors`.
9. **Determinismo por plataforma.** Mismo resultado en la misma plataforma y build. Entre sistemas operativos, las funciones trigonométricas pueden diferir en el último bit y, en un empate exacto, cambiar el camino. Es una limitación documentada, no un requisito.

## Open questions

Ninguna. Toda decisión nueva durante la implementación se registra acá antes de implementarla.
