# Modelos digitales de elevación y relieve para RailWeaver

## Question

¿Qué fuentes de datos de elevación (DEM/MDE) son adecuadas para RailWeaver y para el dataset inicial de Córdoba, qué precisión y datum vertical entregan, y cómo deben integrarse en el backend (.NET) y en el visor 3D (CesiumJS) para permitir la visualización de relieve, consulta de cotas y perfiles de elevación en V0.4?

## Sources

- Instituto Geográfico Nacional (IGN), [Modelo Digital de Elevaciones (MDE-Ar v2.1)](https://www.ign.gob.ar/NuestrasActividades/Geodesia/ModeloDigitalElevaciones), consultado el 2026-09-22. Cobertura nacional continental en resolución de 30 m, formato GeoTIFF, vinculado a POSGAR 07 y marco altimétrico RN-Ar.
- Infraestructura de Datos Espaciales de la Provincia de Córdoba (IDECOR), [Geoportal Mapas Córdoba — MDE Provincial](https://mapascordoba.gob.ar/), consultado el 2026-09-22. Mosaico provincial unificado en GeoTIFF basado en SRTM 30 m, referencia POSGAR 07 / EPSG:4326.
- European Space Agency (ESA) / Airbus, [Copernicus DEM GLO-30](https://spacedata.copernicus.eu/), consultado el 2026-09-22. Modelo digital de superficie (DSM) global a 30 m de resolución, datum vertical EGM2008 (EPSG:3855), disponible bajo licencia de datos abiertos.
- NASA / USGS, [SRTM (Shuttle Radar Topography Mission) 1 Arc-Second Global](https://www2.jpl.nasa.gov/srtm/), resolución ~30 m, datum vertical EGM96.
- Cesium GS, [CesiumJS Terrain Documentation & Quantized-Mesh Specification](https://github.com/CesiumGS/quantized-mesh), especificación abierta de mallas de terreno optimizadas para GPU y streaming en web.
- International Union of Railways (UIC), [UIC Leaflet 700 / Railway track design standards], directrices de gradientes y rampas admisibles por tipo de tráfico. **A verificar:** la ficha UIC 700 trata la clasificación de líneas por carga por eje, no pendientes; los rangos de pendiente de esta nota requieren una fuente primaria antes de usarse como límites (V0.5).
- Ferrocarriles Argentinos / RITO (Reglamento Interno Técnico Operativo), prescripciones sobre pendientes en líneas de llanura vs líneas de montaña.

## Real-world behavior

### 1. Naturaleza de los datos de elevación
Un Modelo Digital de Elevaciones (DEM o MDE) representa la altitud del territorio:
- **DTM (Digital Terrain Model):** Representa el terreno desnudo (bare earth), filtrando vegetación y construcciones. Es el modelo ideal para ingeniería civil.
- **DSM (Digital Surface Model):** Representa la primera superficie reflectora capturada por el sensor (incluyendo copas de árboles y edificaciones). Copernicus DEM y SRTM son primariamente DSM, aunque en zonas abiertas o agrícolas se aproximan fuertemente al terreno.

A una resolución de 30 metros por píxel (aproximadamente 1 segundo de arco en el ecuador), una vía férrea de 3 a 5 metros de ancho no es resuelta directamente. Las obras de arte menores (alcantarillas, pequeños desmontes y terraplenes de 1 o 2 metros) quedan suavizadas dentro del píxel promedio. Para la escala provincial y la planificación de corredores de RailWeaver (V0.4 y V0.5), esta resolución es adecuada para capturar la topografía general, pasos de montaña, valles y pendientes macroscópicas.

### 2. Referencia vertical: Altura ortométrica vs. elipsoidal
Existen dos formas principales de expresar la elevación:
- **Cota ortométrica ($H$):** Altura sobre el nivel medio del mar (geoide). Es la que tiene sentido físico (determina hacia dónde fluye el agua y el esfuerzo de gravedad que siente un tren al trepar). IGN (RN-Ar), Copernicus DEM (EGM2008) y SRTM (EGM96) entregan cotas ortométricas en metros.
- **Altura elipsoidal ($h$):** Altura geométrica sobre el elipsoide matemático WGS84, utilizada nativamente por receptores GPS y por el sistema de coordenadas tridimensional de CesiumJS.

La relación física es:
$$h = H + N$$
donde $N$ es la ondulación del geoide. En la provincia de Córdoba, la ondulación del geoide oscila entre $+15$ y $+25$ metros aproximadamente. Si se proyectan datos con cotas ortométricas directamente como alturas elipsoidales sin compensación, existe un desfase vertical casi uniforme respecto al elipsoide, pero las **pendientes relativas** (la inclinación metro a metro $\Delta H / \Delta s$) se conservan casi intactas.

### 3. La pendiente en el dominio ferroviario
En el ferrocarril, la pendiente no se mide en grados sexagesimales sino en **tanto por mil (‰)** o porcentaje (%):
- $10\,‰ = 1\,\% = 10\text{ metros de ascenso por cada } 1.000\text{ metros horizontales}$.
- **Vías troncales de llanura:** Se busca no superar $3\,‰$ a $5\,‰$.
- **Vías principales mixtas:** Típicamente hasta $10\,‰$ – $12\,‰$.
- **Vías secundarias o de montaña:** $15\,‰$ a $25\,‰$.
- **Límites de adherencia simple:** Por encima de $35\,‰$ – $40\,‰$, la tracción rueda-carril de acero pierde confiabilidad (riesgo severo de patinamiento y frenado ineficaz), requiriendo cremallera o funicular. Por ejemplo, el Ramal A1 (Tren de las Sierras de Córdoba) posee rampas que rondan los $20\,‰$ a $25\,‰$.

## RailWeaver abstraction

### 1. Dataset de elevación de Córdoba
- **Fuente recomendada para el dataset inicial:** Mosaico de Córdoba del IGN / IDECOR en formato GeoTIFF (o Copernicus DEM GLO-30 recortado al bounding box provincial).
- **Almacenamiento:** Un archivo raster GeoTIFF optimizado (Cloud Optimized GeoTIFF o comprimido con LZW/DEFLATE) en el repositorio bajo `data/regions/cordoba/elevation/` (o descargable de forma reproducible mediante script en `tools/`).
- **Resolución:** ~30 metros (~1 segundo de arco), suficiente para toda la provincia. **Corrección de tamaño (2026-09-22):** la caja de Córdoba con margen son ~15.000 × 20.500 celdas; en `float32` son ~1,2 GB sin comprimir y cientos de MB comprimidos, por lo que no entra en el repositorio git. RW-004 lo resuelve con descarga reproducible y un formato por bloques comprimido (objetivo < 400 MB).

### 2. Capa de servicio en backend (`RailWeaver.Core` y `RailWeaver.Api`)
- `IElevationService` en Core:
  - `GetElevation(GeoCoordinate coordinate) -> ElevationResult` (retorna metros sobre el nivel del mar).
  - `GetElevationProfile(IReadOnlyList<GeoCoordinate> path, double samplingStepMeters) -> ElevationProfile`.
- La API expondrá endpoints de consulta:
  - `GET /api/regions/{id}/elevation?lat={lat}&lon={lon}`
  - `POST /api/regions/{id}/elevation/profile` (recibe array de coordenadas GeoJSON y devuelve serie de distancias acumuladas, altitudes y pendientes segmentarias).
- Ningún cálculo dependerá de Cesium en el cliente; el backend debe ser la fuente de verdad determinista para perfiles de elevación y validación de gradientes.

### 3. Visualización en frontend (`src/web`)
- En CesiumJS, sustituir `EllipsoidTerrainProvider` por un proveedor de terreno cuando el usuario activa la capa de relieve.
- Soporte para:
  - Streaming de terreno en formato estándar de Cesium (tiles `quantized-mesh` servidos localmente o proveedor de terreno configurable).
  - `clampToGround: true` en las polilíneas de vías ferroviarias para que los rieles sigan el contorno de la montaña y no queden flotando en el aire ni enterrados bajo tierra.
- Visualización de perfil altimétrico:
  - Componente de interfaz que grafica la curva de elevación siguiendo las directrices de `DESIGN.md` (DESIGN.md › Charts › Longitudinal profile: SVG inline, tokens del sistema, cara de datos Fragment Mono).

## Safety invariants

1. **Determinismo altimétrico:** Dadas dos coordenadas idénticas en la misma región, el backend siempre debe calcular la misma cota y la misma pendiente.
2. **Manejo estricto de NoData:** Si una coordenada consultada cae fuera de los límites del raster o sobre un píxel sin datos (`NoData`), el servicio nunca debe sustituirlo silenciosamente por `0` m (lo que generaría acantilados y pendientes irreales infinitas). Debe reportar un resultado no disponible o un error controlado.
3. **No-violación física de gradientes:** En V0.5 y posteriores, el simulador debe impedir que un corredor propuesto supere el gradiente máximo configurado para el tipo de servicio, basándose exclusivamente en el cálculo numérico del backend.

## Simplifications

1. **Resolución de 30 metros:** Se acepta que la vía real corre a veces sobre pequeños terraplenes de 1–2 metros o desmontes que el raster de 30 m suaviza. No se modela micro-infraestructura de tierra en V0.4.
2. **Alineación elipsoide/geoide:** En V0.4 se reportarán cotas ortométricas en metros directamente. En el visor 3D se asume que las alturas ortométricas proyectadas sobre el elipsoide WGS84 ofrecen una representación visual adecuada y coherente para el usuario, posponiendo conversiones geoidales complejas de alta densidad para cuando se requiera precisión submétrica.

## Unknowns

- Selección definitiva del proveedor de tiles 3D para CesiumJS: si generar las baldosas `quantized-mesh` estáticas para Córdoba offline mediante herramientas en Docker (`ctb-tile`) o consumir un servicio abierto compatible.
- Rendimiento del muestreo de perfiles en backend sobre archivos GeoTIFF grandes sin impacto en latencia (resuelto mediante lectura en memoria o indexación por bloques).

## Confidence

**HIGH.** Las fuentes de datos para la provincia de Córdoba están bien documentadas, probadas y disponibles gratuitamente (IGN e IDECOR). La física de pendientes ferroviarias es estándar y las capacidades nativas de CesiumJS para terreno y clamping son maduras y estables.
