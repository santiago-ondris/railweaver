# Material rodante V1 y cálculo de tiempo de recorrido (Running Time Calculation)

## Question

¿Cuáles son las formulaciones físicas, algoritmos deterministas y parámetros normativos estándar (UIC 451-1 / EN 14531 / RITO) para calcular el perfil de velocidad y el tiempo de marcha de un tren con material rodante V1 (longitud, velocidad máxima, aceleración y frenado constantes; Kickoff §7) sobre una traza ferroviaria con restricciones de velocidad por geometría de curva (RW-007) y estaciones, de manera explicable y libre de heurísticas estocásticas?

## Sources

- **Union Internationale des Chemins de fer (UIC)**:
  - *UIC Leaflet 451-1: Timetable compilation - Running time calculation principles*. Metodología canónica para la determinación de tiempos de marcha base, márgenes de regularidad y cálculo de perfiles de velocidad.
  - *UIC Leaflet 505-1 / 544-1: Railway applications - Braking*. Parámetros de frenado continuo y distancias de desaceleración según porcentaje de masa frenada.
- **Comité Europeo de Normalización (CEN)**:
  - *EN 14531: Railway applications - Methods for calculation of stopping and slowing distances and deceleration*. Ecuaciones cinemáticas y dinámicas para curvas de desaceleración y frenado de servicio.
- **Reglamento Interno Técnico Operativo (RITO)** y disposiciones de la **Comisión Nacional de Regulación del Transporte (CNRT)** / Ferrocarriles Argentinos:
  - Prescripciones sobre distancias de frenado de servicio, velocidades máximas por tipo de material (carga, pasajeros interurbanos, automotores diésel) y consideraciones de longitud de tren en pasos a nivel y cambios de vía.
- **Hansen, I. A. & Pachl, J.** (2014), *Railway Timetable & Traffic: Analysis - Modelling - Simulation*, Eurailpress. Capítulo 2: *Running Time Estimation and Traction Dynamics*.
- **Pachl, J.** (2020), *Railway Operation and Control*, 4th Edition, VTD Rail Publishing. Modelos de aceleración, frenado, envolventes de velocidad (*Speed Profiles*) y paso del tren como cuerpo rígido extenso.
- **López Pita, A.** (2006), *Infraestructuras Ferroviarias*, Edicions UPC; y **Oliveros Rives, F. et al.** (1980), *Tratado de Ferrocarriles*. Cinemática del tren y resistencia al avance.
- **Fichas técnicas y manuales de material rodante en Argentina**:
  - Trenes Argentinos Operaciones (SOFSE) / Línea Mitre: Locomotoras diésel-eléctricas CSR Qishuyan CKD8G/8H y coches de pasajeros CNR.
  - Tren de las Sierras (Ramal A1, trocha métrica): Coche motor Materfer CML / duplas Alna DMU.
  - Trenes Argentinos Cargas (Líneas Belgrano y Mitre): Locomotoras EMD GT22CW / CRRC CDD5A1 y formaciones de tolvas graneleras de 40 a 50 vagones.
- **RailWeaver Project Kickoff** (§7 *Niveles de fidelidad: Rolling stock V1 vs V2*, §15 *Route / Tramo Generator*, §38–39 *Milestone M1 — Primer tren: V0.8 Train and running time*).
- **RailWeaver Backlog** (`docs/specs/backlog/RW-008-train-running-time.md`).

---

## Real-world behavior

### 1. Dinámica del tren en marcha

El movimiento longitudinal de un tren a lo largo de una vía férrea se rige por la segunda ley de Newton:

$$M_e \cdot \frac{dv}{dt} = F_t(v) - R_t(v, s) - B(v)$$

donde:
- $M_e = M \cdot (1 + \rho)$ es la masa inercial efectiva del tren, incorporando la inercia rotacional de los ejes, ruedas y motores ($\rho \approx 0{,}06\text{ a }0{,}15$).
- $F_t(v)$ es el esfuerzo de tracción entregado por las locomotoras o unidades motoras en el contacto rueda-carril, limitado por la adherencia a baja velocidad y por la potencia máxima del motor a alta velocidad ($P = F_t \cdot v$).
- $R_t(v, s)$ es la resistencia total al avance, compuesta por:
  1. Resistencia básica al rodamiento y aerodinámica (fórmula de Davis: $R_0 = A + B \cdot v + C \cdot v^2$).
  2. Resistencia por pendiente topográfica ($R_g = M \cdot g \cdot s$).
  3. Resistencia adicional por curvas de radio $R$ ($R_c = M \cdot g \cdot \frac{C}{R}$, analizada en RW-007).
