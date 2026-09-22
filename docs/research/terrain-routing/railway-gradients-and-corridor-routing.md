# Restricciones de pendiente ferroviaria y búsqueda de corredores candidatos

## Question

¿Cuáles son los límites físicos y normativos de pendiente y rampa en el diseño ferroviario (según la práctica argentina de RITO/NTVO y estándares internacionales TSI INF / UIC / AREMA), y cómo debe estructurarse algorítmicamente en `RailWeaver.Core` la búsqueda determinista de un corredor preliminar (Candidate Corridor) sobre el terreno (V0.5) respetando dichas restricciones de gradiente sin inventar comportamiento no justificado?

## Sources

- **Reglamento Interno Técnico Operativo (RITO)**, Ferrocarriles Argentinos / Comisión Nacional de Regulación del Transporte (CNRT). Prescripciones reglamentarias sobre circulación y maniobras en pendientes y rampas pronunciadas (umbrales operativos a partir de $4\,‰$, limitaciones en playas y desvíos).
- **Normas Técnicas de Vía y Obras (NTVO)** y manuales de diseño geométrico de vía de Trenes Argentinos Infraestructura (ADIFSE) / Universidad de Buenos Aires (FIUBA). Criterios de diseño para trochas ancha ($1.676\text{ mm}$) y métrica ($1.000\text{ mm}$), determinando rampas máximas de proyecto para servicios de carga ($\le 10\,‰$ a $15\,‰$) y pasajeros ($\le 25\,‰$ a $30\,‰$).
- **European Union Agency for Railways (ERA)**, *Technical Specification for Interoperability relating to the 'infrastructure' subsystem (TSI INF)*, Commission Regulation (EU) No 1299/2014. Parámetros de gradiente máximo admisible: $35\text{ mm/m}$ (tramos hasta $0,5\text{ km}$ o $6\text{ km}$ acumulados), $20\text{ mm/m}$ (tramos hasta $3\text{ km}$), y limitación estricta de $2,5\text{ mm/m}$ ($2,5\,‰$) en andenes y vías de estacionamiento.
- **Comité Europeo de Normalización (CEN)**, *EN 13803: Railway applications - Track - Track alignment design parameters - Track gauges 1435 mm and wider*. Definición geométrica de acuerdos verticales y transición de pendientes.
- **American Railway Engineering and Maintenance-of-Way Association (AREMA)**, *Manual for Railway Engineering*, Capítulos 5 (*Track*) y 16 (*Economics of Railway Location and Operation*). Concepto de *ruling grade* (pendiente gobernante o determinante) y compensación de pendiente por curvatura ($0,04\,\%$ de reducción por grado de curva, equivalente a $\sim 650/R$ métrico).
- **Jong, J. C. & Schonfeld, P.** (2003), *An Evolutionary Approach for Selecting Rough Alignment of Railway Corridors*, y **Pu, J. et al.** (2021), *Railway Alignment Optimization on Raster DEM*. Técnicas de trazado de corredores preliminares mediante grafos regulares sobre modelos digitales de elevación y algoritmos tipo A* / Dijkstra con funciones de coste basadas en relieve.
- **RailWeaver Project Kickoff** (§15 *Route / Tramo Generator*, §39 *V0.5 Candidate corridor prototype*).

## Real-world behavior

### 1. Física del contacto rueda-carril y resistencia por gravedad

El ferrocarril convencional se basa en la adherencia entre ruedas de acero cónicas y rieles de acero. La fricción acero-acero es extraordinariamente baja (coeficiente de fricción por rodadura $\mu_r \approx 0,001$ a $0,002$), lo que permite mover cargas masivas con mínimo consumo energético en plano.

Sin embargo, el coeficiente de adherencia para tracción y frenado ($\mu_a$) también es moderado: en condiciones secas óptimas oscila entre $0,25$ y $0,35$, pero en riel húmedo, helado o con hojas puede descender drásticamente por debajo de $0,10$.

Cuando un tren de masa $M$ circula por una pendiente con ángulo de inclinación $\alpha$, experimenta una componente de fuerza gravitatoria opuesta al avance (en ascenso) o aceleradora (en descenso):
$$F_g = M \cdot g \cdot \sin(\alpha) \approx M \cdot g \cdot s$$
donde $s = \tan(\alpha) \approx \sin(\alpha)$ es la pendiente en valor absoluto.

