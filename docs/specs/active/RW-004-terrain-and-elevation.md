# RW-004 — Terrain and elevation

- Status: Active
- Milestone / release objetivo: V0.4 — Terrain → release `v0.4.0`

## Goal

Que RailWeaver conozca la altura del terreno de Córdoba: mostrar el relieve real en 3D con las vías apoyadas sobre él, consultar la altura de cualquier punto y dibujar una línea para ver su perfil longitudinal con pendientes en ‰. Todo sale de un único dataset de elevación, y el backend es la única fuente de verdad para cotas y pendientes. Con esto se cierra el paso `elevation profile` del vertical slice de M0.

## Scope

1. **Investigación de dominio** (ya resuelta): [digital-elevation-models.md](../../research/geography/digital-elevation-models.md), con las correcciones de tamaño, tipografía y fuente UIC del 2026-09-22.
2. **Dataset de elevación**:
   - Fuente: **Copernicus DEM GLO-30** (≈30 m, cotas ortométricas sobre EGM2008). Se obtiene del bucket público `copernicus-dem-30m` de AWS Open Data, sin cuenta.
   - Recorte: la bounding box de Córdoba de RW-001 más un margen de 0,1° por lado, para que el borde del relieve no quede a la vista al navegar la provincia.
   - **El raster no se commitea**, porque pesa ~1,2 GB. Vive en `data/regions/cordoba/elevation/` (en `.gitignore`), y en git queda solo la receta:
     - `elevation.source.json`: fuente, URL, fecha de acceso, lista de tiles de origen, versión de GDAL usada, bounding box, tamaño de la grilla, paso de celda, valor NoData, SHA-256 del resultado, licencia y texto de atribución.
     - `LICENSE-elevation.md`: licencia de Copernicus DEM con el texto de atribución exacto copiado del documento de licencia oficial.
   - Formato local: grilla `float32` little-endian en filas norte→sur, más su manifiesto. Se lee con la BCL de .NET (archivo mapeado en memoria), sin dependencias nativas.
3. **Herramienta de obtención** `tools/fetch-elevation.py`:
   - Usa solo la biblioteca estándar de Python, como `extract-osm-railway.py`.
   - Delega el raster en la imagen oficial de GDAL con versión fija (`docker run`). Docker ya es requisito del proyecto por PostGIS.
   - Descarga los tiles necesarios, arma el mosaico, recorta y escribe la grilla y el manifiesto.
   - Si al volver a correrla el SHA-256 no coincide con el del manifiesto commiteado, falla con un mensaje claro.
   - Si falta algún tile de cobertura, falla en vez de rellenar.
4. **Core** (`RailWeaver.Core.Geography`, sin E/S):
   - `IElevationSource`: altura en una coordenada, o "sin dato".
   - `ElevationSample` / `ElevationProfile`: distancia acumulada, coordenada, cota (o sin dato) y pendiente del intervalo en ‰.
   - `ElevationProfileBuilder`, que construye el perfil muestreando la polilínea cada 30 m. Incluye siempre los vértices y el punto final.
   - Distancia: gran círculo sobre el radio medio de WGS84 (6.371.008,8 m). Es una simplificación documentada con error < 0,5 %, despreciable para pendientes a escala de corredor.
   - Interpolación bilineal entre las cuatro celdas vecinas. Si alguna es NoData, la muestra queda "sin dato".
   - **Nunca se reemplaza un NoData por 0.** Si alguno de los dos extremos de un intervalo no tiene dato, ese intervalo no tiene pendiente.
   - Resultados deterministas para la misma entrada.
5. **API** (adaptador en `RailWeaver.Api`, con el patrón `*FileStore`):
   - `GET /api/regions/{id}/elevation?lat=&lon=`:
     - responde la cota en metros sobre el nivel del mar;
     - si el punto está fuera de cobertura o cae sobre NoData, responde 200 con `elevation: null` y el motivo.
   - `POST /api/regions/{id}/elevation/profile`:
     - recibe un arreglo de coordenadas y devuelve el perfil;
     - acepta entre 2 y 100 vértices y hasta 20.000 muestras (≈600 km); pasados esos límites responde 400.
   - `GET /api/regions/{id}/terrain/{level}/{x}/{y}`:
     - devuelve un heightmap binario de 65 × 65 `float32` en el `GeographicTilingScheme` de Cesium, usado solo para la visualización;
     - fuera de la cobertura el relieve visual vale 0 (terreno plano). Es la única excepción a la regla de NoData y no se usa para cálculos;
     - lleva cabeceras de caché ligadas al SHA-256 del dataset.
   - Si el dataset no está descargado, los endpoints de elevación responden 503 con un mensaje que nombra el comando para obtenerlo, y el resto de la API sigue funcionando.
   - Cotas en metros con precisión de centímetro; pendientes en ‰ con una décima.
