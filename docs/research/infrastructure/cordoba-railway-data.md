# Datos de infraestructura ferroviaria existente para Córdoba

## Question

¿Qué fuentes de datos de infraestructura ferroviaria existen para la provincia de Córdoba,
cómo se comparan en calidad y cobertura, qué implicaciones de licencia (particularmente ODbL
de OpenStreetMap) imponen al proyecto, y qué estrategia de extracción, saneamiento y modelado
de dominio debe adoptar RailWeaver para V0.3 (Existing railway data)?

## Sources

- OpenStreetMap Foundation (OSMF), [Open Database License (ODbL) v1.0](https://opendatacommons.org/licenses/odbl/1.0/), consultado el 2026-09-22. Define los términos de copia, distribución, adaptación y el principio de Share-Alike (compartir bajo la misma licencia) para bases de datos derivadas.
- OSMF, [Licensing Guidelines — Produced Work vs Derivative Database](https://wiki.osmfoundation.org/wiki/Licence/Community_Guidelines), consultado el 2026-09-22. Establece la distinción entre obras producidas (visualizaciones, mapas renderizados) y bases de datos derivadas (datasets vectoriales para extracción estructurada).
- OpenRailwayMap, [Tagging conventions](https://wiki.openstreetmap.org/wiki/OpenRailwayMap/Tagging) y [Key:railway](https://wiki.openstreetmap.org/wiki/Key:railway), consultado el 2026-09-22. Especifica el etiquetado estándar internacional de vías, trochas (`gauge`), estados operacionales, desvíos y dependencias.
- Instituto Geográfico Nacional (IGN), [Capas SIG — Red Ferroviaria y Estaciones](https://www.ign.gob.ar/NuestrasActividades/InformacionGeoespacial/CapasSIG), consultado el 2026-09-22. Capas vectoriales oficiales de la red ferroviaria nacional en EPSG:4326.
- Infraestructura de Datos Espaciales de la Provincia de Córdoba (IDECOR / Mapas Córdoba), [Geoportal y Mapa Vial/Ferroviario](https://mapascordoba.gob.ar/), consultado el 2026-09-22. Capa oficial de infraestructura vial y ferroviaria de la Dirección Provincial de Vialidad de Córdoba.
- Comisión Nacional de Regulación del Transporte (CNRT) / Ministerio de Transporte de la Nación, [Portal de Datos Abiertos de Transporte](https://datos.transporte.gob.ar/), consultado el 2026-09-22. Mapas y registros oficiales de líneas, concesionarios de cargas y servicios de pasajeros de la República Argentina.
- Ferrocarriles Argentinos, *Reglamento Interno Técnico Operativo* (RITO). Establece normas operativas, señalización y definiciones reglamentarias fundamentales de la red argentina.

## Real-world behavior

### La red ferroviaria de Córdoba

La infraestructura ferroviaria de la provincia de Córdoba es el resultado de un desarrollo
histórico policéntrico que combina líneas de diferentes administraciones históricas y, de
manera determinante, **dos trochas incompatibles**:

1. **Ferrocarril General Belgrano (trocha métrica — 1000 mm)**:
   - **Líneas y ramales principales**:
     - *Ramal CC*: Eje troncal este-centro. Ingresa desde Santa Fe (San Francisco, Arroyito,
       Río Primero, Monte Cristo) hacia la estación Alta Córdoba en la capital. Tráfico
       activo de cargas de granos e insumos.
     - *Ramal A*: Eje centro-norte. Comunica Córdoba capital con Jesús María, Deán Funes
       y el noroeste argentino (Catamarca, La Rioja, Tucumán). Tráfico activo de cargas.
     - *Ramal A1 ("Tren de las Sierras")*: Comunica Córdoba (Alta Córdoba / Córdoba Mitre)
       con La Calera, San Roque, Cosquín, Valle Hermoso y Capilla del Monte, atravesando
       la Quebrada del Río Suquía y el Valle de Punilla. Servicio regular de pasajeros
       operado por SOFSE.
     - *Ramales secundarios y transversales*: Numerosos ramales en el este y sur de la
       red métrica (ej. Ramal CC7 a Miramar, Ramal A2, etc.), muchos de ellos en desuso
       o clausurados.
   - **Operadores**: Trenes Argentinos Cargas (Línea Belgrano) en cargas; SOFSE (Trenes
     Argentinos Operaciones) en pasajeros.

2. **Ferrocarril General Bartolomé Mitre (trocha ancha — 1676 mm / 5 ft 6 in)**:
   - **Líneas y ramales principales**:
     - *Línea Principal Retiro–Rosario–Córdoba*: Ingresa por el sudeste provincial (Marcos
       Juárez, Leones, Bell Ville, Villa María) y llega a la estación Córdoba Central (Mitre).
       Servicio activo de cargas y de pasajeros de larga distancia.
     - *Ramal Villa María–Río Cuarto*: Conexión troncal intermedia de trocha ancha.
     - *Ramales cerealeros del sur y este cordobés*: Ramales a Cruz Alta, Los Surgentes,
       Hernando, etc.
   - **Operadores**: Nuevo Central Argentino (NCA) como concesionario de carga; SOFSE en
     servicios de pasajeros de larga distancia y regionales (Villa María–Córdoba).

3. **Ferrocarril General San Martín (trocha ancha — 1676 mm)**:
   - Atraviesa transversalmente el extremo sur de la provincia (Laboulaye, Vicuña
     Mackenna) en su corredor troncal Buenos Aires–Mendoza.
   - **Operador**: Trenes Argentinos Cargas (Línea San Martín).

### Convivencia y discontinuidad de trochas

La incompatibilidad física de trocha es una restricción fundamental:
- Un vehículo ferroviario diseñado para trocha métrica (1000 mm) **no puede circular**
  por una vía de trocha ancha (1676 mm) ni viceversa.
- El intercambio directo solo es posible mediante transbordo de carga/pasajeros, cambio
  de bogies en instalaciones especializadas, o sobre infraestructura de **bitrocha**
  (tramos con tercer o cuarto riel).
- En la ciudad de Córdoba existe un tramo de conexión bitrocha histórico entre la estación
  Córdoba Mitre y Empalme Garita / Alta Córdoba, que permite a las formaciones del Tren de
  las Sierras ingresar a la estación Mitre.

### Estados operacionales reales

En el territorio coexisten cuatro estados físicos y regulatorios muy disímiles:
1. **Activo con tráfico regular**: Vías mantenidas con paso diario o semanal de trenes
   de carga o pasajeros.
2. **Activo ocasional / Desvíos industriales**: Vías de acceso a canteras, silos o
   fábricas, operadas bajo maniobra.
3. **En desuso (*disused*)**: Infraestructura con rieles y durmientes físicamente
   presentes, pero sin tráfico comercial regular, frecuentemente con maleza o pasos a
   nivel tapados. Son corredores prioritarios para reactivación o reutilización.
4. **Abandonado / Levantado (*abandoned*)**: Trazas donde los rieles han sido
   retirados, desmantelados o la traza ha sido interrumpida por obras viales o
   urbanización. Solo subsiste la servidumbre de paso o el terraplén.

## Comparativa de fuentes de datos

| Criterio | OpenStreetMap (OSM / ORM) | Instituto Geográfico Nacional (IGN) | IDECOR (Provincia de Córdoba) | CNRT / Transporte Nación |
|---|---|---|---|---|
| **Nivel de detalle geométrico** | **Extremadamente alto**. Vías individuales, vías dobles, apartaderos, desvíos en estaciones (*switches*), playas de maniobras. | **Medio-Bajo**. Trazas representadas como eje unificado simple. Sin topología de cambios. | **Medio**. Ejes viales y ferroviarios provinciales para gestión catastral y ambiental. | **Esquemático / Medio**. Mapas de red y corredores de concesión. |
| **Atributos de trocha (`gauge`)** | **Presente** en la mayoría de las vías principales (`1000`, `1676`). A veces ausente en desvíos menores. | Incompleto o generalizado por ramal. | No modelado explícitamente en capas abiertas. | Clasificado por ferrocarril histórico (Belgrano vs Mitre vs San Martín). |
| **Estado operacional** | Distingue `railway=rail`, `disused`, `abandoned`, `preserved`. | Mixto; a menudo incluye trazas que ya no existen físicamente. | Enfocado en el corredor físico y mantenimiento de vegetación. | Distingue ramales concesionados y fuera de servicio regulatorio. |
| **Estaciones y apeaderos** | `railway=station`, `halt` con nombres y posiciones geográficas detalladas. | Capa de estaciones oficial con nombres INDEC/IGN. | Puntos de interés y localidades. | Nómina oficial de estaciones y cabeceras de servicio. |
| **Licencia** | **ODbL 1.0** (Open Database License). Requiere atribución y Share-Alike en bases derivadas. | **Libre uso** citando institucionalmente al IGN. | **Datos Abiertos** provinciales (reutilización libre). | **Datos Abiertos** bajo Ley Nacional 27.275. |
| **Formato y acceso** | Overpass API (JSON/GeoJSON), extractos Geofabrik (.osm.pbf). | Descarga Shapefile, KML, GeoJSON, WFS. | Geoportal Mapas Córdoba, WFS, SHP, GeoJSON. | CSV, GeoJSON, Shapefiles en datos.transporte.gob.ar. |

### Conclusión de la comparación

OpenStreetMap es la **única fuente que provee la geometría detallada de vías, agujas,
playas de maniobras y trochas** requerida para que RailWeaver represente la infraestructura
física real en Córdoba. Las fuentes oficiales (IGN, IDECOR, CNRT) carecen del nivel de
detalle a nivel de vía (modelan solo ejes globales), pero resultan invaluables como
**patrón de verificación territorial y validación de nombres oficiales y estados de
concesión**.

## Licenciamiento y marco legal (ODbL 1.0)

El uso de datos provenientes de OpenStreetMap está regulado por la **Open Database License (ODbL)
v1.0**, complementada por las directrices comunitarias de la OSM Foundation. Esto impone reglas
estrictas que deben entenderse antes de incorporar datos a RailWeaver:

### 1. "Produced Work" vs "Derivative Database"

- **Produced Work (Obra producida)**:
  - Una visualización, mapa renderizado o imagen en pantalla generada a partir de datos de
    OSM.
  - La visualización en CesiumJS de las vías sobre el globo es una Obra Producida.
  - **Obligación**: Debe mostrar atribución visible: *"© OpenStreetMap contributors"* (o
    *"Datos de infraestructura ferroviaria de OpenStreetMap / OpenRailwayMap"*).
  - **Efecto de Share-Alike**: **No** contagia la licencia del resto de la aplicación web
    ni del motor de simulación.

- **Derivative Database (Base de datos derivada)**:
  - Si RailWeaver extrae las geometrías de OSM, las filtra, las transforma o las almacena en
    un archivo estructurado (por ejemplo, `data/regions/cordoba/railway.geojson` o tablas de
    PostGIS) y las distribuye o las pone a disposición para consulta estructurada (a través
    de la API), ese dataset constituye una Base de Datos Derivada.
  - **Obligación**: La base de datos derivada debe licenciarse bajo los términos de la ODbL
    1.0 y debe estar disponible para quien la reciba bajo esas mismas condiciones
    (Share-Alike).
  - Debe acompañarse de un archivo de licencia o aviso explícito en `data/regions/cordoba/`
    y en los metadatos de la API (`attribution`, `license: "ODbL-1.0"`).

### 2. Separación de código vs datos ("Collective Database")

La ODbL distingue la base de datos del software que la procesa:
- El código de `RailWeaver.Core`, `RailWeaver.Api` y `src/web` es software independiente y
  **no queda sometido a la ODbL**.
- El dataset geográfico derivado de OSM en `data/` sí queda cubierto por la ODbL.
- Esta separación limpia coincide exactamente con la arquitectura de RailWeaver: Córdoba es
  un dataset versionado, no una dependencia del código fuente.

## RailWeaver abstraction

### Estrategia de extracción técnica para V0.3

Para mantener la reproducibilidad, el funcionamiento sin conexión y la agilidad de desarrollo,
la extracción de datos para V0.3 debe ser un **proceso offline documentado y determinista**, no
una consulta dinámica en vivo en cada inicio de la aplicación:

1. **Pipeline de extracción**:
   - Una consulta estructurada en Overpass QL acotada al bounding box oficial de Córdoba
     definido en RW-001 (`west: -65.7720, south: -35.0002, east: -61.7708, north: -29.5004`):
     - Elementos `way["railway"]` (`rail`, `disused`, `abandoned`, `narrow_gauge`).
     - Elementos `node["railway"="station"]`, `node["railway"="halt"]`.
   - Puede ejecutarse mediante un script reproducible en `tools/extract-osm-railway.py` (o
     documentarse la consulta exacta de Overpass Turbo).
2. **Saneamiento y filtrado de datos**:
   - Descartar vías accesorias que no aporten a la red o que sean ruido de mapeo (ej.
     maquetas, tranvías históricos descontextualizados, funiculares que no sean ferrocarril).
   - Normalizar la trocha (`gauge`): si el valor es numérico, convertir a milímetros
     enteros (`1000`, `1676`). Si falta el tag `gauge`, inferirlo según la red/operador
     del ramal cuando sea unívoco (ej. Ramal Belgrano = 1000 mm; Ramal Mitre = 1676 mm), o
     marcarlo explícitamente como `Unknown`.
   - Normalizar el estado operacional: `Active`, `Disused`, `Abandoned`.
3. **Persistencia versionada**:
   - Guardar el resultado en formato GeoJSON estructurado en `data/regions/cordoba/railway.geojson`
     (o separar en `tracks.geojson` y `stations.geojson`).
   - Registrar metadatos completos: fecha de extracción, URL de consulta, licencia ODbL y
     atribución.
4. **Exposición en la API**:
   - `GET /api/regions/{id}/railway` expone el dataset validado por el Core.
5. **Visualización en el frontend (CesiumJS)**:
   - Capa Cesium `ExistingRailwayLayer` que dibuja polilíneas sobre el elipsoide.
   - Aplicar el sistema visual de [`DESIGN.md`](../../../DESIGN.md):
     - Vías activas: trazo sólido con contraste adecuado sobre el mapa base neutralizado.
     - Vías en desuso / abandonadas: trazo atenuado o punteado, diferenciando claramente
       la infraestructura transitable de la histórica.
     - Estaciones: marcadores sobrios técnicos con tooltip/popup que muestre nombre, tipo,
       trocha y estado.
     - Atribución en pantalla a OpenStreetMap y OpenRailwayMap.

### Modelo conceptual en `RailWeaver.Core`

En V0.3, el Core debe incorporar abstracciones mínimas en el namespace
`RailWeaver.Core.Infrastructure`:

```csharp
// Abstracciones preliminares independientes de proveedores GIS o frameworks

public enum TrackGaugeKind
{
    Unknown = 0,
    Metre = 1000,       // Belgrano (1000 mm)
    Standard = 1435,    // Urquiza (1435 mm)
    Broad = 1676        // Mitre / San Martín / Roca (1676 mm)
}

public readonly record struct TrackGauge(int WidthMillimetres, TrackGaugeKind Kind)
{
    public static TrackGauge Metre => new(1000, TrackGaugeKind.Metre);
    public static TrackGauge Broad => new(1676, TrackGaugeKind.Broad);
    public static TrackGauge Standard => new(1435, TrackGaugeKind.Standard);
    public static TrackGauge Unknown => new(0, TrackGaugeKind.Unknown);

    public static TrackGauge FromMillimetres(int mm) => mm switch
    {
        1000 => Metre,
        1435 => Standard,
        1676 => Broad,
        _ => new TrackGauge(mm, TrackGaugeKind.Unknown)
    };
}

public enum TrackOperationalStatus
{
    Active = 1,
    Disused = 2,
    Abandoned = 3
}

public enum TrackUsage
{
    Unknown = 0,
    MainLine = 1,
    BranchLine = 2,
    Siding = 3,
    Yard = 4,
    IndustrialSpur = 5
}

public sealed record TrackSegment(
    string Id,
    IReadOnlyList<GeoCoordinate> Geometry,
    TrackGauge Gauge,
    TrackOperationalStatus Status,
    TrackUsage Usage,
    string? Name,
    string? LineReference);

public enum StationType
{
    Station = 1,
    Halt = 2,
    Junction = 3
}

public sealed record RailwayStation(
    string Id,
    string Name,
    GeoCoordinate Location,
    StationType Type,
    TrackGauge Gauge);
```

## Safety invariants

1. **Incompatibilidad de trocha**: Dos tramos de vía con diferente trocha nominal
   (`WidthMillimetres`) no pueden conectarse en un mismo punto sin una entidad explícita
   que lo justifique (tramo bitrocha o instalación de transbordo).
2. **Invariante geométrica de vía**: Un `TrackSegment` debe contener como mínimo dos
   coordenadas geográficas válidas no idénticas consecutivas.
3. **Invariante operacional de estado**: La infraestructura en estado `Abandoned` no
   puede considerarse transitable ni asignable a rutas de simulación operativa; solo
   puede constituir servidumbres o alineamientos candidatos en Planning.
4. **Obligación legal de atribución**: Todo dataset, endpoint o visualización derivado
   de OSM debe contener y exponer de manera obligatoria e inmutable la atribución de
   derechos a OpenStreetMap contributors según ODbL 1.0.

## Simplifications

- **V0.3 no construye un grafo de ruteo**: El objetivo de V0.3 es la ingestión, validación,
  almacenamiento y visualización de la infraestructura existente, no el despacho de trenes
  ni la búsqueda de caminos óptimos (reservado para V0.5 y posteriores).
- **Aparatos de vía implícitos**: Los desvíos y cruzamientos (*switches / turnouts*) se
  representan en V0.3 a través de los nodos compartidos por las polilíneas de las vías; no
  se modela la máquina de cambio de agujas individual ni sus estados mecánicos (F0).
- **Geometría sobre el elipsoide**: Las vías se dibujan e interpretan en WGS84 sobre el
  elipsoide, postergando la proyección de cotas de elevación y cálculo de pendientes hasta
  la integración del DEM en V0.4.
- **Acotación al Bounding Box**: Se importan únicamente las vías y estaciones comprendidas
  en el bounding box oficial de Córdoba adoptado en RW-001, sin pretender abarcar toda la
  red federal argentina en este hito.

## Unknowns

- **Consistencia de tags en ramales rurales**: Es esperable que ramales secundarios o
  playas de maniobras menores en el interior de Córdoba carezcan de la etiqueta `gauge` o
  tengan discrepancias entre `disused` y `abandoned`. La extracción requerirá reglas de
  normalización robustas que asignen valores por defecto documentados.
- **Tramos bitrocha específicos**: Debe verificarse cómo está modelado en OSM el tramo urbano
  del Tren de las Sierras que comparte traza con trocha ancha (habitualmente mapeado como dos
  ways superpuestos o con tags específicos de trocha múltiple).
- **Evolución hacia PostGIS**: Aunque en V0.3 el dataset puede residir en un archivo GeoJSON
  versionado (manteniendo la simetría con `cordoba.json`), en hitos posteriores con búsquedas
  espaciales intensivas se evaluará si la ingestión de OSM se traslada a PostGIS mediante
  `osm2pgsql` o cargadores propios en .NET.

## Confidence

**HIGH** para:
- La caracterización del sistema ferroviario cordobés (red Belgrano vs Mitre, trochas 1000 mm
  vs 1676 mm, servicios del Tren de las Sierras y Retiro-Córdoba, operadores).
- Las obligaciones legales impuestas por la licencia ODbL 1.0 (Share-Alike sobre la base
  derivada, separación limpia del software de RailWeaver, necesidad de atribución).
- Las convenciones de etiquetado de OpenRailwayMap y su suficiencia técnica para representar
  la geometría de vías y estaciones.

**MEDIUM** para:
- El grado exacto de completitud y frescura del etiquetado de ramales secundarios y
  desvíos industriales en zonas rurales alejadas de los centros urbanos de Córdoba en OSM,
  lo que se constatará empíricamente al ejecutar la extracción para RW-003.