- $B(v)$ es la fuerza de frenado aplicada por zapatas o discos mediante el sistema neumático continuo.

### 2. El tren como cuerpo continuo extenso (*Extended Body*)

Un tren real no es una masa puntual adimensional. Tiene una longitud física considerable ($L$, desde $\sim 40\text{ m}$ en una dupla ligera hasta más de $600\text{ m}$ en un tren de cargas):

```text
Sentido de marcha: ------>
                        ┌──────────────────────────────────────────────┐
                        │              TREN (Longitud L)               │
       COLA DEL TREN    │                                              │ CABEZA DEL TREN
            ▼           └──────────────────────────────────────────────┘       ▼
────────────────────────|══════════════════════════════════════════════|────────────────────────
                       s_in                                           s_out
                        ▲                                              ▲
                        └── Tramo de curva lenta o restricción local ──┘
```

Esta dimensión física introduce una asimetría cinemática crítica entre la aceleración y el frenado:
1. **Frenado (cabeza del tren manda):** Para entrar a una curva lenta, una estación o un desvío con límite $V_{\text{tramo}}$, la **cabeza del tren** (locomotora) debe haber reducido su velocidad a $V_{\text{tramo}}$ antes de tocar el punto de inicio de la restricción ($s_{\text{in}}$).
2. **Aceleración (cola del tren manda):** Al salir de una curva lenta hacia un tramo de mayor velocidad (por ejemplo, saliendo de una curva de $40\text{ km/h}$ a una recta de $100\text{ km/h}$), el tren **NO puede acelerar** a la velocidad superior hasta que el **último vagón** (la cola) haya abandonado por completo la curva ($s_{\text{out}} + L$). Si el maquinista acelerara en cuanto la locomotora sale de la curva, la cola del tren sufriría un tirón a exceso de velocidad en plena curva, generando riesgo de descarrilamiento por vuelco o rotura de enganches.

### 3. Fases de una marcha ferroviaria

En la operación ferroviaria ideal entre dos paradas, la marcha se descompone en cuatro regímenes principales:
1. **Aceleración:** Partiendo desde el reposo o desde una restricción previa, el tren acelera hasta alcanzar la velocidad de régimen permitida.
2. **Marcha a velocidad crucero / sostenida:** El tren mantiene la velocidad máxima autorizada (sea el límite mecánico del material rodante o el límite impuesto por la geometría de vía).
3. **Marcha restringida por infraestructura / curvas:** Tramos donde el tren debe mantener una velocidad reducida para no exceder la aceleración centrífuga admisible en arcos circulares (calculada con las fórmulas de RW-007).
4. **Frenado de servicio:** Desaceleración progresiva y controlada para adaptarse a una curva más cerrada o para detenerse exactamente en la estación de destino.

### 4. Valores de referencia de material rodante en Argentina

Para calibrar y verificar las simulaciones de RailWeaver sobre el territorio de Córdoba y líneas conexas, se identifican tres tipos de trenes representativos:

| Parámetro | Pasajeros Interurbano Troncal (ej. Mitre Córdoba-Retiro) | Automotor Liviano Serrano (ej. Tren de las Sierras Ramal A1) | Carga Pesado Granelero (ej. Belgrano Cargas / NCA) |
|---|---|---|---|
| **Composición típica** | Locomotora CSR CKD8G + 6 a 8 coches CNR | Dupla Alna DMU o Coche Motor Materfer | Locomotora GT22CW o CDD5A1 + 40 tolvas |
| **Trocha** | Ancha ($1.676\text{ mm}$) | Métrica ($1.000\text{ mm}$) | Métrica ($1.000\text{ mm}$) o Ancha ($1.676\text{ mm}$) |
| **Longitud total ($L$)** | $\approx 200\text{ m}$ | $\approx 42\text{ m}$ | $\approx 550\text{ m}$ |
| **Velocidad máxima técnica ($V_{\max}$)** | $120\text{ km/h}$ | $80\text{ km/h}$ | $60\text{ km/h}$ |
| **Aceleración de servicio ($a$)** | $0{,}25\text{ a }0{,}35\text{ m/s}^2$ | $0{,}50\text{ a }0{,}65\text{ m/s}^2$ | $0{,}06\text{ a }0{,}10\text{ m/s}^2$ |
| **Frenado de servicio ($b$)** | $0{,}50\text{ a }0{,}65\text{ m/s}^2$ | $0{,}70\text{ a }0{,}85\text{ m/s}^2$ | $0{,}25\text{ a }0{,}35\text{ m/s}^2$ |
| **Frenado de emergencia** | $\approx 0{,}90\text{ m/s}^2$ | $\approx 1{,}10\text{ m/s}^2$ | $\approx 0{,}50\text{ m/s}^2$ |
| **Comportamiento operativo** | Requiere $\sim 2\text{ km}$ para acelerar de 0 a 100 km/h; frena suavemente por confort de pasajeros. | Muy ágil en paradas serranas frecuentes; alta aceleración por tracción distribuida y baja inercia. | Extremadamente inercial; tarda $> 4\text{ km}$ en alcanzar 60 km/h y distancias de frenado de $> 800\text{ m}$. |

