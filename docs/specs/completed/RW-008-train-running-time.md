# RW-008 — Tren V1 y tiempo de recorrido

- Status: Completed (2026-09-23; verificación visual confirmada por el autor)
- Milestone / release objetivo: M1 — Primer tren → V0.8, release `v0.8.0`.

## Goal

Elegir un tren (material rodante V1: largo, velocidad máxima, aceleración y
frenado; Kickoff §7) y calcular de forma determinista su **perfil de velocidad** y
su **tiempo de recorrido** sobre un corredor candidato (RW-007) o sobre una ruta
entre estaciones de la red existente (RW-006).

El resultado se explica: dónde acelera, dónde va a su máxima, dónde lo limita la
vía y por qué (curva, velocidad de la línea, cola todavía en la curva), dónde frena
y cuánto tiempo le cuesta la curva más restrictiva. Es cálculo puro en el core, sin
motor de eventos (RW-009).

## Por qué ahora

Separa la física del recorrido del motor de eventos: RW-009 va a mover el tren con
este perfil y RW-010 lo va a usar sobre un ramal unido a la red.

## Conceptos (en palabras simples)

- **Tren V1:** cuatro datos. Largo, velocidad máxima, aceleración (constante) y
  frenado de servicio (constante). Sin masa, potencia ni pendiente (eso es V2).
- **Límite de la vía:** la velocidad máxima de cada tramo. En un corredor sale de
  RW-007: la velocidad de diseño en rectas y la de cada curva. En la red existente
  la elige el usuario, un valor para toda la ruta.
- **Regla de la cola:** al salir de una curva lenta, el tren no acelera hasta que
  su último vagón salió de la curva. Al entrar, la cabeza ya tiene que venir a la
  velocidad de la curva.
- **Perfil de velocidad:** la velocidad del tren en cada punto del recorrido. En
  cada punto es la menor entre: lo que alcanzó acelerando, el límite de la vía, su
  propia máxima y lo que le permite frenar a tiempo para el próximo límite o parada.
- **Inversión de marcha:** en la red existente, el tren pasa el desvío hasta que la
  cola lo libera, se detiene, el conductor cambia de cabina y retrocede hacia la otra
  vía. El tramo que recorre más allá del desvío es la **maniobra** y mide lo mismo
  que el tren. La **vía de maniobra** es la vía donde hace ese tramo.
- **Tiempo de marcha puro:** sin márgenes de horario ni paradas intermedias.

## Datos medidos antes de escribir la spec

- De 2.335 `way` de `railway.overpass.json`, solo **59 (2,5 %)** tienen `maxspeed`
  (valores entre 15 y 70 km/h, casi todos en vías principales). No alcanzan para
  dar velocidades a la red existente: la elige el usuario (decisión 2).
- La tabla de la nota de investigación y su texto no coinciden en el tren de carga:
  con 0,06 m/s² y 0,25 m/s², llegar de 0 a 60 km/h lleva 2,3 km (el texto dice
  más de 4 km) y frenar desde 60 km/h, 556 m (el texto dice más de 800 m). La spec
  usa los valores de la tabla, como referencia a validar.
- Referencia analítica para la verificación, con la fórmula de §2.4 (el tren alcanza
  la velocidad de la vía): tiempo = `D / V + V / (2a) + V / (2b)` por etapa.
  Con "Pasajeros troncal" a 100 km/h en la vía:
  - Córdoba → Villa María (141,6 km, sin inversiones) → **≈ 5.181 s (1 h 26 min 21 s)**.
- Córdoba → Río Cuarto (272,4 km, una inversión, maniobra de 200 m, 0 min
    detenido) → **≈ 9.980 s (2 h 46 min 20 s)**.

Medición de implementación (2026-09-23, dataset real, con el algoritmo de §2.8):
la vía de maniobra de la inversión de Córdoba → Río Cuarto y de Córdoba → Alta
Córdoba alcanza en ambos casos el tope de búsqueda de **1.500 m**.

Medición local con DEM real (pedidos HTTP, sin verificación visual del visor):

