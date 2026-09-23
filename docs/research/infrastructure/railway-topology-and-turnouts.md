# Topología ferroviaria, aparatos de vía y grafo de red

## Question

¿Cómo se transforma una colección de polilíneas lineales sueltas extraídas de OpenStreetMap (2.374 tramos en la provincia de Córdoba) en un grafo topológico de red ferroviaria navegable, determinista y físicamente veraz para trenes en RailWeaver (M1 / V0.6 — La red como grafo)?

Específicamente:
1. ¿Cómo se representan y validan las conexiones reales entre tramos (nodos compartidos, bifurcaciones y empalmes en T)?
2. ¿Cómo se modelan físicamente los aparatos de vía (agujas, desvíos y cruzamientos a nivel) y sus restricciones direccionales de circulación (*de punta*, *de talón*, prohibición de giros cerrados e imposibilidad de transferencias en cruzamientos a nivel)?
3. ¿Cómo se garantiza la estricta continuidad por trocha (métrica 1000 mm vs ancha 1676 mm) y el tratamiento de tramos bitrocha?
4. ¿Cómo se asocian las estaciones (nodos geográficos) a las vías del grafo?
5. ¿Cómo se detectan, clasifican y reportan las discontinuidades y huecos en los datos de OSM sin inventar infraestructura?

---

## Sources

- **Ferrocarriles Argentinos**: *Reglamento Interno Técnico Operativo* (RITO).
  - Título II: Vía y Obras. Artículos referidos a aparatos de vía (ADV), definiciones de agujas, contraagujas, cruzamientos (ranas/corazones), cerrojos y cambios de talón y de punta.
  - Título III: Señales y circulación de trenes en estaciones y empalmes.
- **Asociación Latinoamericana de Ferrocarriles (ALAF)** / **Comisión Nacional de Regulación del Transporte (CNRT)**:
  - Normas técnicas sobre trochas ferroviarias y gálibos en la República Argentina (Trocha ancha 1.676 mm, media 1.435 mm, angosta/métrica 1.000 mm).
- **International Union of Railways (UIC)**:
  - *UIC Leaflet 711*: *Geometric characteristics of points and crossings*.
  - *UIC Leaflet 710*: *Minimum curve radii and geometric track alignment standards*.
- **railML.org**:
  - *railML 3.x Infrastructure Scheme*: Especificación formal de topología micro (nodos, aristas orientadas, puertos de conexión, desvíos) y macro (rutas operativas).
- **EULYNX**:
  - *Data Model and Interlocking Architecture*: Modelado formal de elementos de campo y subsistema de cambios de aguja (*Points*).