---

## RailWeaver abstraction

### 1. El modelo de Material Rodante V1 (Kickoff §7)

De acuerdo con el principio de fidelidad progresiva del proyecto (Kickoff §7), el material rodante en **V1** se modela mediante cinemática determinista parametrizada por cuatro variables cardinales:

```csharp
namespace RailWeaver.Core.RollingStock;

public sealed record RollingStockV1(
    string Id,
    string Name,
    int GaugeMillimetres,
    double LengthMeters,
    double MaxSpeedKmh,
    double AccelerationMpss, // m/s² (aceleración media de servicio)
    double BrakingRateMpss   // m/s² (desaceleración de servicio en valor positivo)
);
```

Este modelo no requiere en V0.8 calcular la masa en toneladas, el esfuerzo tractor dinámico $F_t(v)$ en kN, ni las curvas hiperbólicas de potencia del motor diésel (reservadas para V2 y V3). La abstracción V1 asume una **aceleración media efectiva de servicio constante** y un **frenado de servicio constante**, lo que permite resolver las ecuaciones de movimiento de forma cerrada, exacta y determinista.

### 2. Algoritmo de tres pasadas para el Perfil de Velocidad (Running Time Calculation - RTC)

Para cualquier camino o corredor discretizado en tramos consecutivos de longitud $\Delta s$, el cálculo del perfil de marcha óptimo y seguro se resuelve mediante el **algoritmo clásico de tres pasadas (Three-Pass RTC Algorithm)**:

```mermaid
flowchart TD
    subgraph Paso 1: Perfil de Vía
        A1["Trazado (Rectas y Curvas)"] --> A2["Límites de velocidad por curva (RW-007)<br/>y velocidad máxima del trazado"]
        A2 --> A3["Expansión por longitud del tren L<br/>La cola no acelera hasta salir de la curva"]
    end

    subgraph Paso 2: Pase hacia adelante (Forward Pass)
        B1["v[0] = 0 en el origen"] --> B2["Aceleración continua: v[i+1] = sqrt(v[i]² + 2·a·Δs)"]
        B2 --> B3["Acotado por min(v_acel, V_tren_max, V_via)"]
    end

    subgraph Paso 3: Pase hacia atrás (Backward Pass)
        C1["v[N] = 0 en el destino"] --> C2["Frenado continuo: v[i] = sqrt(v[i+1]² + 2·b·Δs)"]
        C2 --> C3["Curva envolvente: v_final[i] = min(v_forward[i], v_backward[i])"]
    end

    A3 --> B2
    B3 --> C2
    C3 --> D["Integración de Tiempo t[i] = Δs / v_medio<br/>Métricas de viaje y desglose"]
```

#### Paso 1: Perfil de velocidad máxima permitida por la vía ($V_{\text{track}}(s)$)
Dado un camino de longitud total $D$ compuesto por tramos $k$, cada tramo tiene una velocidad máxima admisible $V_{\text{geom}, k}$ (derivada de la geometría de curva en RW-007 o de la velocidad de diseño en recta):
- Para cada punto del eje longitudinal $s \in [0, D]$, la velocidad máxima admisible para la cabeza del tren en esa posición debe garantizar que **ningún punto del tren** (desde la cabeza $s$ hasta la cola $s - L$) viole la velocidad del tramo donde se encuentra:
  $$V_{\text{track}}(s) = \min \{ V_{\text{geom}}(u) \mid \max(0, s - L) \le u \le s \}$$
- Esto modela rigurosamente el retraso en la aceleración tras una curva: hasta que la cola no sale del tramo lento, $V_{\text{track}}(s)$ permanece en el valor restrictivo.