| Corredor | Tren | Tiempo total | Regímenes A/M/V/F (s) | Curva más costosa | Cálculo |
|---|---|---:|---|---|---:|
| Villa María → Río Cuarto, 10 ‰, ancha, 120 km/h | Pasajeros troncal | 4.108,5 s | 143,3 / 3.833,3 / 60,1 / 71,7 | R 458,54 m, km 132,23, +3,1 s | 0,031 s |
| Villa María → Río Cuarto | Coche motor serrano | 6.038,0 s | 44,4 / 5.961,9 / 0 / 31,7 | — | 0,009 s |
| Villa María → Río Cuarto | Carga granelero | 8.172,2 s | 277,8 / 7.827,7 / 0 / 66,7 | — | 0,009 s |
| Córdoba → Cosquín, 35 ‰, métrica, 80 km/h | Pasajeros troncal | 2.373,4 s | 118,0 / 0 / 2.196,4 / 59,0 | R 205,53 m, km 32,07, +3,2 s | 0,010 s |
| Córdoba → Cosquín | Coche motor serrano | 2.340,3 s | 59,0 / 2.154,3 / 84,8 / 42,1 | R 205,53 m, km 32,07, +3,2 s | 0,009 s |
| Córdoba → Cosquín | Carga granelero | 3.219,3 s | 277,8 / 2.874,8 / 0 / 66,7 | — | 0,004 s |

`A/M/V/F`: acelerando / a la máxima del tren / limitado por la vía / frenando.
La búsqueda del corredor tardó 2,613 s y 0,569 s, respectivamente; no entra en
el límite de 1 s del cálculo de tiempo. El autor confirmó el funcionamiento y la
presentación en el visor el 2026-09-23.
En los siete pedidos HTTP de rutas alcanzables de la tabla RW-006, con el tren
troncal a 100 km/h, la primera respuesta (topología fría) tardó 0,765 s y las
otras seis 0,005–0,020 s. Córdoba → Villa María dio 5.181,8 s y Córdoba → Río
Cuarto 9.980,0 s, dentro del ±0,5 % previsto.

## Scope

### 1. Investigación de dominio (resuelta)

[train-v1-and-running-time-calculation.md](../../research/rolling-stock/train-v1-and-running-time-calculation.md).
La spec prevalece sobre la nota; las diferencias están en "Decisiones resueltas".

### 2. Core (sin E/S)

Namespaces nuevos: `RailWeaver.Core.RollingStock` (el tren) y
`RailWeaver.Core.Operations` (el cálculo de tiempo de recorrido), según los
contextos del Kickoff (Rolling Stock; Operations define cómo se usa la
infraestructura). Carpetas `src/RailWeaver.Core/RollingStock/` y
`src/RailWeaver.Core/Operations/`.

#### 2.1 Tren (`RailWeaver.Core.RollingStock`)

`RollingStockV1(string Name, double LengthMeters, double MaxSpeedKmh,
double AccelerationMetersPerSecondSquared, double BrakingMetersPerSecondSquared)`.

Validación (lanza `ArgumentException`; son rangos de entrada, no reglas ferroviarias):

| Dato | Rango (inclusive) |
|---|---|
| `Name` | no vacío |
| `LengthMeters` | 10–1.500 m |
| `MaxSpeedKmh` | 10–160 km/h |
| `AccelerationMetersPerSecondSquared` | 0,01–1,5 m/s² |
| `BrakingMetersPerSecondSquared` | 0,05–1,5 m/s² |

`RollingStockPresets` (clase estática, lista ordenada) con los tres tipos de la
nota. De cada rango se toma el **valor más bajo** (conservador: acelera más lento y
frena antes):

| Id | Nombre | Largo | Vel. máx. | Aceleración | Frenado |
|---|---|---|---|---|---|
| `intercity-passenger` | Pasajeros troncal | 200 m | 120 km/h | 0,25 m/s² | 0,50 m/s² |
| `mountain-railcar` | Coche motor serrano | 42 m | 80 km/h | 0,50 m/s² | 0,70 m/s² |
| `bulk-freight` | Carga granelero | 550 m | 60 km/h | 0,06 m/s² | 0,25 m/s² |

El tren no tiene trocha: en V1 sus datos no dependen de ella y recorre la vía que
se le da (decisión 6).

#### 2.2 Entrada del cálculo (`RailWeaver.Core.Operations`)

- `SpeedSection(double FromMeters, double ToMeters, double SpeedLimitKmh,
  SpeedLimitSource Source, double? CurveRadiusMeters)`.
  `SpeedLimitSource`: `LineSpeed` (red existente), `DesignSpeed` (recta de corredor),
  `Curve` (curva de corredor).
- `ReversalPoint(double DistanceMeters, double? ManeuverTrackMeters)`.
- `RunningTimeRequest(IReadOnlyList<SpeedSection> Sections, IReadOnlyList<ReversalPoint> Reversals,
  RollingStockV1 Train, double ReversalDwellSeconds)`.