6. **Frontend** (según `DESIGN.md`):
   - **Relieve 3D**:
     - `CustomHeightmapTerrainProvider` alimentado por el endpoint de terreno, activado por defecto y conmutable desde el control de capas;
     - si el dataset no está disponible, vuelve al elipsoide y la barra de estado lo avisa;
     - las vías y el contorno de región se apoyan sobre el terreno (`clampToGround`).
   - **Altura de un punto**:
     - al hacer clic en el mapa, el panel de detalle muestra la cota del punto clicado en la cara de datos;
     - si el clic cae sobre una vía o una estación, la cota aparece junto a sus metadatos de RW-003.
   - **Perfil de una línea**:
     - una herramienta "Perfil" en el riel lateral permite marcar vértices con clic, terminar con doble clic o Enter y cancelar con Esc;
     - el perfil se muestra en el **dock de análisis** (DESIGN.md › Layout) como perfil longitudinal en SVG inline (DESIGN.md › Charts), sin librería de gráficos;
     - el dock muestra distancia total, cota mínima y máxima, desnivel acumulado de subida y bajada, y pendiente máxima en ‰;
     - los tramos sin dato se ven como huecos rotulados, nunca como una caída a 0.
   - **Pendientes sin juicio**: se muestran los valores en ‰ sin bandas de `warning`, porque todavía no hay un límite de pendiente configurado. Esos límites llegan con `maximumGradient` en V0.5.
   - **Atribución**: el texto de Copernicus DEM va en la barra de estado junto a los créditos existentes.

## Non-goals

- Límites de pendiente o avisos de pendiente excedida (V0.5, `maximumGradient`).
- Generación de corredores (V0.5).
- Perfil de una vía o ramal existente: requiere unir los tramos de OSM en una red continua, un trabajo de topología que queda para más adelante.
- Conversión geoide ↔ elipsoide. Las cotas ortométricas se dibujan sobre el elipsoide con un desfase casi uniforme de ~15–25 m, que no afecta las pendientes.
- Modelado de terraplenes, desmontes u obras de arte que el raster de 30 m suaviza.
- Fuentes IGN o IDECOR como alternativa o validación cruzada.
- Descarga del dataset en CI; PostGIS; librería de gráficos; tests de frontend.

## Inputs

- Tiles Copernicus DEM GLO-30 que cubren la bounding box de Córdoba más el margen.
- `data/regions/cordoba.json` (bounding box).

## Outputs

- `tools/fetch-elevation.py`, `data/regions/cordoba/elevation.source.json`, `data/regions/cordoba/LICENSE-elevation.md` y la entrada en `.gitignore`.
- Tipos de elevación y perfil en `RailWeaver.Core.Geography`.
- Los tres endpoints de elevación y terreno.
- Relieve 3D, cota por clic y herramienta de perfil con dock de análisis en el visor.
- README: sección breve de puesta en marcha con el comando para descargar el relieve.

## Acceptance criteria