#### Paso 2: Pase hacia adelante (Forward Acceleration Pass)
Se barre el trazado desde el origen $s = 0$ hasta el destino $s = D$ con paso de integración $\Delta s$:
- Condición inicial: $v_{\text{fwd}}(0) = 0$ (partida desde el reposo en estación).
- Para cada punto $i \to i+1$:
  $$v_{\text{potencial}} = \sqrt{v_{\text{fwd}}(s_i)^2 + 2 \cdot a \cdot \Delta s}$$
  $$v_{\text{fwd}}(s_{i+1}) = \min \left( v_{\text{potencial}}, \; V_{\text{train\_max}}, \; V_{\text{track}}(s_{i+1}) \right)$$

#### Paso 3: Pase hacia atrás (Backward Braking Pass)
Garantiza que el tren anticipe todas las frenadas necesarias para no llegar a exceso de velocidad a ninguna curva cerrada ni a la estación final:
- Condición final: $v_{\text{bwd}}(s_N) = 0$ (detención completa en destino).
- Se barre hacia atrás desde $s = D$ ($i = N$) hasta $s = 0$ ($i = 0$):
  $$v_{\text{freno}} = \sqrt{v_{\text{bwd}}(s_{i+1})^2 + 2 \cdot b \cdot \Delta s}$$
  $$v(s_i) = \min \left( v_{\text{fwd}}(s_i), \; v_{\text{freno}} \right)$$

#### Paso 4: Integración del tiempo de recorrido
Una vez obtenida la velocidad efectiva $v(s_i)$ en cada estación de muestreo:
- Para cada intervalo entre $s_i$ y $s_{i+1}$:
  - Si la velocidad varía linealmente bajo aceleración/frenado constante:
    $$\Delta t_i = \frac{2 \cdot \Delta s}{v(s_i) + v(s_{i+1})}$$
  - Si ambas velocidades son idénticas ($v(s_i) = v(s_{i+1}) = v_0 > 0$):
    $$\Delta t_i = \frac{\Delta s}{v_0}$$
- El tiempo total de recorrido es la suma exacta de los intervalos:
  $$T_{\text{total}} = \sum_{i=0}^{N-1} \Delta t_i$$

### 3. Explicabilidad del viaje: Métricas y desglose de régimen

En concordancia con el principio de explicabilidad de RailWeaver (Kickoff §14 y §38: *"con un tiempo de viaje que se pueda explicar"*), el resultado de la corrida no entrega un simple número de minutos, sino un desglose inteligible:

1. **Tiempo total de viaje ($T$) y velocidad comercial ($V_{\text{com}} = D / T$):** Permite comparar la velocidad media efectiva contra el transporte automotor.
2. **Desglose de tiempo y distancia por régimen:**
   - **Tiempo acelerando ($T_{\text{accel}}$):** Tiempo transcurrido con $a > 0$.
   - **Tiempo a velocidad máxima del tren ($T_{\text{train\_max}}$):** Tiempo circulando a la velocidad tope del vehículo.
   - **Tiempo limitado por curvas / infraestructura ($T_{\text{track\_limited}}$):** Tiempo donde el tren debió circular más lento que su capacidad por el radio de las curvas de la traza.
   - **Tiempo frenando ($T_{\text{braking}}$):** Tiempo empleado en desaceleraciones controladas.
3. **Identificación de cuellos de botella:** Reporte del tramo o curva que mayor penalización de tiempo introdujo en el viaje.

### 4. Trade-offs sobre las preguntas abiertas

#### Pregunta A: ¿Cómo estimar velocidades y tiempos sobre las rutas de la red existente de OpenStreetMap (RW-006)?
- **Situación:** En RW-007 se decidió conscientemente no calcular curvas en las 3.953 aristas de OSM para no introducir ruido de digitalización.
- **Opciones:**
  - *Opción 1 (Velocidad máxima reglamentaria por trocha):* Asignar a la red OSM una velocidad nominal de vía por defecto según trocha y tipo de vía (ej. trocha ancha: $80\text{ km/h}$; trocha métrica llanura: $50\text{ km/h}$; trocha métrica serrana Ramal A1: $35\text{ km/h}$), o leer el tag `maxspeed` de OSM si existe.
  - *Opción 2 (Estimador de curvatura discreta sobre OSM):* Intentar medir radios sobre las polilíneas de OSM.
- **Evaluación y recomendación:** La **Opción 1 es la recomendada para V0.8**. Mantiene el sistema determinista, predecible y consistente con RW-007. El cálculo cinemático de aceleración y frenado del tren V1 funcionará exactamente igual sobre rutas OSM (acelerando al salir de una estación y frenando al llegar a la siguiente), mientras que sobre corredores nuevos de RW-007 aplicará adicionalmente los frenados por curva.