Validación (`ArgumentException`): al menos una sección; la primera empieza en 0;
cada una empieza donde termina la anterior (igualdad exacta); `ToMeters > FromMeters`;
límites finitos y > 0; `CurveRadiusMeters` solo con `Curve`; inversiones en orden
estrictamente creciente y estrictamente dentro de `(0, longitud)`;
`ReversalDwellSeconds` entre 0 y 7.200 s.

Longitud de la ruta `D` = `ToMeters` de la última sección.

#### 2.3 Etapas y límite efectivo

1. **Etapas.** Las inversiones cortan la ruta en etapas. Cada etapa empieza con el
   tren detenido. La etapa que termina en una inversión se alarga con una **sección
   de maniobra** de largo `L` (largo del tren), con `Source = ReversalManeuver` y el
   límite de la última sección de la etapa. La siguiente etapa empieza en el punto de
   inversión: la nueva cabeza (la cola de antes) está sobre el desvío. La última etapa
   termina en el destino.
2. **Tope del tren.** Cada límite se toma como `min(límite, MaxSpeedKmh)`.
3. **Regla de la cola.** Dentro de cada etapa (distancia `s` desde su inicio),
   `V_ef(s) = mín { V(u) : max(0, s − L) ≤ u ≤ s }`. Como los límites son constantes por
   sección, `V_ef` también lo es: cada sección `[a, b)` con límite `v` rige en
   `[a, min(b + L, fin de etapa))`, y en cada punto vale el menor de los que rigen.
   Se unen tramos consecutivos de igual valor y motivo.
4. **Motivo de cada tramo de `V_ef`:** la sección que da el mínimo (a igualdad, la de
   menor índice), o `TrainMaximum` si el tope del tren es menor o igual que todas. Si
   el tramo existe solo por la regla de la cola (la sección ya terminó), el motivo
   lleva `TailClearing = true`.

#### 2.4 Perfil de velocidad (exacto, sin paso de integración)

Con aceleración `a` y frenado `b` constantes y `V_ef` constante por tramos, el
perfil se calcula en forma cerrada; no hay discretización (resuelve la Unknown 1 de
la nota). Por etapa, con tramos `j = 0…m−1` de `V_ef`, bordes `x_0 = 0 … x_m`,
velocidades en m/s:

1. Tope en cada borde: `c_0 = c_m = 0`; `c_j = min(V_{j−1}, V_j)`.
2. Ida: `f_0 = 0`; `f_j = min(c_j, √(f_{j−1}² + 2a·(x_j − x_{j−1})))`.
3. Vuelta: `u_m = 0`; `u_j = min(f_j, √(u_{j+1}² + 2b·(x_{j+1} − x_j)))`.
4. Dentro del tramo `j`: `v(s) = min(V_j, √(u_j² + 2a(s − x_j)), √(u_{j+1}² + 2b(x_{j+1} − s)))`.
   Da hasta tres fases: **aceleración** desde `u_j`, **crucero** a `V_j` si se
   alcanza, **frenado** hasta `u_{j+1}`. Si no alcanza `V_j`, aceleración y frenado se
   cruzan en `s* = x_j + (u_{j+1}² − u_j² + 2b·ℓ) / (2(a + b))`.
5. Tiempos: aceleración `(v₁ − v₀) / a`; frenado `(v₀ − v₁) / b`; crucero `ℓ / V`.
6. Después de una etapa que termina en inversión, una fase **detenido** de
   `ReversalDwellSeconds` (aunque sea 0).

#### 2.5 Fases y explicación

`RunningPhase(PhaseKind Kind, int Stage, double FromMeters, double ToMeters,
double FromSpeedKmh, double ToSpeedKmh, double StartSeconds, double EndSeconds,
PhaseReason? Reason)`.

- `PhaseKind`: `Accelerate`, `Cruise`, `Brake`, `Dwell`.
- Distancias en **coordenadas de la ruta** (0…`D`). La maniobra está fuera de la
  ruta: sus fases llevan `FromMeters = ToMeters` = punto de inversión y se reconocen
  por el motivo `ReversalManeuver`.
- `PhaseReason(SpeedLimitSource? Source, bool TrainMaximum, bool TailClearing,
  double? CurveRadiusMeters, double? CurveFromMeters, BrakeTarget? Target)`:
  - crucero: el motivo del tramo de `V_ef`;
  - frenado: `Target` = `Destination`, `Reversal` o `LowerLimit` (con el motivo del
    tramo siguiente, donde termina el frenado);
  - aceleración y detenido: sin motivo.
- Se unen fases consecutivas del mismo tipo y la misma etapa, salvo crucero con
  distinta velocidad o motivo. No hay fases de largo y duración cero, salvo la fase
  detenido con 0 s.