- [ ] `tools/fetch-elevation.py` genera la grilla desde cero en una máquina con Docker, y el SHA-256 coincide con el del manifiesto commiteado.
- [ ] Ningún archivo raster queda en git; `git check-ignore` lo confirma.
- [ ] `ElevationProfileBuilder` interpola bilinealmente, muestrea cada 30 m incluyendo vértices y punto final, y calcula la pendiente en ‰. Hay tests con una grilla sintética de valores conocidos (plano inclinado → pendiente constante exacta).
- [ ] NoData y puntos fuera de cobertura devuelven "sin dato", nunca 0. Un intervalo sin dato en un extremo no tiene pendiente. Hay tests que lo cubren.
- [ ] La misma entrada produce siempre el mismo perfil (test de determinismo).
- [ ] Los endpoints respetan los límites (2–100 vértices, ≤ 20.000 muestras → 400), responden 404 para una región inexistente y 503 con el comando de descarga si falta el dataset. Hay tests de API con una grilla de prueba chica en el proyecto de tests.
- [ ] El endpoint de terreno devuelve 65 × 65 `float32`, con 0 fuera de cobertura y cabeceras de caché.
- [ ] En el visor, las Sierras se ven en relieve, las vías de RW-003 siguen el terreno y la capa de relieve se puede apagar. Sin dataset, el visor funciona plano y avisa en la barra de estado.
- [ ] Un clic en el mapa muestra la cota del punto en el panel de detalle.
- [ ] La herramienta Perfil dibuja una línea y muestra en el dock de análisis el perfil, la distancia, las cotas mínima y máxima, los desniveles y la pendiente máxima en ‰, con los huecos sin dato rotulados. Sigue DESIGN.md: tokens, cara de datos y sin bandas de `warning`.
- [ ] Verificación manual contra el mundo real: la cota del Cerro Champaquí da ≈ 2.790 m y la de la ciudad de Córdoba ≈ 400 m. Si hay diferencias de decenas de metros, se explican (DSM, resolución) en el registro de la spec.
- [ ] La atribución de Copernicus DEM se ve en pantalla y figura en el manifiesto.
- [ ] `CoreBoundaryTests` sigue en verde: el core no hace E/S ni referencia frameworks.
- [ ] `dotnet test`, `npm run lint`, `npm run build` pasan; CI en verde sin descargar el dataset.
- [ ] `project-status.md`, `architecture/overview.md`, `CHANGELOG.md` y README actualizados; kickoff revisado; tag `v0.4.0`.

## Relevant domain docs

- [digital-elevation-models.md](../../research/geography/digital-elevation-models.md)
- [coordinate-reference-systems.md](../../research/geography/coordinate-reference-systems.md)
- [ADR-005](../../decisions/ADR-005-cesiumjs-primary-renderer.md), [ADR-007](../../decisions/ADR-007-cordoba-first-dataset.md)

## Relevant architecture

- [Overview](../../architecture/overview.md):
  - el core define las abstracciones (`IElevationSource`) y el cálculo del perfil;
  - el adaptador que lee el archivo vive en la API, como `RailwayFileStore`;
  - Cesium solo dibuja: ningún cálculo de cota o pendiente ocurre en el navegador.
- [DESIGN.md](../../../DESIGN.md) › Layout (dock de análisis), Charts (perfil longitudinal) y barra de estado (atribución y aviso de dato faltante).

## Tests

- Core: interpolación bilineal, muestreo, distancia, pendiente, NoData y determinismo, sobre grillas sintéticas en memoria.
- API: los tres endpoints con una grilla de prueba de pocas celdas copiada al output de tests; casos 200, 400, 404 y 503.
- Frontera del core: `CoreBoundaryTests` sin cambios.
- Dataset real: la verificación de cotas conocidas es manual y queda registrada en la spec, porque CI no descarga el raster.

## Decisions resolved before implementation

1. **Fuente — Copernicus DEM GLO-30** (decidido con el usuario). Tiene descarga reproducible sin cuenta, licencia abierta con atribución y cobertura global. Es un DSM, pero en el campo cordobés la diferencia con el terreno desnudo es menor que la resolución. IGN MDE-Ar queda como posible validación futura.
2. **Resolución 30 m, dataset fuera de git con descarga por comando** (decidido con el usuario). A 90 m cabría mejor, pero se pierden pendientes reales en las Sierras. Git LFS no se usa porque agrega un servicio y cuotas sin necesidad.
3. **Relieve 3D servido por la propia API** (decidido con el usuario). `CustomHeightmapTerrainProvider` (CesiumJS ≥ 1.97; el proyecto usa 1.145) pide heightmaps al backend, así que lo que se ve y lo que se calcula sale del mismo dato. Se descartan tanto `ctb-tile` (herramienta sin mantenimiento que además genera miles de archivos) como Cesium World Terrain (requiere token de ion y usaría un dato distinto del de los cálculos).
4. **Interacciones** (decidido con el usuario, por recomendación): la cota por clic y el perfil de línea libre entran en esta spec; el perfil de una vía existente queda fuera (ver Non-goals).
5. **Formato local sin dependencias nativas**: GDAL corre solo dentro de la herramienta offline, en contenedor. La API lee una grilla `float32` cruda con la BCL. No se agrega una librería TIFF ni GDAL a .NET.
6. **Sin juicio de pendientes en V0.4**: se informan valores en ‰ sin umbrales. Los rangos típicos de la nota de investigación todavía no tienen una fuente primaria verificada (ver la corrección sobre UIC 700).
