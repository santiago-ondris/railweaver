# RW-007 — Curvas

- Status: Completed (2026-09-23)
- Milestone / release objetivo: M1 — Primer tren → V0.7, release `v0.7.0`.

## Goal

Que el corredor candidato de RW-005 deje de ser una poligonal de la malla y pase a
ser un **trazado ferroviario**: rectas unidas por arcos circulares, sin ninguna
curva más cerrada que el radio mínimo absoluto de su trocha, con la velocidad
admisible de cada curva y una vía que nunca supera la pendiente máxima, reducida
en las curvas (fidelidad de terreno V2, Kickoff §7).

El usuario elige, además de la pendiente, la **trocha** y la **velocidad de diseño**
de la línea. RailWeaver calcula el radio que esa velocidad necesita, lo usa donde
el terreno lo permite y, donde no, traza una curva más cerrada y marca que ahí el
tren debe ir más lento. Si ni siquiera así hay trazado, lo dice y no inventa uno.

Deja lista para RW-008 la lista de tramos del corredor con su velocidad máxima.

## Por qué ahora

Al verificar RW-005, el autor observó curvas no aptas para un proyecto ferroviario.
Además, RW-008 necesita la velocidad admisible en cada curva para calcular tiempos
de recorrido.

## Conceptos (en palabras simples)

- **Recta y curva:** el trazado alterna rectas y arcos de circunferencia. Cada arco
  empalma con sus rectas sin quiebre (la dirección es continua).
- **Radio:** el de la circunferencia del arco. Cuanto más chico, más cerrada la curva
  y más lento debe ir el tren.
- **Peralte:** el riel exterior de la curva va más alto que el interior para
  compensar la fuerza centrífuga. Tiene un máximo por trocha. RailWeaver no lo dibuja:
  lo usa para calcular velocidades.
- **Insuficiencia de peralte:** cuánta fuerza centrífuga queda sin compensar a la
  velocidad máxima. También tiene un máximo por trocha.
- **Velocidad de diseño:** la que el usuario quiere para la línea. Define el
  **radio de diseño**, el que permite esa velocidad con peralte e insuficiencia
  máximos.
- **Radio mínimo absoluto:** por debajo de este radio no se acepta ninguna curva, a
  ninguna velocidad.
- **Curva lenta:** curva con radio menor que el de diseño. Se acepta, pero su
  velocidad máxima es menor que la de diseño.
- **Compensación de pendiente en curva:** en una curva el tren tiene más resistencia
  al avance, como si subiera una pendiente extra. Por eso, en las curvas la pendiente
  admitida se reduce en esa pendiente extra.
- **Rasante:** la altura de la vía a lo largo del trazado. Ya no toca el terreno en
  cada vértice: puede ir por encima (**terraplén**) o por debajo (**corte**) para no
  superar la pendiente. RailWeaver informa cuánto, sin calcular la obra.

## Datos medidos antes de escribir la spec