#### 2.6 Resultado y métricas

`RunningTimeCalculator.Calculate(RunningTimeRequest)` → `RunningTimeResult(Phases,
SpeedProfile, Reversals, Metrics)`.

- `SpeedProfile`: `SpeedProfilePoint(double DistanceMeters, double SpeedKmh,
  double TrackLimitKmh)` para dibujar. Cada fase de la ruta (no la maniobra) se
  muestrea con `⌈largo / 25 m⌉` partes iguales, extremos incluidos. `TrackLimitKmh`
  es el límite de la sección en ese punto (sin la regla de la cola ni el tope del
  tren). En una inversión hay dos puntos a la misma distancia: el de llegada y el de
  salida (0 km/h).
- `ReversalOutcome(double DistanceMeters, double ManeuverMeters, double? ManeuverTrackMeters,
  bool? ManeuverFits, double DwellSeconds)`. `ManeuverFits = ManeuverTrackMeters ≥ L`
  (`null` si no se conoce).
- `RunningTimeMetrics`:

| Métrica | Definición |
|---|---|
| `TotalSeconds` | Fin de la última fase |
| `RunningSeconds` / `DwellSeconds` | Tiempo en movimiento / detenido en inversiones |
| `RouteLengthMeters` | `D` |
| `TravelledMeters` | `D` + maniobras |
| `CommercialSpeedKmh` | `D / TotalSeconds` |
| `MaxReachedSpeedKmh` | Mayor velocidad del perfil |
| `Regimes` | Segundos y metros por régimen: acelerando, a la máxima del tren (crucero con `TrainMaximum`), limitado por la vía (crucero con otro motivo), frenando, detenido. Suman `TotalSeconds` y `TravelledMeters` |
| `CostliestCurve` | Ver abajo; `null` si no hay |

- **Curva más costosa.** Solo si hay secciones `DesignSpeed`. `V_ref` = el mayor
  límite `DesignSpeed`. Para cada sección `Curve` con límite `< min(V_ref, MaxSpeedKmh)`:
  se recalcula todo con esa curva a `V_ref`; tiempo perdido = `TotalSeconds` −
  el recalculado. Se informa la de mayor tiempo perdido (a igualdad, la de menor
  `FromMeters`), si es > 0: `(FromMeters, ToMeters, CurveRadiusMeters, SpeedLimitKmh,
  TimeLostSeconds)`.

#### 2.7 Invariantes (verificadas por tests)

1. **Velocidad:** en todo punto `v ≤ V_ef ≤ límite de la sección` y `v ≤ MaxSpeedKmh`.
2. **Paradas:** `v = 0` al inicio y al fin de cada etapa (origen, punto de detención
   de cada maniobra y destino).
3. **Cola:** después de una sección de límite `v`, la velocidad no supera `v` hasta
   que la cabeza recorrió `L` desde su fin.
4. **Aceleración y frenado:** ninguna fase acelera más que `a` ni frena más que `b`.
5. **Continuidad:** las fases son contiguas en tiempo y en distancia; la suma de
   duraciones es `TotalSeconds`.
6. **Determinismo:** misma entrada → mismo resultado bit a bit en la misma plataforma.
7. El core no hace E/S (`CoreBoundaryTests`).

#### 2.8 Vía de maniobra en la red (cambia RW-006 §2.3 y §2.5)

- `NetworkReversal` agrega `double ManeuverTrackMeters`.
- Cálculo: desde el nodo de inversión, por la tercera pata `x`, se sigue **la vía más
  derecha**: en cada nodo siguiente se toma la pata disponible con menor desviación,
  si es ≤ 45° (a igualdad, la de menor índice de arista), sumando longitudes hasta
  llegar a `MaxManeuverSearchMeters = 1.500 m` (el largo máximo de tren), a un
  extremo de vía o a un nodo sin paso directo. Se informa `min(suma, 1.500)`.
- Si hay varias terceras patas válidas, se elige la de mayor `ManeuverTrackMeters`
  (a igualdad, la de menor índice de arista). No cambia qué ruta se elige ni su costo.
- Contrato de `POST /network/routes`: cada `reversals[i]` agrega `maneuverTrackMeters`.

#### 2.9 Límite de curva redondeado hacia abajo (cambia RW-007 §3)

En la respuesta de corredores, `sections[].speedLimitKmh` se redondea **hacia abajo**
a 0,1 km/h (antes, al más cercano). El navegador devuelve esas secciones para el
cálculo (§3.2), y así el límite usado nunca supera al del core.

### 3. API