#### Pregunta B: ¿El gráfico velocidad-distancia comparte componente con el perfil longitudinal en el frontend?
- **Opciones:**
  - *Opción 1 (Componente compartido con pestañas/toggle en `ProfileDock`):* Reutilizar el panel inferior desplegable existente, permitiendo al usuario alternar entre ver el "Perfil de Elevación" (cota del terreno y rasante) y el "Perfil de Velocidad" (límites de vía vs velocidad real del tren), o superponer la curva de velocidad en un eje secundario derecho.
  - *Opción 2 (Ventana flotante / modal independiente):* Crear un visor de telemetría aparte.
- **Evaluación y recomendación:** La **Opción 1 es la más coherente con `DESIGN.md` y la experiencia de usuario**. Mantiene la coordinación con la posición del cursor sobre el mapa Cesium y no satura la pantalla con paneles flotantes superpuestos.

---

## Safety invariants

1. **Invariante de velocidad no excedida:** En ningún punto del recorrido la velocidad simulada del tren puede superar la velocidad máxima permitida por la vía ($v(s) \le V_{\text{track}}(s)$) ni la velocidad máxima técnica del material rodante ($v(s) \le V_{\text{train\_max}}$).
2. **Invariante de parada segura:** En los extremos del recorrido (estación de origen y estación de destino) la velocidad simulada debe ser idénticamente cero ($v(0) = 0$ y $v(D) = 0$).
3. **Invariante de cola de tren:** Durante una transición de una curva lenta a una sección rápida, la velocidad no puede comenzar a superar el límite de la curva lenta hasta que la distancia recorrida desde el fin de la curva sea estrictamente mayor o igual a la longitud del tren $L$.
4. **Determinismo absoluto:** Dadas las mismas características de material rodante y la misma traza de vía, el cálculo cinemático debe arrojar tiempos y perfiles idénticos al milisegundo, sin diferencias entre ejecuciones ni fluctuaciones de punto flotante.

---

## Simplifications

1. **Aceleración y frenado constantes (Fidelidad V1):** No se calcula el esfuerzo tractor dinámico $F_t(v)$ dependiente de la curva de potencia de la locomotora ni la fricción rueda-carril húmedo. La aceleración $a$ y el frenado $b$ se asumen uniformes a lo largo de toda la gama de velocidades.
2. **Efecto de la pendiente diferido a V2:** El efecto de las rampas ascendentes y pendientes descendentes sobre la aceleración del tren se documenta como simplificación deliberada de V1. En V1 la pendiente ya condiciona la velocidad admisible de la vía (a través del radio de curva compensado de RW-007), pero la masa del tren y el gradiente directo se incorporarán en el nivel V2 (Kickoff §7) cuando se modelen toneladas y tracción.
3. **Sin márgenes de colchón de horario (*recovery time margins*):** El cálculo entrega el tiempo físico puro de marcha (*pure running time*). No se añaden suplementos de regularidad comercial (habitualmente $3\% - 7\%$ en trenes reales) a menos que el usuario lo solicite expresamente en la configuración.

---

## Unknowns

1. **Resolución óptima del paso de integración ($\Delta s$):** Un paso de integración demasiado grande (ej. $100\text{ m}$) puede sobreestimar la velocidad de entrada a curvas cortas; un paso demasiado pequeño (ej. $0{,}1\text{ m}$) incrementaría el tiempo de cómputo en corredores de $200\text{ km}$. Se calibrará un paso nominal óptimo (ej. entre $5\text{ m}$ y $20\text{ m}$) que balancee precisión subsegundo y rendimiento en milisegundos.
2. **Manejo de inversiones de marcha intermedias:** Si una ruta entre estaciones en la red existente de RW-006 incluye una o más inversiones de marcha (cambio de sentido en estación intermedia), el viaje debe descomponerse en etapas independientes, reiniciando la velocidad a cero en cada inversión.

---

## Confidence

La implementación de V1 está definida por [RW-008](../../specs/completed/RW-008-train-running-time.md),
que resuelve las diferencias y preguntas abiertas de esta nota (fórmulas cerradas,
maniobras y velocidad única elegida por el usuario para la red existente).

**HIGH.**
- El algoritmo de tres pasadas (Forward/Backward integration) es el estándar universal en toda la literatura de simulación ferroviaria (Pachl, Hansen, UIC 451-1, OpenTrack, RailSys).
- Las ecuaciones cinemáticas con aceleración y desaceleración constantes son cerradas y tienen solución analítica exacta, eliminando cualquier riesgo de inestabilidad numérica.
- Los parámetros de material rodante seleccionados provienen directamente de las formaciones que operan hoy en la provincia de Córdoba y la red troncal nacional argentina.