En una pendiente de apenas $10\,‰$ ($1\,\%$):
- La fuerza de resistencia por gravedad es de $10\text{ kgf}$ por tonelada de tren ($98\text{ N/t}$).
- Esta resistencia es de **3 a 5 veces superior** a la resistencia al rodamiento básica en llano (que suele rondar $1,5$ a $3\text{ kgf/t}$).
- Por ello, duplicar la pendiente reduce drásticamente el tonelaje que una locomotora puede arrastrar o exige locomotoras auxiliares (*helpers* o empujadoras).

### 2. Terminología y unidades ferroviarias

En la ingeniería ferroviaria hispanohablante y en la normativa argentina (RITO / NTVO), la pendiente no se expresa en grados sexagesimales ni habitualmente en porcentaje:
- **Tanto por mil ($‰$) o milésimas:** Representa los metros de variación de cota por cada $1.000\text{ metros}$ de longitud horizontal.
  $$10\,‰ = 10\text{ mm/m} = 1\,\% = 10\text{ m de desnivel en } 1.000\text{ m}$$
- **Rampa:** Tramo con pendiente en sentido ascendente respecto al sentido de marcha. Limita la potencia tractora, la adherencia y el tonelaje admisible.
- **Pendiente / Declive:** Tramo en sentido descendente. Limita el frenado continuo y la capacidad de disipación térmica de zapatas y discos de freno, creando riesgo crítico de tren fuera de control (*runaway train*).
- **Pendiente gobernante o determinante (*Ruling grade*):** Es la mayor rampa continua sin compensar de una sección de línea que determina la carga máxima neta que puede remolcar una locomotora estándar sin requerir tracción auxiliar.

### 3. Límites reglamentarios y operativos según tipo de tráfico

| Tipo de línea / servicio | Pendiente típica de diseño | Pendiente máxima excepcional | Justificación física / operativa |
|---|---|---|---|
| **Carga pesada / Troncales llanura** | $2\,‰$ a $5\,‰$ ($0,2\% - 0,5\%$) | $10\,‰$ ($1,0\%$) | Maximizar toneladas brutas por tren y eficiencia de combustible. |
| **Líneas mixtas principales (convencional)** | $8\,‰$ a $12\,‰$ ($0,8\% - 1,2\%$) | $15\,‰$ ($1,5\%$) | Balance entre movimiento de trenes de carga estándar y pasajeros. |
| **Pasajeros convencional y cercanías** | $15\,‰$ a $20\,‰$ ($1,5\% - 2,0\%$) | $25\,‰$ a $30\,‰$ ($2,5\% - 3,0\%$) | Formaciones livianas o automotores (EMU/DMU) con alta relación potencia/peso. |
| **Alta velocidad dedicada** | $25\,‰$ a $35\,‰$ ($2,5\% - 3,5\%$) | $40\,‰$ ($4,0\%$) | Trenes de pasajeros ultraligeros con tracción distribuida y sin tráfico de cargas (ej. LGV París-Sud-Est hasta $35\,‰$). |
| **Montaña en adherencia simple** | $20\,‰$ a $30\,‰$ ($2,0\% - 3,0\%$) | $35\,‰$ a $40\,‰$ ($3,5\% - 4,0\%$) | Trazados sinuosos siguiendo quebradas. Límite de seguridad para adherencia de rueda de acero sin cremallera. |
| **Estaciones y playas de maniobra** | $0\,‰$ a $1\,‰$ ($0,0\% - 0,1\%$) | $2,5\,‰$ ($0,25\%$) | Evitar deslizamiento accidental de vagones o material desenganchado por gravedad (RITO / TSI INF). |

### 4. Referencias en el contexto de Córdoba y Argentina

- **Ramal A1 (Tren de las Sierras, trocha métrica $1.000\text{ mm}$):** Cruza las Sierras Chicas entre Córdoba y Cruz del Eje. Presenta sectores continuos con rampas de entre $18\,‰$ y $25\,‰$, llegando localmente a casi $27\,‰$. Opera exclusivamente con material rodante liviano (duplas diésel Alna DMU o coches motores) debido a la imposibilidad de operar trenes de carga pesados en esas pendientes.
- **Líneas de llanura del Ferrocarril Mitre (trocha ancha $1.676\text{ mm}$ hacia Rosario y Retiro):** Trazadas casi íntegramente con pendientes inferiores al $3\,‰$ a $5\,‰$, lo que históricamente permitió trenes graneleros de gran longitud.