Contratos en `RailWeaver.Api/Operations/OperationsContracts.cs`. Los endpoints solo
traducen HTTP ↔ core. Redondeo solo en la respuesta: metros a 0,01, segundos a 0,1,
km/h a 0,1, m/s² a 0,01. Respuestas 200 con este formato común:

```json
{
  "train": { "name": "Pasajeros troncal", "lengthMeters": 200, "maxSpeedKmh": 120,
             "accelerationMetersPerSecondSquared": 0.25, "brakingMetersPerSecondSquared": 0.5 },
  "metrics": {
    "totalSeconds": 0, "runningSeconds": 0, "dwellSeconds": 0,
    "routeLengthMeters": 0, "travelledMeters": 0,
    "commercialSpeedKmh": 0, "maxReachedSpeedKmh": 0,
    "regimes": [{ "regime": "accelerating | train_maximum | track_limited | braking | dwell", "seconds": 0, "meters": 0 }],
    "costliestCurve": { "fromMeters": 0, "toMeters": 0, "radiusMeters": 0, "speedLimitKmh": 0, "timeLostSeconds": 0 }
  },
  "phases": [{
    "kind": "accelerate | cruise | brake | dwell", "stage": 0,
    "fromMeters": 0, "toMeters": 0, "fromSpeedKmh": 0, "toSpeedKmh": 0,
    "startSeconds": 0, "endSeconds": 0,
    "reason": { "source": "line_speed | design_speed | curve | reversal_maneuver | null",
                "trainMaximum": false, "tailClearing": false,
                "curveRadiusMeters": null, "curveFromMeters": null,
                "brakeTarget": "destination | reversal | lower_limit | null" }
  }],
  "speedProfile": [{ "distanceMeters": 0, "speedKmh": 0, "trackLimitKmh": 0 }],
  "reversals": [{ "distanceMeters": 0, "maneuverMeters": 0, "maneuverTrackMeters": 0,
                  "maneuverFits": true, "dwellSeconds": 0 }]
}
```

Tren en los pedidos: `{ name, lengthMeters, maxSpeedKmh, accelerationMetersPerSecondSquared,
brakingMetersPerSecondSquared }`. 400 con `message` si falta un dato o está fuera de rango.

#### 3.1 `GET /api/rolling-stock/presets`

Devuelve la lista de §2.1 (`id`, `name` y los cuatro datos). El navegador no
duplica los valores.

#### 3.2 `POST /api/regions/{id}/corridors/running-time`

Cuerpo: `{ "sections": [{ kind, fromMeters, toMeters, speedLimitKmh, radiusMeters }], "train": {…} }`,
con las secciones tal como las devolvió `POST /corridors`.

- 404 si la región no existe. No requiere el DEM.
- La API arma las `SpeedSection`: `tangent` → `DesignSpeed`, `curve` → `Curve`; cada
  sección empieza en el `toMeters` de la anterior (absorbe el redondeo a 0,01 m); se
  rechaza con 400 si algún hueco o solape supera 0,02 m, si están vacías o si un límite
  no es > 0.
- Sin inversiones; `ReversalDwellSeconds = 0`.

#### 3.3 `POST /api/regions/{id}/network/routes/running-time`

Cuerpo:

```json
{ "originStationId": "node/1", "destinationStationId": "node/2", "includeDisused": false,
  "lineSpeedKmh": 100, "reversalDwellMinutes": 0, "train": { } }
```

- 404 si la región no existe. 400 si faltan datos, si `lineSpeedKmh` está fuera de
  5–160 km/h, si `reversalDwellMinutes` está fuera de 0–120, o con los mismos casos
  que `POST /network/routes`.
- La API busca la ruta otra vez con `NetworkRouteFinder` (misma entrada → misma ruta,
  ≤ 1 s) y, si es `unreachable`, responde 400 "No hay ruta entre esas estaciones".
- Una sola sección `[0, D)` con `LineSpeed` = `lineSpeedKmh`; inversiones en su
  `DistanceAlongMeters` con su `ManeuverTrackMeters`.
- Se pasa `HttpContext.RequestAborted` a la búsqueda de ruta.

### 4. Frontend (según `DESIGN.md`, textos en español)

Módulo nuevo `src/web/src/operations/` (alineado con `RailWeaver.Api/Operations`,
ADR-010): cliente de API, panel de tarea, bloque de resultado y gráfico de velocidad.
Los módulos `planning` y `network` solo agregan la acción y ubican los componentes.

#### 4.1 Acción

Acción secundaria **"Tiempo de recorrido"** en el detalle del corredor (`C-NN`) y
en el de la ruta (`R-NN`). Abre el panel de tarea.