Medidos con un prototipo descartable (C#, fuera del repo) sobre el DEM de Córdoba,
con los valores de §2.1. Justifican las decisiones y sirven de referencia para la
verificación.

1. **Suavizar después de la búsqueda no alcanza.** Se aplicó el suavizado de la nota
   de investigación (simplificación + arcos) al corredor de RW-005 sin cambios en la
   búsqueda:
   - Córdoba → Cruz del Eje (25 ‰), Córdoba → Cosquín (25 ‰) y Córdoba → Mina Clavero
     (35 ‰) fallaron en **todas** las combinaciones de trocha y velocidad: la búsqueda
     sube las laderas en zigzag, con giros de 153° a 162° en tramos de 250 m, que
     ningún arco de ≥ 100 m puede redondear.
   - Villa María → Río Cuarto (10 ‰) falló en trocha ancha por un quiebre junto al
     destino (radio posible 117 m < 300 m).
2. **Con la regla de giro dentro de la búsqueda (§2.4) no hubo ninguna falla de
   radio** en la etapa de suavizado (cero refinamientos en todos los casos) y cada
   búsqueda tardó ≤ 1,8 s en la máquina de desarrollo (build Release), con un
   pico de memoria del proceso de 225 MB.
3. Resultados de referencia (velocidad de diseño 80 km/h salvo indicación):

| Caso | Trocha | Resultado | Longitud | Curvas (lentas) | Radio mín. | Corte / terraplén máx. |
|---|---|---|---|---|---|---|
| Villa María → Río Cuarto, 10 ‰ | ancha | `Found` | 133,47 km | 56 (0) | 365 m | 6,7 / 5,6 m |
| Villa María → Río Cuarto, 10 ‰, 120 km/h | ancha | `Found` | 133,33 km | 56 (5), vel. mín. 89 km/h | 453 m | 6,0 / 5,0 m |
| Villa María → Río Cuarto, 10 ‰ | métrica | `Found` | 132,04 km | 49 (2, junto a los extremos) | 117 m | 4,2 / 4,5 m |
| Córdoba → Cruz del Eje, 25 ‰ | ancha | `NoFeasiblePath`, sin la regla de giro habría camino | — | — | — | — |
| Córdoba → Cruz del Eje, 25 ‰ | métrica | `Found` | 154,20 km | 99 (6) | 105 m | 33,9 / 29,7 m |
| Córdoba → Cosquín, 25 ‰ | ambas | `NoFeasiblePath`, sin la regla de giro habría camino | — | — | — | — |
| Córdoba → Cosquín, 35 ‰ | métrica | `Found` | 50,78 km | 38 (6) | 206 m | 49,6 / 33,4 m |
| Córdoba → Mina Clavero, 35 ‰ | métrica | `Found` | 162,03 km | 184 (81) | 135 m | 54,4 / 53,1 m |
| Córdoba → Mina Clavero, 35 ‰ | ancha | `NoFeasiblePath` | — | — | — | — |

Coordenadas usadas: las de `stations.geojson` para Villa María, Río Cuarto, Córdoba,
Cruz del Eje y Cosquín; Mina Clavero (−31,72100; −65,00619), de
[GeoNames](https://www.geonames.org/3844229/mina-clavero.html), como en RW-005.

## Scope

### 1. Investigación de dominio (resuelta)

[railway-curves-and-horizontal-alignment.md](../../research/infrastructure/railway-curves-and-horizontal-alignment.md).
La spec prevalece sobre la nota; las diferencias están en "Decisiones resueltas".

### 2. Core — `RailWeaver.Core.Planning` (sin E/S)

Todo el cálculo vive en el core. Archivos nuevos en `src/RailWeaver.Core/Planning/`
(`CurvatureRules.cs`, `AlignmentBuilder.cs`, `AlignmentModels.cs`) y
`src/RailWeaver.Core/Geography/AzimuthalEquidistantProjection.cs`. `CorridorFinder`
se modifica según §2.4.

#### 2.1 Parámetros por trocha (`CurvatureRules`)

Clase estática con constantes públicas y `GaugeCurveParameters For(TrackGauge gauge)`
(lanza `ArgumentException` para una trocha que no sea métrica ni ancha).

| Parámetro | Métrica (1.000 mm) | Ancha (1.676 mm) | Qué es |
|---|---|---|---|
| `ContactWidthMillimetres` (`s`) | 1.060 | 1.740 | Distancia entre los puntos de apoyo de las ruedas |
| `MaxCantMillimetres` (`h_max`) | 90 | 140 | Peralte máximo |
| `MaxCantDeficiencyMillimetres` (`I_max`) | 70 | 100 | Insuficiencia de peralte máxima |
| `AbsoluteMinimumRadiusMeters` (`R_abs`) | 100 | 300 | Radio mínimo absoluto |
| `CurveResistanceConstant` (`C`) | 500 | 650 | Resistencia en curva `C / R`, en ‰ |

Constantes comunes:

| Constante | Valor | Qué es |
|---|---|---|
| `GravityMetersPerSecondSquared` | 9,80665 | `g` |
| `MinDesignSpeedKmh` / `MaxDesignSpeedKmh` | 20 / 120 | Rango de la velocidad de diseño |
| `SimplificationToleranceFactor` | 0,5 | Tolerancia de simplificación = factor × paso de malla |
| `MaxArcChordMeters` | 10 | Largo máximo de cada cuerda al dibujar un arco |
| `RadiusTolerance` | 1e-9 | Tolerancia relativa al comparar radios calculados con `R_abs` |

Fórmulas (`V` en km/h, `R` en m, `h` e `I` en mm):

- Constante cinemática: `K = s / (3,6² · g)` → métrica ≈ 8,34; ancha ≈ 13,69.
- **Radio de diseño:** `R_d = max(R_abs, K · V_d² / (h_max + I_max))`.
- **Velocidad máxima en una curva de radio `R`:**
  `V(R) = min(V_d, √(R · (h_max + I_max) / K))`. En recta, `V_d`.
- **Pendiente admitida en una curva de radio `R`:** `max(0, G − C / R)` ‰, con `G`
  la pendiente máxima pedida.

Los valores salen de la nota de investigación. Sus citas argentinas (RITO/NTVO) no
indican documento ni página, así que se muestran como "referencia, a validar", igual
que en RW-004 y RW-005. De cada rango de la nota se toma el valor que usa su tabla de
radios (`h_max + I_max` = 160 y 240 mm), su radio excepcional más bajo (100 y 300 m)
y su ejemplo de compensación (`C` = 500 y 650).

#### 2.2 Pedido y validación

`CorridorRequest(GeoCoordinate Origin, GeoCoordinate Destination, double MaxGradientPermille,
TrackGauge Gauge, double DesignSpeedKmh, GeoBoundingBox SearchLimit)`.

A las validaciones de RW-005 se agregan (lanzan `ArgumentException`):

- `Gauge` métrica o ancha.
- `DesignSpeedKmh` finito y entre **20 y 120 km/h** inclusive. 120 es el máximo de la
  tabla de la nota: no se extrapola.

#### 2.3 Plano de cálculo

Toda la geometría horizontal (ángulos, radios, tangentes, arcos) se calcula en una
**proyección azimutal equidistante esférica** (`R` = 6.371.008,8 m) centrada en el
promedio de latitudes y longitudes de origen y destino. Clase pública
`AzimuthalEquidistantProjection(GeoCoordinate center)` con `Project` (→ `x` este, `y`
norte, en metros) e `Unproject`. A 200 km del centro la escala se desvía menos de
0,02 %: los radios y ángulos son exactos a efectos prácticos.

Las distancias a lo largo del trazado se miden, como en RW-004/005, por gran círculo
sobre la polilínea geográfica.

#### 2.4 Búsqueda con regla de giro (cambia RW-005 §2.4)

Área, malla, paso, 16 vecinos, conexión de extremos, restricción dura de pendiente
y costo quedan como en RW-005 §2.1–2.3. Cambia el estado de la búsqueda:

- **Estado = (nodo, movimiento de llegada).** Movimiento `h` ∈ 0…15 (índice en la
  lista de 16 vecinos de `CorridorFinder`) o `h = 16` para "llegó desde el origen",
  "es el origen" o "llegó al destino". Índice de estado = `índice de nodo · 17 + h`.
- Estado inicial: (origen, 16); si el origen está a < 1 m de una esquina, (esa esquina, 16),
  sin movimiento previo. Cualquier estado del destino termina la búsqueda.
- **Regla de giro.** Al pasar por el nodo `v` desde `p` hacia `n` (`p` es el nodo
  anterior del estado, que queda determinado por `h`):
  - `θ` = ángulo de giro en el plano de §2.3: `|atan2(u × w, u · w)|`, con
    `u = v − p` y `w = n − v`.
  - Disponible de cada lado: el largo del segmento en el plano si su otro extremo es
    el origen o el destino (ahí no hay otra curva), o la **mitad** del largo si no.
  - La transición existe solo si `R_abs · tan(θ / 2) ≤ min(disponible de entrada, disponible de salida)`.
    Se compara sin tolerancia. Desde el estado inicial no hay giro que verificar.
  - Así, cada giro del camino entra en un arco de radio ≥ `R_abs` sin pisarse con
    los vecinos. Una vuelta de 180° en un nodo nunca es posible.
- Cola ordenada por `(f, índice de estado)`; cerrados por estado; "estados explorados"
  = estados extraídos de la cola. La heurística sigue siendo la distancia de gran
  círculo al destino.
- La búsqueda no conoce la velocidad de diseño: solo `R_abs` de la trocha.
- `CorridorSearchInfo.ExploredNodes` pasa a llamarse `ExploredStates`.
- Si no hay camino, se repite la búsqueda **sin la regla de giro** (misma búsqueda con
  `R_abs = 0`) y el resultado informa `FeasibleWithoutCurveLimit` (`true` / `false`).
  Con `Found` o `EndpointWithoutElevation`, `null`.

La memoria puede usar arreglos por estado (17 × nodos) o una estructura dispersa, a
elección de la implementación, siempre que el resultado sea el mismo.

#### 2.5 Trazado en rectas y arcos (`AlignmentBuilder`)

Entrada: el camino de la búsqueda (origen, nodos, destino) en el plano de §2.3,
`R_d` y los parámetros de la trocha.

1. **Simplificación.** Ramer–Douglas–Peucker sobre los vértices del camino, con
   tolerancia `0,5 × paso de malla` (media celda: la malla no ubica el camino con más
   precisión que eso). Origen y destino se conservan siempre. Se divide en el vértice
   de mayor distancia a la cuerda; a igualdad, el de menor índice. Los vértices que
   quedan son los **PI** (puntos de intersección) `P_0 = origen, P_1…P_m, P_{m+1} = destino`.
2. **Ángulos.** Para cada PI interior, `Δ_k` = ángulo de giro (como en §2.4),
   `t_k = tan(Δ_k / 2)`, sentido izquierda si `u × w > 0`, derecha si `< 0`.
   `t_0 = t_{m+1} = 0`. Un PI con `Δ_k = 0` no genera curva.
3. **Radios.** `D_{a,b}` = distancia en el plano entre PI consecutivos.
   `R_k = min(R_d, D_{k−1,k} / (t_{k−1} + t_k), D_{k,k+1} / (t_k + t_{k+1}))`.
   Las tangentes `T_k = R_k · t_k` de dos curvas vecinas nunca se pisan y no hay que
   iterar: el reparto es proporcional y no depende del orden.
4. **Refinamiento.** Si algún `R_k < R_abs · (1 − 1e-9)`: se toma el de menor `R_k`
   (a igualdad, el de menor `k`), se vuelven PI **todos** los vértices del camino entre
   `P_{k−1}` y `P_{k+1}`, y se repiten los pasos 2 y 3. Termina: con todos los vértices
   del camino, la regla de giro de §2.4 garantiza `R_k ≥ R_abs`. En el prototipo
   nunca hizo falta, pero se implementa y se prueba.
5. **Geometría.** Para cada curva: `PC = P_k − û_entrada · T_k`; centro a distancia
   `R_k` de `PC`, perpendicular a `û_entrada`, hacia el lado del giro; el arco se
   dibuja con `⌈R_k · Δ_k / 10 m⌉` cuerdas de igual ángulo, y su último punto es el
   `PT`. Entre curvas, recta. Se omiten vértices consecutivos a menos de 1 mm (tangente
   de largo cero entre dos curvas). La polilínea se desproyecta a `GeoCoordinate`:
   ese es el nuevo `Alignment`.
6. **Tramos.** `AlignmentSection(SectionKind Kind, double FromMeters, double ToMeters,
   double? RadiusMeters, double? DeflectionDegrees, TurnDirection? Direction,
   double SpeedLimitKmh, GeoCoordinate? CurveMidpoint)`, en orden, cubriendo el
   trazado de 0 a `LengthMeters` sin huecos. `Kind`: `Tangent` | `Curve`. Las
   distancias son de gran círculo a lo largo del `Alignment`; los límites de cada
   curva son sus vértices PC y PT. No hay tramos de largo cero. `CurveMidpoint` = punto
   del trazado a la distancia `(From + To) / 2`, solo en curvas.

#### 2.6 Velocidades

`SpeedLimitKmh` = `V_d` en rectas y `V(R_k)` (§2.1) en curvas. **Curva lenta** =
curva con `SpeedLimitKmh < V_d`. Es el dato que usará RW-008.

#### 2.7 Rasante

1. `TerrainProfile` = `ElevationProfileBuilder.Build(Alignment)` (muestras cada 30 m,
   vértices incluidos).
2. **Puntos de rasante:** `n = ⌈L / paso de malla⌉`, separación `Δ = L / n`, punto `i`
   a la distancia `i · Δ` (`i = 0…n`). `L` = longitud del trazado.
3. **Terreno de referencia** `z_i` = promedio de las cotas de las muestras con dato a
   distancia `≤ Δ / 2` del punto (inclusive). Promediar ~250 m filtra árboles y
   edificios del DSM. Sin muestras con dato, el punto no tiene referencia (no acota).
4. **Pendiente admitida de cada cuerda** `a_i` (entre los puntos `i` e `i + 1`) =
   `max(0, G − max(C / R))` sobre las curvas que se superponen con la cuerda
   (`From < (i+1)·Δ` y `To > i·Δ`); `G` si ninguna. En metros: `b_i = a_i / 1000 · Δ`.
5. **Ajuste minimax.** `U` = la rasante más alta que nunca pasa por encima de ninguna
   referencia; `Lo` = la más baja que nunca pasa por debajo. Se calculan con dos
   barridos:
   - `U_i = z_i` (o `+∞` sin referencia); ida `U_i = min(U_i, U_{i−1} + b_{i−1})`;
     vuelta `U_i = min(U_i, U_{i+1} + b_i)`.
   - `Lo_i` igual con `−∞`, `max` y `−b`.
   - **Rasante** `y_i = (U_i + Lo_i) / 2`. Cumple `|y_{i+1} − y_i| ≤ b_i` por
     construcción y minimiza la mayor diferencia con el terreno de referencia.
   El primer y el último punto siempre tienen referencia (origen y destino tienen
   cota), así que `U` y `Lo` son finitos.
6. `TrackProfile`: un `TrackProfilePoint` por punto de rasante:
   `(DistanceMeters, Coordinate, ElevationMeters, GradientPermille, GradientLimitPermille)`.
   Pendiente y límite son los de la cuerda que termina en el punto (con signo la
   pendiente, en sentido origen → destino); el primer punto no tiene.
7. Corte y terraplén contra cada muestra de `TerrainProfile` con dato; la cota de la
   vía en la muestra se interpola linealmente entre puntos de rasante.

#### 2.8 Métricas (`CorridorMetrics`)

Se conservan las de RW-005 §2.7, calculadas ahora sobre el nuevo trazado y la
rasante (`DistanceByGradientBand` con las cuerdas de la rasante y relativa a `G`).
Se agregan:

| Métrica | Definición |
|---|---|
| `Gauge` | Trocha pedida |
| `DesignSpeedKmh` / `DesignRadiusMeters` | `V_d` y `R_d` |
| `AbsoluteMinimumRadiusMeters` | `R_abs` de la trocha |
| `CurveCount` / `ReducedSpeedCurveCount` | Curvas y curvas lentas |
| `MinimumRadiusMeters` | Menor radio; `null` sin curvas |
| `MinimumSpeedKmh` | Menor `SpeedLimitKmh` de los tramos |
| `CurveLengthMeters` | Suma del largo de las curvas |

`CandidateCorridor` pasa a ser `(Alignment, Sections, TrackProfile, TerrainProfile, Metrics)`.
El camino quebrado de la malla no se devuelve.

#### 2.9 Invariantes (verificadas por tests)

1. **Radio:** toda curva tiene `R ≥ R_abs · (1 − 1e-9)`.
2. **Tangencia:** en cada PC y PT la dirección de la recta y la del arco difieren en
   menos de 1e-6 rad (medido en el plano).
3. **Pendiente:** cada cuerda de la rasante cumple `|pendiente| ≤ a_i + 1e-9`; en
   particular, nunca supera `G`, y en curvas nunca supera `G − C / R`.
4. **Velocidad:** en cada tramo, `SpeedLimitKmh ≤ V_d`, y en cada curva es `V(R)`.
5. **Sin curva imposible:** si no hay camino que cumpla la regla de giro, el resultado
   es `NoFeasiblePath` sin corredor. Nunca se relaja `R_abs` ni la pendiente, ni se
   devuelve un trazado parcial.
6. **Determinismo:** misma entrada, mismo `IElevationSource` → mismo resultado bit a
   bit en la misma plataforma.
7. El core no hace E/S (`CoreBoundaryTests`).

### 3. API — `POST /api/regions/{id}/corridors` (cambia RW-005 §3)

Cuerpo:

```json
{
  "origin": { "latitude": -32.4126, "longitude": -63.2448 },
  "destination": { "latitude": -33.1309, "longitude": -64.3391 },
  "maxGradientPermille": 10,
  "gaugeMillimetres": 1676,
  "designSpeedKmh": 80
}
```

- **400** además si falta `gaugeMillimetres` o `designSpeedKmh`, si la trocha no es
  1000 ni 1676, o si la velocidad está fuera de 20–120 km/h.
- `search.exploredNodes` pasa a `search.exploredStates`.
- Respuesta de `no_feasible_path`: agrega `"feasibleWithoutCurveLimit": true | false`
  en el nivel raíz (`null` en los otros estados).
- `corridor` con `found`:
  - `alignment`: la polilínea en rectas y arcos;
  - `sections`: `[{ kind: "tangent" | "curve", fromMeters, toMeters, radiusMeters,
    deflectionDegrees, direction: "left" | "right" | null, speedLimitKmh,
    curveMidpoint: { latitude, longitude } | null }]`;
  - `trackProfile`: agrega `gradientLimitPermille`;
  - `metrics`: agrega `gaugeMillimetres`, `designSpeedKmh`, `designRadiusMeters`,
    `absoluteMinimumRadiusMeters`, `curveCount`, `reducedSpeedCurveCount`,
    `minimumRadiusMeters`, `minimumSpeedKmh`, `curveLengthMeters`.
- Redondeo solo en la respuesta: metros (incluidos radios) a 0,01, ‰ a 0,1, grados a
  0,1, km/h a 0,1, sinuosidad a 0,001.

### 4. Frontend (según `DESIGN.md`, textos en español)

Cambios en el módulo `src/web/src/planning/`.

#### 4.1 Panel de tarea "Corredor candidato"

Orden: Pendiente máxima (sin cambios), **Trocha**, **Velocidad de diseño**, Origen,
Destino, botones.

- **Trocha:** dos botones `button-secondary` con `aria-pressed`: "Ancha 1.676 mm"
  (seleccionado por defecto) y "Métrica 1.000 mm". El seleccionado lleva fondo
  `primary`, texto `on-primary` y una marca visible.
- **Velocidad de diseño:** tres botones de referencia "Montaña 40 km/h",
  "Regional 80 km/h" (por defecto) y "Troncal 120 km/h", y un campo numérico en km/h
  (20–120, paso 1) que se comporta como el de pendiente. Salen de la tabla de la nota
  (serrano 40–50, regional 70–90, troncal 100–120 km/h).
- Debajo, en la cara de datos: "Radio de diseño {R_d} m · mínimo absoluto {R_abs} m",
  calculado en el navegador con las fórmulas de §2.1 solo para mostrar (la API
  decide). Nota: "Valores de referencia, a validar."
- Validación antes de llamar a la API: velocidad fuera de 20–120 km/h, con mensaje
  junto al campo.

#### 4.2 Corredor en el mapa

- La línea del corredor como en RW-005 (sólida `primary`, casing `map-ground`), ahora
  con el trazado en rectas y arcos.
- **Solo las curvas lentas** se sobredibujan en `warning` y llevan el triángulo junto
  a su `curveMidpoint`, con el rótulo en la cara de datos "R {radio} m · {velocidad} km/h"
  (DESIGN.md › Cartography). Las demás curvas no llevan marca.
- Tras la revisión visual del autor: a escala regional permanecen los tramos
  `warning` pero se ocultan los rótulos individuales. Al acercarse, se muestran
  solo los rótulos que no se superponen en pantalla, priorizando las curvas con
  menor velocidad admisible. El detalle indica que hay que acercarse para leerlos.
- Clic en una marca o en la línea → detalle del corredor.

#### 4.3 Panel de detalle

- Procedencia: "Límite 15,0 ‰ · trocha 1.676 mm · 80 km/h · malla 250 m".
- Métricas nuevas: velocidad de diseño, radio de diseño, curvas, radio mínimo,
  curvas con velocidad reducida y velocidad mínima. Si hay curvas lentas, esas dos
  métricas van en `warning` con su triángulo.
- "Nodos explorados" pasa a "Estados explorados".
- Nota de supuestos: "Curvas circulares sin transiciones; peralte supuesto, no
  dibujado. Corte y terraplén sin calcular la obra. Sin túneles, puentes ni costos de
  suelo. Valores de referencia, a validar."
- Las velocidades se muestran redondeadas hacia abajo al km/h entero; los radios al
  metro.

#### 4.4 Dock de análisis

- Perfil como en RW-005, con la rasante en lugar de la línea de vía por vértices. La
  anotación pasa a "máx. {x} ‰ ≤ límite {G} ‰ · reducido en curvas".
- **Banda de curvas** debajo del perfil, con el mismo eje de distancia: una línea de
  base `border-subtle` para las rectas; cada curva es un rectángulo sobre la base si
  gira a la derecha y bajo la base si gira a la izquierda, en `ink-subtle`, o en
  `warning` si es lenta. Rótulo "R {radio}" en la cara de datos solo si el rectángulo
  mide al menos 40 px de ancho. Leyenda: "Arriba: curva a la derecha · Abajo: a la
  izquierda".

#### 4.5 Resultado `no_feasible_path`

Durante la búsqueda, una tarjeta centrada sobre el mapa informa que se está
generando el corredor, muestra los segundos transcurridos y permite cancelar sin
perder origen ni destino. La alerta `alarm` aparece en esa misma posición cuando
la búsqueda falla; el panel de tarea queda abierto para modificar parámetros.

- **Título:** "No existe un corredor con pendiente ≤ {G} ‰ y curvas de radio ≥ {R_abs} m
  entre estos puntos".
- **Por qué:**
  - con `feasibleWithoutCurveLimit = true`: "Con esa pendiente hay camino, pero exige
    curvas más cerradas que el mínimo absoluto de la trocha {X} mm ({R_abs} m)."
  - con `false`: el texto de RW-005 (el relieve exige pendientes mayores en toda el
    área; esta versión no usa túneles ni puentes).
- **Qué probar:** con `true` y trocha ancha: "Trocha métrica (admite curvas más
  cerradas), subir el límite de pendiente o mover los puntos". Con `true` y métrica:
  "Subir el límite de pendiente o mover los puntos". Con `false`: el texto de RW-005.
- Se muestran el paso de malla y los estados explorados.

## Non-goals

- Curvas en la red existente de OSM (decisión 1).
- Curvas de transición (clotoides), rampas de peralte y peralte dibujado en 3D.
- Distancia mínima en recta entre curvas (curvas en S sin recta intermedia) y
  acuerdos verticales de la rasante.
- Exceso de peralte para trenes lentos y velocidades distintas por tipo de tren.
- Obras: volúmenes de corte y terraplén, túneles, puentes, costos de suelo.
- Mostrar el camino quebrado de la malla.
- Ubicar el lugar exacto que impide el trazado cuando no hay corredor.
- Tiempos de recorrido y material rodante (RW-008).
- Edición manual del trazado, radios por curva elegidos por el usuario, persistencia.
- Tests de frontend.

## Simplificaciones

- **Arcos circulares sin transiciones.** A escala territorial la diferencia con una
  clotoide es de centímetros a pocos metros (nota, Simplificación 1).
- **Peralte supuesto al máximo de la trocha** para calcular `V(R)`; no se calcula el
  peralte de cada curva ni se dibuja.
- **Curvas solo en los vértices de la malla.** La búsqueda ve giros cada 250 m o más.
  Un `NoFeasiblePath` no prueba que el trazado sea físicamente imposible, igual que en
  RW-005.
- **Curvas lentas junto a los extremos.** La conexión del origen o el destino a la
  malla puede ser corta y obligar a una curva más cerrada ahí (visto en Villa María →
  Río Cuarto, métrica).
- **Compensación también en bajada.** Una línea se recorre en los dos sentidos, así
  que la reducción `C / R` se aplica a la pendiente absoluta.
- **Pendiente admitida con piso en 0 ‰.** Si `C / R > G` (límites muy bajos), la
  curva debe ser horizontal y aun así su resistencia supera el límite. Solo puede
  pasar con `G` < 2,2 ‰ en ancha (650 / 300) y < 5 ‰ en métrica (500 / 100).
- **Cuerdas de rasante que tocan una curva** usan la pendiente admitida de la curva
  más cerrada que tocan, en toda la cuerda (conservador).
- **Rasante con extremos libres:** la vía puede empezar o terminar algo por encima o
  por debajo del terreno.
- **Terreno de referencia promediado** cada ~250 m: el corte y terraplén informados se
  miden contra las muestras de 30 m, sin promediar.

## Inputs

- DEM de RW-004 y `data/regions/cordoba.json`.
- Origen, destino, pendiente máxima, trocha y velocidad de diseño elegidos por el usuario.

## Outputs

- `RailWeaver.Core.Planning`: `CurvatureRules`, `GaugeCurveParameters`, `AlignmentBuilder`,
  `AlignmentSection`, `SectionKind`, `TurnDirection`; `CorridorRequest`, `CorridorResult`,
  `CorridorSearchInfo`, `CandidateCorridor`, `TrackProfilePoint` y `CorridorMetrics`
  modificados.
- `RailWeaver.Core.Geography.AzimuthalEquidistantProjection`.
- Endpoint `POST /api/regions/{id}/corridors` con el contrato de §3.
- Panel con trocha y velocidad, curvas lentas en el mapa, detalle, banda de curvas y
  alerta nueva.
- Docs: `project-status.md`, `architecture/overview.md` (curvas en Planning),
  `CHANGELOG.md`, README (uso de la herramienta), kickoff revisado y nota de
  investigación enlazando a esta spec.

## Acceptance criteria

- [x] `CurvatureRules` con los valores y fórmulas de §2.1.
- [x] `CorridorFinder` implementa la búsqueda por estados y la regla de giro de §2.4,
      incluida la repetición sin regla para `FeasibleWithoutCurveLimit`.
- [x] `AlignmentBuilder` implementa §2.5–2.7 tal como están escritos.
- [x] Tests sintéticos del core (§Tests) en verde, incluidas las invariantes de §2.9.
- [x] Con el DEM real, verificación manual registrada en esta spec con resultado,
      longitud, curvas, curvas lentas, radio mínimo, corte y terraplén, estados
      explorados, tiempo y memoria, para los casos de la tabla de "Datos medidos". Se
      espera el mismo resultado (`Found` / `NoFeasiblePath`); longitudes dentro de
      ±2 % y conteos de curvas dentro de ±10 % del prototipo. Una diferencia mayor se
      explica antes de cerrar.
- [x] Tiempo: cada caso manual responde en **≤ 20 s** (incluido el reintento sin regla
      de giro) en la máquina de desarrollo. Se registra el pico de memoria de la API
      durante el caso más grande.
- [x] Endpoint según §3: 200 (tres estados, con `feasibleWithoutCurveLimit`), 400
      (incluidos trocha y velocidad), 404 y 503, con tests de API sobre la grilla de
      prueba.
- [x] En el visor: trocha y velocidad en el panel, radio de diseño mostrado,
      corredor en rectas y arcos, curvas lentas marcadas y rotuladas, detalle con las
      métricas nuevas, rasante en el perfil, banda de curvas, alerta de §4.5. Todo con
      tokens de `DESIGN.md`. El autor confirma el funcionamiento; los agentes no hacen
      verificación visual y entregan una lista específica de puntos a revisar.
- [x] `CoreBoundaryTests` en verde. `dotnet test`, `npm --prefix src/web run lint`,
      `format:check` y `build` pasan localmente y en CI sin el DEM.
- [x] `project-status.md`, `architecture/overview.md`, `CHANGELOG.md` y README
      actualizados; kickoff revisado; tag `v0.7.0`.

### Verificación con el DEM real (2026-09-23)

API local en Debug, peticiones HTTP completas. Las coordenadas de las estaciones
son de `stations.geojson` y Mina Clavero usa las de "Datos medidos". Las longitudes
y los conteos coinciden con el prototipo dentro de las tolerancias de aceptación.
Los tiempos incluyen el reintento sin radio en los casos sin corredor.

| Caso | Estado | Longitud km | Curvas (lentas) | Radio mín. m | Corte / terraplén máx. m | Estados | Tiempo s |
|---|---|---:|---:|---:|---:|---:|---:|
| Villa María → Río Cuarto, 10 ‰, ancha, 80 | `Found` | 133,47 | 56 (0) | 365 | 6,75 / 5,60 | 1.180.646 | 2,62 |
| Villa María → Río Cuarto, 10 ‰, ancha, 120 | `Found` | 133,33 | 56 (5) | 453 | 5,99 / 5,02 | 1.180.646 | 2,79 |
| Villa María → Río Cuarto, 10 ‰, métrica, 80 | `Found` | 132,00 | 48 (2) | 103 | 4,53 / 4,20 | 1.123.497 | 2,42 |
| Córdoba → Cruz del Eje, 25 ‰, ancha | `NoFeasiblePath` (sin radio: sí) | — | — | — | — | 1.114.900 | 4,42 |
| Córdoba → Cruz del Eje, 25 ‰, métrica | `Found` | 154,20 | 99 (6) | 105 | 33,87 / 29,66 | 1.385.814 | 3,17 |
| Córdoba → Cosquín, 25 ‰, ancha | `NoFeasiblePath` (sin radio: sí) | — | — | — | — | 237.754 | 0,82 |
| Córdoba → Cosquín, 25 ‰, métrica | `NoFeasiblePath` (sin radio: sí) | — | — | — | — | 251.175 | 0,75 |
| Córdoba → Cosquín, 35 ‰, métrica | `Found` | 50,78 | 38 (6) | 206 | 49,58 / 33,42 | 259.908 | 0,49 |
| Córdoba → Mina Clavero, 35 ‰, métrica | `Found` | 162,03 | 184 (81) | 135 | 54,35 / 53,08 | 1.156.621 | 2,56 |
| Córdoba → Mina Clavero, 35 ‰, ancha | `NoFeasiblePath` (sin radio: sí) | — | — | — | — | 919.950 | 3,60 |

Pico de RSS observado al muestrear el proceso API cada 0,1 s: **744.576 KiB**
(caso Córdoba → Cruz del Eje, métrica). Incluye caché del DEM y memoria retenida de
peticiones anteriores; no es memoria incremental del caso.

El autor verificó además Córdoba → Nono: la opción de montaña encuentra un trazado
con curvas naturales y la de pasajeros lo rechaza. Confirmó la legibilidad de las
curvas lentas en distintos niveles de zoom y los mensajes centrales de progreso y
error. En otro recorrido excepcionalmente largo observó unos 5 millones de estados
explorados y unos 300 s; queda registrado como límite conocido para optimización
posterior, fuera de los diez casos de aceptación medidos arriba.

## Relevant domain docs

- [railway-curves-and-horizontal-alignment.md](../../research/infrastructure/railway-curves-and-horizontal-alignment.md)
- [railway-gradients-and-corridor-routing.md](../../research/terrain-routing/railway-gradients-and-corridor-routing.md)
- [digital-elevation-models.md](../../research/geography/digital-elevation-models.md) (DSM)
- Kickoff §7 (fidelidad V2), §15, §38–39 (M1, V0.7).

## Relevant architecture

- [Overview](../../architecture/overview.md): el core busca y arma el trazado; la API
  traduce HTTP y aporta el DEM; el navegador solo dibuja (el radio de diseño del panel
  es solo informativo).
- Curvas dentro del namespace `Planning` (Kickoff: Planning › Alignment, Curvature),
  sin proyecto nuevo.
- [ADR-006](../../decisions/ADR-006-discrete-event-simulation.md): determinismo.
- [ADR-010](../../decisions/ADR-010-frontend-structure-and-formatting.md): módulo `planning`.
- [DESIGN.md](../../../DESIGN.md) › Layout, Components (botones, alertas, métricas en
  warning), Cartography (segmentos sobredibujados en `warning`) y Charts (perfil).

## Tests

- **Core, con grillas sintéticas en memoria:**
  - `CurvatureRules`: `K`, `R_d` y `V(R)` con valores conocidos (p. ej. ancha a 120 km/h →
    `R_d` ≈ 821 m; métrica a 40 km/h → `R_d` = 100 m por el mínimo absoluto).
  - Proyección: ida y vuelta con error < 1 mm; distancia y ángulo contra casos conocidos.
  - Regla de giro: con dos caminos de igual pendiente, uno con un giro de 90° en un
    nodo y otro más largo con giros suaves, la búsqueda toma el suave en ancha y
    puede tomar el de 90° en métrica si entra; una vuelta en U en dos nodos es
    imposible en ancha.
  - Plano llano con extremos a ≥ 20 km: `Found`, trazado casi recto, sin curvas lentas
    a 80 km/h.
  - Valle en forma de L: una curva, radio `R_d` si hay espacio; con el valle más
    angosto, curva lenta con `V(R)` correcto.
  - Regla sin camino: un paso de montaña que solo se cruza en zigzag → `NoFeasiblePath`
    con `FeasibleWithoutCurveLimit = true`.
  - Radios: reparto proporcional con dos curvas vecinas; tangente al origen.
  - Refinamiento: una polilínea armada a mano donde la simplificación produce
    `R < R_abs` y el refinamiento lo corrige.
  - Rasante: terreno en escalón con límite conocido (corte y terraplén iguales, máximo
    minimizado); compensación `C / R` aplicada en la cuerda de una curva.
  - Invariantes de §2.9 en todos los casos `Found`; tramos que cubren el trazado sin
    huecos; determinismo; validaciones de trocha y velocidad.
- **API:** endpoint con la grilla de prueba: 200 × 3 estados con los campos nuevos,
  400 (trocha, velocidad, campos faltantes), 404 y 503.
- **Frontera del core:** `CoreBoundaryTests` sin cambios.
- **Dataset real:** verificación manual registrada (CI no descarga el DEM).

## Decisiones resueltas antes de implementar

1. **Solo el corredor; la red existente queda afuera** (decidido con el usuario). La
   geometría de OSM tiene ruido de digitalización que produce radios falsos; si
   RW-008 necesita velocidades en curvas de la red existente, se decide ahí.
2. **El usuario elige la velocidad de diseño, no el radio** (decidido con el usuario).
   RailWeaver calcula el radio de diseño y lo muestra.
3. **Curvas más cerradas que el diseño se aceptan con velocidad reducida, hasta el
   mínimo absoluto** (decidido con el usuario). Es lo que ocurre en las líneas reales.
4. **Rasante con corte y terraplén, siempre dentro de la pendiente** (decidido con el
   usuario). Es la "aproximación básica de movimiento de suelos" de la fidelidad V2
   (Kickoff §7). Reemplaza la regla de RW-005 de tocar el terreno en cada vértice.
5. **Trocha elegida por el usuario, ancha por defecto** (decidido con el usuario). En
   RW-010 el ramal se une a la red de esa trocha. La estándar (1.435 mm) no se ofrece:
   la nota no da sus límites y no hay red de esa trocha en Córdoba.
6. **Bajo el mínimo absoluto no hay corredor** (decidido con el usuario), como con la
   pendiente en RW-005. En lugar de marcar un punto exacto (con la regla dentro de la
   búsqueda no existe "la" curva que falla), la alerta dice si la causa son las curvas
   o la pendiente.
7. **Se muestran solo las curvas lentas en el mapa, más la banda de curvas en el
   perfil** (decidido con el usuario). El camino de la malla no se muestra.
8. **La regla de radio va dentro de la búsqueda** (medido, ver "Datos medidos"): el
   suavizado posterior de la nota falló en todos los casos serranos por los zigzags de
   la búsqueda sin regla.
9. **Minimax para la rasante:** exacto, determinista, lineal en tiempo y nunca falla.
   Se aplica sobre el terreno promediado para que un árbol o un edificio del DSM no
   levante la vía kilómetros.
10. **Diferencias con la nota de investigación:**
    - se usa `IElevationSource` (no `IElevationService`);
    - no se fusionan curvas vecinas ni se reubican vértices: el reparto proporcional y
      el refinamiento resuelven los solapes, y la regla de giro garantiza el radio;
    - tipos `AlignmentSection` en lugar de `AlignmentSegment`/`TrackAlignment`, dentro
      de `Planning` y no de `Geometry`; no hay métrica `CurvatureSinuosity`;
    - de cada rango de la nota se toma un valor (§2.1).
11. **Determinismo por plataforma**, como en RW-005 (decisión 9).

## Open questions

Ninguna. Toda decisión nueva durante la implementación se registra acá antes de implementarla.