## RailWeaver abstraction

### 1. Definición del problema en V0.5

El objetivo del prototipo de corredor candidato en V0.5 no es calcular planos constructivos de ingeniería civil con curvas de transición ni calcular volúmenes exactos de movimiento de suelos, sino responder la pregunta macroscópica de planificación:
> *Dados un origen geográfico $A$, un destino $B$ y una restricción de pendiente máxima admisible $s_{\max}$, ¿existe un camino sobre el terreno que una ambos puntos sin superar dicha pendiente, y cuál es su trazado y perfil de elevación?*

### 2. Espacio de búsqueda y discretización (Malla orientada al problema)

El dataset de elevación de Córdoba (V0.4) dispone de una resolución nativa de $\sim 30\text{ m}$ ($15.000 \times 20.500$ píxeles). Ejecutar un algoritmo de búsqueda exhaustivo sobre cientos de millones de celdas es prohibitivo en tiempo de respuesta y consumo de memoria.

Para V0.5, el core aplicará la abstracción de **malla de búsqueda acotada (*coarse grid*)**:
1. **Región de interés (Bounding Box):** Se delimita un rectángulo geográfico que abarca $A$ y $B$, expandido con un margen de holgura lateral (*buffer*, típicamente un $20\% - 30\%$ de la distancia directa o un mínimo de $10\text{ a }15\text{ km}$ a cada lado) para permitir que el camino rodee elevaciones o sierras.
2. **Paso de discretización ($d_{\text{step}}$):** Malla regular de nodos espaciados a una distancia uniforme fija (por ejemplo, entre $250\text{ m}$ y $500\text{ m}$).
3. **Cota de los nodos:** Cada nodo del grafo toma su elevación consultando `IElevationService` (desarrollado en V0.4), garantizando continuidad con el DEM determinista del core.
4. **Topología de conectividad:** Cada nodo se conecta con sus 8 vecinos adyacentes (ortogonales y diagonales), o con 16 vecinos (incluyendo saltos tipo caballo de ajedrez para reducir la distorsión métrica angular característica de mallas ortogonales).

### 3. Modelo de grafo y función de costo en A*

Sea $u$ el nodo actual y $v$ un nodo vecino, con distancia en proyección horizontal $d(u, v)$ y cotas $H(u)$ y $H(v)$:
- La pendiente local del tramo es:
  $$s(u, v) = \frac{|H(v) - H(u)|}{d(u, v)}$$
- **Restricción dura (Hard constraint):**
  $$\text{Si } s(u, v) > s_{\max} \implies \text{Arista intransitable (costo } \infty \text{ o podada)}$$
- **Función de costo para aristas transitables ($s(u, v) \le s_{\max}$):**
  El objetivo del ferrocarril no es solo encontrar cualquier camino que cumpla la pendiente, sino minimizar la longitud y penalizar el esfuerzo de relieve (evitando subidas y bajadas innecesarias cuando existe terreno llano):
  $$\text{Cost}(u, v) = d(u, v) \cdot \left(1 + \beta \cdot \left(\frac{s(u, v)}{s_{\max}}\right)^2\right)$$
  donde:
  - $d(u, v)$ es la distancia geodésica entre nodos.
  - $\beta \ge 0$ es un factor de penalización de relieve (ponderador de costo de gradiente). Si $\beta = 0$, busca la distancia más corta sin importar qué tanto trepe dentro de $s_{\max}$; con $\beta > 0$, el trazado prefiere rodear lomas o seguir valles fluviales si el rodeo es razonable.
- **Heurística admisible ($h(u)$):**
  Distancia geodésica tridimensional (o euclidiana sobre proyección) desde $u$ hasta el destino $B$. Al ser siempre menor o igual que la distancia real a recorrer, garantiza la admisibilidad y optimalidad en A*.

### 4. Estructuras del dominio en `RailWeaver.Core`