#### 4.2 Panel de tarea "Tiempo de recorrido"

A la izquierda del mapa, como los de Corredor y Ruta. En orden:

1. **Tren:** tres botones (`button-secondary`, `aria-pressed`) con los tipos de
   `GET /presets`; "Pasajeros troncal" por defecto. Debajo, cuatro campos numéricos
   con los datos del tipo elegido: "Largo (m)", "Velocidad máxima (km/h)",
   "Aceleración (m/s²)" y "Frenado de servicio (m/s²)". Si se edita un campo, ningún
   tipo queda marcado y el nombre del tren pasa a "Tren personalizado". Nota:
   "Valores de referencia, a validar."
2. Solo para rutas de la red:
   - **"Velocidad de la vía (km/h)"**, campo vacío y obligatorio (5–160). Nota: "OSM
     no informa la velocidad de estas vías: se usa este valor en toda la ruta."
   - Solo si la ruta tiene inversiones: **"Minutos detenido en cada inversión"**, 0 por
     defecto (0–120). Nota: "Cambio de cabina. Sin dato de referencia: por defecto no
     se cuenta."
3. Botón primario **"Calcular"**, habilitado con datos válidos; secundario **"Cancelar"**.
   Mientras calcula, "Calculando tiempo de recorrido…"; "Cancelar" aborta el pedido.

Validaciones en el panel con los rangos de §2.1 y §3.3, con mensaje junto al campo.

#### 4.3 Resultado

- El panel de tarea se cierra. El detalle del corredor o de la ruta agrega la sección
  **"Tiempo de recorrido · {nombre del tren}"**:
  - Métricas: tiempo total (`1 h 26 min 21 s`, redondeado al segundo), velocidad
    comercial, velocidad máxima alcanzada, distancia recorrida (si hay maniobras) y el
    desglose por régimen con tiempo y porcentaje: "Acelerando", "A la máxima del tren",
    "Limitado por la vía", "Frenando" y "Detenido en inversiones" (solo si hay).
  - Corredor: **"Curva más costosa"**: "R {radio} m en km {x} · {v} km/h · +{t} s".
  - Ruta con inversiones: "Inversiones: N · maniobra de {L} m cada una". Si alguna
    maniobra no entra, alerta `warning`:
    - **Título:** "La vía de maniobra es más corta que el tren".
    - **Por qué:** "En km {x} hay {m} m de vía para pasar el desvío y el tren mide {L} m."
    - **Consecuencia:** "El tiempo supone que la maniobra entra. En la realidad habría
      que partir el tren o elegir otro."
  - Nota: "Tiempo de marcha puro: aceleración y frenado constantes, sin efecto de la
    pendiente, sin márgenes ni paradas intermedias."
  - Acción secundaria "Quitar tiempo de recorrido".
- Un cálculo nuevo reemplaza al anterior. Quitar el corredor o la ruta quita su
  tiempo de recorrido. No se persiste.
- El mapa no cambia (decisión 5).

#### 4.4 Gráfico de velocidad (dock)

- Debajo del perfil del terreno (y de la banda de curvas, en corredores), con el
  mismo eje de distancia, título "Velocidad del tren".
- Límite de la vía (`trackLimitKmh`) como línea escalonada `ink-subtle`; velocidad
  del tren (`speedKmh`) como línea `primary`; la máxima del tren como línea
  horizontal punteada `border-subtle` rotulada en la cara de datos.
- Cada inversión, línea vertical rotulada "Invierte la marcha", como en el perfil de
  la ruta. La maniobra no se dibuja.
- Eje vertical en km/h desde 0; eje horizontal compartido con el perfil.

## Non-goals

- Efecto de la pendiente, masa, esfuerzo de tracción y resistencia al avance (V2).
- Paradas en estaciones intermedias, tiempos de parada, horarios, márgenes de regularidad.
- Velocidad en desvíos (vía desviada) y en cambios de vía.
- Curvas de la red existente y lectura de `maxspeed` de OSM.
- Compatibilidad de trocha entre tren y vía.
- Frenado de emergencia, señales y distancias de frenado de seguridad.
- Cambiar la ruta según el largo del tren (la ruta se elige como en RW-006).
- Tren animado en el mapa y línea de tiempo (RW-009).
- Persistencia de trenes o resultados; trenes guardados por el usuario.
- Tests de frontend.

## Simplificaciones

- **Aceleración y frenado constantes** en todo el rango de velocidades (V1).
- **Sin pendiente:** el tren acelera y frena igual en subida y en bajada.
- **Velocidad única en la red existente**, elegida por el usuario, también en vías en
  desuso.