- **OpenRailwayMap / OpenStreetMap Wiki**:
  - [OpenRailwayMap Tagging](https://wiki.openstreetmap.org/wiki/OpenRailwayMap/Tagging) y [Tag:railway=switch](https://wiki.openstreetmap.org/wiki/Tag:railway=switch).
  - Convenciones sobre conectividad por nodos coincidentes, nodos de desvío y cruzamientos a nivel (`railway=railway_crossing`).
- **Dataset ferroviario de Córdoba en RailWeaver**:
  - `data/regions/cordoba/tracks.geojson` (2.374 tramos normalizados).
  - `data/regions/cordoba/railway.overpass.json` (22.560 nodos y 2.335 polilíneas crudas de Overpass).

---

## Real-world behavior

### 1. La naturaleza geométrica y cinemática de la vía férrea

A diferencia del tráfico automotor o peatonal urbano —donde en una esquina un vehículo puede girar 90°, doblar a la izquierda, a la derecha o dar una vuelta en "U"—, **el ferrocarril es un sistema con guiado unidimensional estricto**.
- Las pestañas de las ruedas de acero van confinadas entre los rieles.
- Los vehículos ferroviarios poseen rigidez entre ejes y bogies, lo que impone **radios de curvatura mínimos** para cualquier cambio de alineación: típicamente $R \ge 150\text{--}200\text{ m}$ en vías principales métricas y $R \ge 250\text{--}300\text{ m}$ en trocha ancha; en desvíos de baja velocidad ($15\text{--}30\text{ km/h}$) el radio puede bajar a $R \ge 100\text{--}150\text{ m}$, pero nunca admitir una discontinuidad angular brusca.
- Por tanto, un grafo ferroviario **no puede ser un grafo no dirigido convencional** donde cualquier arista incidente a un nodo puede conectarse libremente con cualquier otra arista incidente.

### 2. Aparatos de vía (ADV)

El aparato de vía es el mecanismo físico que permite bifurcar, unir o cruzar vías férreas.

#### A. Cambios de agujas y desvíos simples (Turnouts)
Un desvío simple posee:
- Una **pata común o troncal** (hacia la contraaguja).
- Una **vía directa** (alineación tangente o normal).
- Una **vía desviada** (alineación en curva hacia el ramal secundario).

**Sentidos de circulación reglamentarios:**
1. **Circulación de punta (*facing point*):** El tren avanza desde la pata común hacia las agujas. La posición mecánica de las agujas determina si el tren continúa por la vía directa o toma la vía desviada. Ambas transiciones son cinemáticamente válidas.
2. **Circulación de talón (*trailing point*):** El tren avanza desde la vía directa hacia la pata común, o desde la vía desviada hacia la pata común. Ambas convergen en la vía común.
3. **Movimiento prohibido:** Un tren no puede entrar por la vía desviada y salir por la vía directa sin detenerse, retroceder e invertir la marcha (*reversal*). El ángulo entre ambas ramas en el corazón del desvío es agudo ($\approx 4^\circ\text{ a }15^\circ$, raramente más de $25^\circ$), lo que causaría un descarrilamiento instantáneo.

```text
                     /-------- Vía desviada (Diverging)
                   /
 Troncal =========[ Aguja ]=== Vía directa (Straight)
  (Facing)
```

#### B. Cruzamientos a nivel planos (Diamond crossings / Flat crossings)
Dos vías se cruzan sobre el mismo plano horizontal en forma de X o rombo sin aparatos de aguja móviles.
- Cada tren continúa estrictamente por su propia alineación.
- **No existe transferencia mecánica entre una vía y la otra.** Si la vía A cruza a la vía B con un ángulo de $45^\circ$ o $90^\circ$, un tren en la vía A nunca puede doblar hacia la vía B en ese punto.

#### C. Travesías con desvío (Slip switches)
Son cruzamientos a nivel modificados con agujas adicionales en los laterales:
- **Travesía simple:** permite cruzar directo por ambas vías, o desviar de una vía a la otra en un único cuadrante.
- **Travesía doble:** permite cruzar directo o desviar en cualquiera de los cuadrantes.

#### D. Cruces a distinto nivel (Puentes y viaductos)
Dos vías se cruzan en coordenadas cartográficas $(x, y)$, pero con diferente cota vertical (cota rasante separada por un puente o túnel). En el mundo real no existe ninguna conexión física.

### 3. El régimen de trochas y tramos bitrocha en Córdoba

- **Incompatibilidad absoluta de trocha:** Un bogie de trocha métrica ($1.000\text{ mm}$) no puede rodar sobre rieles de trocha ancha ($1.676\text{ mm}$) ni viceversa. La red de trocha métrica (Línea Belgrano) y la de trocha ancha (Líneas Mitre y San Martín) son dos redes físicamente desconectadas en el espacio operativo de trenes continuos.
- **Tramos bitrocha:** Tramos singulares equipados con 3 o 4 rieles que permiten el paso alternativo de trenes de ambas trochas (como el tramo histórico entre Alta Córdoba y Córdoba Mitre). En estos tramos, la vía ofrece servicio a ambas trochas, pero las agujas bitrocha deben separar las circulaciones hacia las playas de su respectiva trocha.

### 4. La realidad del dataset de OpenStreetMap en Córdoba

El análisis exhaustivo del dataset extraído para Córdoba revela particularidades topológicas críticas:
1. **Conexiones en T (T-junctions):** En OSM, 1.384 bifurcaciones y desvíos no están mapeados dividiendo la vía principal en dos tramos; en su lugar, la vía secundaria finaliza en un nodo intermedio de la vía principal. Si los tramos se consideran únicamente entre sus extremos geográficos inicial y final, la red quedaría desmembrada en más de mil pedazos aislados.
2. **Nodos compartidos:** De 22.449 nodos de vías en Córdoba:
   - $19.701$ nodos pertenecen a un único tramo (nodos de trazado interno o toperas).
   - $2.632$ nodos unen exactamente 2 tramos (continuidad lineal o cambio de atributo).
   - $1.473$ nodos unen 3 ramas (desvíos estándar).
   - $112$ nodos unen 4 ramas (cruzamientos a nivel o vías superpuestas de bitrocha).
3. **Geometría de desvíos en el dataset:** El $99,25\%$ de los nodos de grado 3 ($1.462$ de $1.473$) presentan una configuración geométrica impecable de desvío: dos ramas salen con orientaciones casi idénticas (ángulo relativo $< 45^\circ$) y una rama troncal ingresa en sentido opuesto ($\approx 180^\circ$).
4. **Discontinuidades reales:** Existen 77 componentes conexas en el dataset. La componente principal abarca $2.197$ vías (la red troncal activa e histórica vinculada); las otras 76 componentes son apartaderos industriales desvinculados, ramales con vías tapadas o desmanteladas, o brechas de mapeo en OSM.
5. **Estaciones (169 nodos):** Muchas estaciones están mapeadas como nodos independientes contiguos a la vía (a distancias de entre $5\text{ m}$ y $40\text{ m}$ del riel, representando el edificio histórico de viajeros), y no como un nodo embebido directamente en la polilínea del riel.

---

## RailWeaver abstraction

Para satisfacer los requerimientos de simulación de M1 sin sobreingeniería, el modelo topológico de RailWeaver adoptará una arquitectura de dos niveles: **Grafo de Vía Segmentado** y **Transiciones Orientadas Permitidas**.

### 1. Entidades del dominio (`RailWeaver.Core.Topology`)

```text
[TrackEdge]  ------------------->  [TrackEdge]
      \                                /
       \                              /
        [TopologicalNode / Junction]
```

1. **`TopologicalNode` (Vértice topológico):**
   - Punto geométrico singular donde la vía:
     - Comienza o termina (extremo libre, topera o límite de provincia).
     - Se bifurca o une con otras vías (desvío, grado 3).
     - Se cruza a nivel con otra vía (cruzamiento plano, grado 4).
     - Cambia de trocha nominal o de estado operativo crítico.
   - Identificado por una clave determinista (ej. `node/{osm_id}` o coordenada canónica normalizada).

2. **`TrackEdge` (Arista atómica de vía):**
   - Segmento continuo de vía entre exactamente dos `TopologicalNode` (Nodo $A$ y Nodo $B$) sin bifurcaciones intermedias.
   - Posee:
     - Longitud geodésica acumulada en metros.
     - Geometría detallada (lista de coordenadas geográficas $lat, lon$ y cotas de elevación DEM).
     - Vectores tangentes de entrada y salida (rumbo o *azimuth* de aproximación en cada extremo).
     - Trocha nominal (`TrackGauge`).
     - Estado operativo (`TrackOperationalStatus`).
     - Sentido de referencia intrínseco (del nodo $A$ al nodo $B$).

3. **`DirectedTrack` (Arista orientada / Estado de marcha):**
   - Representa el movimiento sobre una `TrackEdge` en un sentido específico:
     - `Forward`: circulando de $A \to B$.
     - `Backward`: circulando de $B \to A$.
   - Este enfoque de grafo de líneas dirigidas (*Directed Line Graph*) permite que la navegación conozca la dirección con la que el tren arriba a un nodo.

4. **`TrackTransition` (Transición permitida en nodo):**
   - Regla determinista que valida si un tren que sale de `(Edge_in, Dir_in)` puede continuar por `(Edge_out, Dir_out)` en un `TopologicalNode`:
     - **Deflexión angular:** $\theta = |\text{Azimuth}_{salida} - (\text{Azimuth}_{entrada} \pm 180^\circ)|$. Si $\theta > \theta_{max}$ (típicamente $45^\circ\text{ a }60^\circ$ en aparatos de vía), la transición está prohibida sin maniobra de retroceso.
     - **Compatibilidad de trocha:** Ambas aristas deben compartir la misma trocha nominal o una de ellas ser bitrocha compatible.
     - **Restricción de cruzamiento a nivel:** En un nodo de grado 4 que representa un cruce plano, solo se permite la transición entre las aristas que forman la misma alineación continua ($\theta \approx 0^\circ$).

### 2. Algoritmo de segmentación de la red OSM

El grafo no debe requerir intervención manual para transformar las 2.374 polilíneas de OSM:
1. **Identificación de vértices topológicos:**
   - Se registran todas las coordenadas y referencias de nodos usadas por los tramos.
   - Todo nodo con más de un tramo incidente, o que sea extremo de un tramo, se promueve a `TopologicalNode`.
2. **Partición de tramos:**
   - Si un tramo contiene un `TopologicalNode` en su interior (conexión en T), se subdivide deterministamente en sub-aristas entre los vértices topológicos consecutivos.
3. **Generación de aristas atómicas:**
   - Cada sub-arista resultante se convierte en un `TrackEdge` atómico.
4. **Construcción del índice de adyacencia dirigida:**
   - Para cada extremo de cada arista, se calculan las transiciones cinemáticamente legales hacia las aristas vecinas.

### 3. Vinculación de estaciones (`Station Snapping`)

Dado que las estaciones en OSM son nodos que a menudo se encuentran a unos metros de las vías:
- Cada `RailwayStation` se proyecta perpendicularmente sobre el `TrackEdge` más cercano que coincida con su trocha (o con la trocha inferida de la línea).
- Se establece una distancia de tolerancia máxima: $d_{max} = 150\text{ metros}$.
- Si una estación está a menos de $150\text{ m}$ de una vía compatible, se le asigna un punto de detención (*StopPoint*) en dicha arista con su kilometraje / abscisa relativa.
- Si no hay vía compatible a menos de $150\text{ m}$, la estación se marca como *Desvinculada* (`OrphanStation`) y se reporta en las métricas de diagnóstico.

### 4. Detección y tratamiento de huecos en los datos (Data Gaps)

En lugar de conectar mágicamente tramos separados o ignorar que una vía no llega a destino:
1. **Clasificación de extremos libres (grado 1):**
   - *Límite de provincia:* El extremo cruza el bounding box oficial de Córdoba. Es un extremo de frontera natural.
   - *Topera legítima:* El extremo corresponde a una topera real de fin de vía (estación terminal, paragolpes o culatón de maniobras).
   - *Brecha de datos / Desconexión sospechosa:* El extremo finaliza abruptamente en campo abierto a corta distancia ($< 500\text{ m}$) de otra vía activa sin conexión, o la vía queda trunca en una playa de maniobras.
2. **Explicación al usuario:**
   - Cuando el usuario solicite un itinerario entre dos estaciones que residen en componentes desconectadas (o de trochas incompatibles), el buscador de rutas devolverá un resultado estructurado:
     - `Status: Unreachable`
     - `Reason: GaugeMismatch` (ej. se intentó viajar entre una estación métrica y una de trocha ancha) o `Reason: DisconnectedNetwork`.
     - `NearestReachablePoint`: Señala geográficamente el punto más cercano alcanzable y la distancia del bache de datos.

---

## Safety invariants

1. **Invariante de inflexión angular admisible:** Un tren en movimiento continuo nunca puede ejecutar una transición entre dos aristas cuyo ángulo de deflexión supere el límite geométrico de aparatos de vía ($\le 45^\circ$, equivalente a prohibir virajes cerrados y giros en U en desvíos).
2. **Invariante de aislamiento en cruzamientos a nivel:** En un cruzamiento plano de diamante, la matriz de transiciones permitidas solo puede conectar aristas pertenecientes a la misma alineación continua colineal; queda estrictamente vedada cualquier transición hacia la vía transversal.
3. **Invariante estricta de compatibilidad de trocha:** Ningún tren o ruta puede transitar por aristas de diferente trocha nominal a menos que ambas aristas declaren explícitamente compatibilidad bitrocha.
4. **Invariante de determinismo y orden independiente:** La construcción del grafo topológico a partir del dataset crudo de vías debe ser puramente determinista, produciendo exactamente la misma topología de aristas, nodos e índices independientemente del orden de los elementos en el archivo GeoJSON.
5. **Invariante de no-invención de infraestructura:** RailWeaver no sintetizará aristas virtuales ni cerrará baches topológicos de forma automática o probabilística para forzar la conectividad entre estaciones. Las desconexiones de la red real se reportan como datos fidedignos.

---

## Simplifications

- **Agujas sin estado mecánico dinámico en V0.6:** En RW-006 el grafo se utiliza para calcular rutas posibles e itinerarios válidos entre estaciones (Planning / Network routing). No se simula el motor de aguja individual moviéndose en tiempo real entre directa y desviada ni el cerrojo eléctrico de enclavamiento; eso formará parte del simulador por eventos en RW-009.
- **Radios finos de curva pospuestos a RW-007:** La validación angular en nodos se realiza mediante el ángulo de deflexión entre vectores tangentes iniciales. El cálculo detallado de curvas circulares, clotoides de transición y velocidades máximas en desvíos se abordará en la spec RW-007.
- **Tramos bitrocha como aristas multi-trocha:** Un tramo marcado como bitrocha se modela como un `TrackEdge` que admite transiciones tanto hacia vías de $1.000\text{ mm}$ como de $1.676\text{ mm}$, simplificando la mecánica interna de los cuatro rieles a nivel de grafo.

---

## Unknowns

- **¿Dónde debe construirse el grafo? (Offline vs Carga en memoria):**
  - *Opción A (Offline en `tools/`):* Generar un archivo `network-graph.json` precalculado durante el paso de extracción en `tools/extract-osm-railway.py`.
  - *Opción B (En memoria en `RailWeaver.Core` al cargar el dataset):* Cargar `tracks.geojson` y segmentar el grafo en tiempo de inicialización en C#.
  - *Análisis empírico:* Con 2.374 tramos y 3.917 aristas topológicas, la segmentación completa en JavaScript tardó menos de $25\text{ milisegundos}$. En C# en .NET 10 tomará aún menos tiempo ($< 10\text{ ms}$). Hacerlo en el Core mantiene el archivo GeoJSON como única fuente de verdad documental y evita redundancia de archivos estáticos versionados. La spec RW-006 evaluará formalmente esta decisión.
- **Grado de precisión del Tren de las Sierras en el acceso a Córdoba Mitre:**
  - Debe confirmarse si el recorrido del servicio actual de SOFSE entre Córdoba Mitre y Alta Córdoba cuenta con continuidad completa en el dataset o si presenta cortes en la zona de paso sobre el Río Suquía o empalmes de playa.

---

## Confidence

**HIGH** para:
- La formulación del grafo ferroviario como grafo orientado por aristas (*Directed Line Graph*) con transiciones cinemáticas angulares.
- La caracterización física de los aparatos de vía (agujas de punta/talón, prohibición de giros cerrados, cruzamientos de diamante sin transferencia).
- La necesidad y viabilidad de segmentar automáticamente las polilíneas de OSM en sus conexiones intermedias en T.
- La regla de aislamiento estricto por trochas nominales.

**MEDIUM** para:
- La completitud del mapeo de toperas (`buffer_stop`) en playas de maniobras del interior provincial, lo cual motivará la visualización explícita de "extremos muertos" en la interfaz de usuario.