```csharp
namespace RailWeaver.Core.Corridors;

public sealed record CorridorRequest(
    GeoCoordinate Origin,
    GeoCoordinate Destination,
    double MaxGradientPerMille, // ej. 15.0 para 15‰ (1.5%)
    double? StepMeters = null   // resolución de malla (por defecto 250m - 500m)
);

public sealed record CorridorMetrics(
    double TotalDistanceMeters,
    double StraightLineDistanceMeters,
    double Sinuosity,             // TotalDistance / StraightLineDistance
    double MaxGradientPerMille,   // gradiente pico observado en el trazado
    double TotalAscentMeters,
    double TotalDescentMeters,
    IReadOnlyDictionary<string, double> DistanceByGradientBand
);

public sealed record CandidateCorridor(
    string Id,
    GeoCoordinate Origin,
    GeoCoordinate Destination,
    IReadOnlyList<GeoCoordinate> Alignment, // polilínea de coordenadas
    ElevationProfile ElevationProfile,     // perfil longitudinal segmentado
    CorridorMetrics Metrics
);

public interface ICorridorService
{
    CorridorResult FindCandidateCorridor(CorridorRequest request);
}
```

## Safety invariants

1. **Invariante estricta de gradiente:** Ningún segmento consecutivo del corredor generado sobre la malla de cálculo puede superar el `MaxGradientPerMille` especificado.
2. **Honestidad ante imposibilidad física:** Si no existe un camino transitable entre el origen y el destino respetando la pendiente máxima (por ejemplo, querer trepar directamente las Altas Cumbres con un límite de $10\,‰$ sin túneles), el sistema **debe fallar explícitamente** reportando que no existe ruta factible para esos parámetros (`CorridorResult.NoFeasiblePath`), en lugar de violar la restricción o generar trayectorias ficticias en el aire.
3. **Determinismo total:** Ante idéntico origen, destino, dataset de elevación y parámetros de búsqueda, el algoritmo debe arrojar exactamente el mismo corredor, métricas y perfil, sin depender de estados aleatorios ni heurísticas estocásticas.
4. **Aislamiento del Core:** La búsqueda de caminos y evaluación de corredores no depende de librerías web, bases de datos externas ni CesiumJS; se ejecuta completamente como lógica pura en `RailWeaver.Core`.

## Simplifications

1. **Superficie de terreno natural (sin desmontes ni terraplenes automáticos):** El trazado se proyecta directamente sobre las cotas del DEM. No se calculan cortes de montaña ni terraplenes de compensación de tierras (*cut-and-fill*) en V0.5.
2. **Sin obras de arte mayores (túneles ni puentes automáticos):** El algoritmo busca pasos superficiales continuos. Si una montaña o quebrada no permite el paso por gradiente, el trazado debe rodearla o el algoritmo declara inviable el corredor.
3. **Geometría de polilínea sin espirales ni peralte:** La polilínea resultante sigue los centros de celda del grafo. No se calculan curvas de transición (clotoides), peraltes ni radios de curvatura mínimos en esta fase preliminar.
4. **Costo de ocupación de suelo homogéneo:** No se penalizan cruces urbanos, ríos, carreteras existentes ni reservas naturales en V0.5. El único costo considerado es la distancia geométrica combinada con la penalización de pendiente topográfica.

## Unknowns

1. **Comportamiento en gargantas y valles estrechos:** En pasos de montaña angostos (ej. Quebrada de San Roque o desfiladeros de las Sierras Chicas), una malla gruesa ($250\text{ m}$ a $500\text{ m}$) puede muestrear puntos en las laderas abruptas y no en el fondo del valle, haciendo que un paso transitable real parezca impracticable. Durante la implementación de V0.5 se calibrará empíricamente el tamaño de celda adecuado.
2. **Sinuosidad en cuadrícula:** La conectividad en cuadrícula (8 vecinos) puede inducir artefactos de alineación ortogonal/diagonal (ángulos de 45° y 90°). Se evaluará la necesidad de aplicar un filtrado geométrico liviano (tipo Ramer-Douglas-Peucker o suavizado de vértices) a la salida.

## Confidence

**HIGH.**
- La fundamentación física del contacto rueda-carril y los límites reglamentarios (RITO, NTVO, TSI INF, AREMA) son sólidos, ampliamente contrastados y formalmente verificados.
- La formulación del algoritmo A* con restricciones duras de pendiente sobre cuadrículas de elevación es determinista, matemáticamente demostrable y estándar en la literatura de planificación territorial preliminar.
- El Core ya dispone de la infraestructura de elevación determinista (`IElevationService`) validada en V0.4 para alimentar el costo de cada nodo sin requerir dependencias adicionales.