- **El tren arranca con la cabeza en el origen** y el resto "detrás", aunque ahí no
  haya vía; la regla de la cola empieza en 0.
- **Maniobra al límite de la última sección**, sin verificar su vía (solo su largo,
  siguiendo la vía más derecha).
- **Curva más costosa medida de a una:** el tiempo perdido de cada curva se calcula
  sin tocar las demás.

## Inputs

- Secciones de un corredor de RW-007, o origen, destino y opción de desuso de una
  ruta de RW-006 con el dataset ferroviario de Córdoba.
- Tren, velocidad de la vía y minutos de inversión elegidos por el usuario.

## Outputs

- `RailWeaver.Core.RollingStock`: `RollingStockV1`, `RollingStockPresets`.
- `RailWeaver.Core.Operations`: `SpeedSection`, `SpeedLimitSource`, `ReversalPoint`,
  `RunningTimeRequest`, `RunningTimeCalculator`, `RunningTimeResult`, `RunningPhase`,
  `PhaseKind`, `PhaseReason`, `BrakeTarget`, `SpeedProfilePoint`, `ReversalOutcome`,
  `RunningTimeMetrics`.
- `NetworkReversal.ManeuverTrackMeters` (RW-006) y redondeo hacia abajo de
  `speedLimitKmh` (RW-007).
- Endpoints `GET /api/rolling-stock/presets`, `POST /api/regions/{id}/corridors/running-time`
  y `POST /api/regions/{id}/network/routes/running-time`.
- Módulo `operations` en el frontend: panel, resultado en el detalle y gráfico de velocidad.
- Docs: `project-status.md`, `architecture/overview.md` (namespaces RollingStock y
  Operations), `CHANGELOG.md`, README (uso), kickoff revisado y nota de investigación
  enlazando a esta spec.

## Acceptance criteria

- [x] `RollingStockV1` y `RollingStockPresets` según §2.1.
- [x] `RunningTimeCalculator` implementa §2.2–2.6 tal como están escritos.
- [x] Tests sintéticos del core (§Tests) en verde, incluidas las invariantes de §2.7.
- [x] `ManeuverTrackMeters` según §2.8 y redondeo de §2.9, con tests; los casos de
      RW-006 siguen dando las mismas rutas.
- [x] Con el dataset real (corre en CI, sin DEM), tests de API verifican con ±0,5 %:
      Córdoba → Villa María y Córdoba → Río Cuarto con "Pasajeros troncal" a
      100 km/h según las fórmulas de "Datos medidos" (con las longitudes reales de las
      rutas). Se registra en esta spec el `maneuverTrackMeters` de la inversión cerca
      de Villa María y el de Córdoba → Alta Córdoba.
- [x] Con el DEM real, verificación manual registrada: un corredor en llanura (Villa
      María → Río Cuarto, 10 ‰, ancha, 120 km/h) y uno serrano (Córdoba → Cosquín, 35 ‰,
      métrica, 80 km/h), con los tres trenes: tiempo total, desglose, curva más
      costosa y tiempo de respuesta.
- [x] Tiempo: cada cálculo responde en ≤ 1 s sin contar la búsqueda de ruta, y
      ≤ 2 s en total para las rutas de la tabla de RW-006.
- [x] Endpoints según §3: 200, 400 y 404, con tests de API.
- [x] En el visor: acción "Tiempo de recorrido" en corredor y ruta, panel con tipos
      editables, velocidad de la vía y minutos de inversión, cálculo cancelable,
      sección de resultado con desglose, curva más costosa y alerta de maniobra,
      gráfico de velocidad debajo del perfil. Todo con tokens de `DESIGN.md`. El autor
      confirma el funcionamiento; los agentes no hacen verificación visual y entregan
      una lista específica de puntos a revisar.
- [x] `CoreBoundaryTests` en verde. `dotnet test`, `npm --prefix src/web run lint`,
      `format:check` y `build` pasan localmente y en CI sin el DEM.
- [x] `project-status.md`, `architecture/overview.md`, `CHANGELOG.md` y README
      actualizados; kickoff revisado; tag `v0.8.0`.

## Relevant domain docs

- [train-v1-and-running-time-calculation.md](../../research/rolling-stock/train-v1-and-running-time-calculation.md)
- [railway-curves-and-horizontal-alignment.md](../../research/infrastructure/railway-curves-and-horizontal-alignment.md) (velocidad en curva)
- [railway-topology-and-turnouts.md](../../research/infrastructure/railway-topology-and-turnouts.md) (inversiones)
- Kickoff §7 (Rolling stock V1), §38–39 (M1, V0.8).

## Relevant architecture

- [Overview](../../architecture/overview.md): el core calcula; la API traduce HTTP y
  busca la ruta; el navegador solo dibuja y devuelve las secciones del corredor sin
  modificarlas.
- `RollingStock` y `Operations` como namespaces dentro de `RailWeaver.Core`, sin
  proyecto nuevo.
- [ADR-006](../../decisions/ADR-006-discrete-event-simulation.md): determinismo; RW-009
  usará este perfil.
- [ADR-010](../../decisions/ADR-010-frontend-structure-and-formatting.md): módulo `operations`.
- [DESIGN.md](../../../DESIGN.md) › Layout (panel de tarea, dock), Components (botones,
  campos, alertas, métricas) y Charts.

## Tests

- **Core, con secciones sintéticas:**
  - Una sección, tren que alcanza su máxima: tiempo = `D/V + V/(2a) + V/(2b)` exacto
    (±1e-9 relativo); fases acelerar / crucero / frenar con sus distancias.
  - Ruta corta que no alcanza la máxima: dos fases que se cruzan en `s*` y pico correcto.
  - Curva lenta en el medio: frena antes de la curva hasta su límite, cruza a ese
    límite y no acelera hasta `L` metros después (regla de la cola, con
    `TailClearing`).
  - Curva más corta que el tren y dos curvas separadas por menos que `L`.
  - Tope del tren menor que todos los límites: crucero con `TrainMaximum`.
  - Inversión: dos etapas, maniobra de largo `L`, fase detenido con los segundos
    pedidos, `ManeuverFits` verdadero, falso y `null`.
  - Curva más costosa: con dos curvas de distinto costo elige la mayor; sin secciones
    `DesignSpeed` es `null`.
  - Métricas: regímenes que suman el total; velocidad comercial.
  - Invariantes de §2.7 en todos los casos; determinismo; validaciones del tren y de
    las secciones.
- **Red:** `ManeuverTrackMeters` en un desvío sintético con vía de maniobra larga,
  corta, con continuación por otro nodo y con dos terceras patas.
- **API:** los tres endpoints: 200, 400 (tren, secciones con huecos, velocidad de la
  vía, minutos, ruta inexistente) y 404; casos reales de Acceptance criteria.
- **Frontera del core:** `CoreBoundaryTests` sin cambios.

## Decisiones resueltas antes de implementar

1. **El tren recorre corredores y rutas de la red** (decidido con el usuario). RW-010
   solo tendrá que unir los dos.
2. **La velocidad de la red existente la elige el usuario, una para toda la ruta**
   (decidido con el usuario). OSM la informa en el 2,5 % de las vías; no se inventan
   valores por trocha.
3. **Tres tipos de tren editables** (decidido con el usuario), con los valores más
   bajos de cada rango de la nota.
4. **Inversiones con el largo del tren** (decidido con el usuario): maniobra igual al
   largo del tren, y aviso si la vía de maniobra es más corta. Cierra la
   simplificación de RW-006.
5. **Viaje directo, minutos de inversión editables con 0 por defecto, gráfico debajo
   del perfil y sin cambios en el mapa** (decidido con el usuario).
6. **El tren no tiene trocha en V1:** sus cuatro datos no dependen de ella. Cuidar la
   compatibilidad tren–vía llega cuando haya flota o servicios.
7. **Cálculo exacto por tramos en lugar de paso de integración:** con aceleración y
   frenado constantes y límites constantes por tramo, las fórmulas son cerradas.
   Elimina el error de paso y la Unknown 1 de la nota.
8. **Stateless:** la ruta se vuelve a buscar (≤ 1 s, determinista) y el corredor
   viaja como secciones desde el navegador, en lugar de guardar corredores en la
   API. Buscar el corredor otra vez tardaría segundos o minutos.
9. **Diferencias con la nota de investigación:**
    - `RollingStockV1` sin `Id` ni `GaugeMillimetres` (los tipos tienen id solo en la
      lista de presets);
    - el cálculo va en `Operations`, no en `RollingStock`;
    - la velocidad en la red no usa valores por trocha ni `maxspeed` (decisión 2);
    - la inversión suma la maniobra, no solo reinicia la velocidad;
    - la curva más costosa se define por recálculo (la nota no la define).
10. **Determinismo por plataforma**, como en RW-005 (decisión 9).

## Open questions

Ninguna. Toda decisión nueva durante la implementación se registra acá antes de implementarla.
