# RW-001 — Geographic viewer

- Status: Completed (v0.2.0, 2026-09-21)
- Milestone: V0.2 — Geographic viewer → release `v0.2.0`

## Goal

Abrir RailWeaver y ver la provincia de Córdoba sobre un globo/mapa real con CesiumJS, con un modelo de coordenadas geográficas en el core y la región definida como **datos**, no como código.

## Scope

1. **Modelo de coordenadas en el core** (`RailWeaver.Core`, namespace `Geography`):
   - `GeoCoordinate` (WGS84 / EPSG:4326, latitud y longitud en grados decimales) con validación de rangos.
   - `GeoBoundingBox` (oeste, sur, este, norte) con validación y `Contains(GeoCoordinate)`. Decidir y documentar el tratamiento del antimeridiano (probablemente: no soportado todavía, rechazado explícitamente).
2. **Definición de región como dato**: `data/regions/cordoba.json` (id, nombre, bounding box, vista inicial de cámara, fuente y fecha del dato). La API la expone en `GET /api/regions/{id}` leyéndola del archivo; el core valida su contenido con los tipos anteriores.
3. **Viewer CesiumJS** en `src/web`: globo con imágenes base, cámara inicial desde la región, contorno de la bounding box de la región.
4. **Control de capas básico**: mostrar/ocultar imágenes base y contorno de región.
5. Nota de investigación `docs/research/geography/coordinate-reference-systems.md` (WGS84 vs proyecciones locales argentinas, p. ej. POSGAR 2007 / Gauss-Krüger faja 4, y cuándo importará).

## Non-goals

- Vías, estaciones o cualquier dato ferroviario (V0.3).
- Terreno/DEM y elevación (V0.4): usar elipsoide.
- Límite provincial real como polígono (puede ser una tarea posterior con fuente IGN).
- Persistencia en PostGIS: la región vive en un archivo.
- Router, state manager, librería de UI.

## Inputs

- `data/regions/cordoba.json` (bounding box aproximada de la provincia con su fuente declarada).

## Outputs

- Página principal mostrando Córdoba en Cesium.
- `GET /api/regions/cordoba` → JSON de la región.

## Acceptance criteria (verificados 2026-09-21)

- [x] `GeoCoordinate` rechaza latitudes fuera de [-90, 90] y longitudes fuera de [-180, 180]; tests cubren límites.
- [x] `GeoBoundingBox` rechaza cajas inválidas y responde `Contains` correctamente; tests incluyen puntos en el borde.
- [x] Ningún identificador, nombre ni coordenada de Córdoba aparece en `src/RailWeaver.Core` (solo en `data/`).
- [x] La app abre con la cámara sobre Córdoba y el contorno visible; el control de capas funciona.
- [x] La app arranca sin secretos commiteados; OpenStreetMap y el terreno elipsoidal no requieren token de Cesium ion.
- [x] `dotnet test`, `npm run lint`, `npm run build` pasan; CI verde.
- [x] ADR si se elige proveedor de imágenes o método de integración de Cesium con consecuencias relevantes. Ambas elecciones son reversibles y quedaron documentadas en esta spec; no requieren ADR nuevo.
- [x] `project-status.md` y `CHANGELOG.md` actualizados; tag `v0.2.0`.

## Relevant domain docs

- [ADR-005](../../decisions/ADR-005-cesiumjs-primary-renderer.md), [ADR-007](../../decisions/ADR-007-cordoba-first-dataset.md)

## Relevant architecture

- [Overview](../../architecture/overview.md): el core no conoce Cesium; la API es delgada; el frontend no contiene reglas de dominio.

## Tests

- Unit tests de `GeoCoordinate` y `GeoBoundingBox` en `RailWeaver.Core.Tests`.
- Test que carga `data/regions/cordoba.json` y valida que su contenido es una región válida.
- Primer test de la API solo si aporta (evaluar `Microsoft.AspNetCore.Mvc.Testing` como dependencia justificada).

## Decisions resolved before implementation

1. **Imágenes base — resuelto**: usar las teselas raster estándar de OpenStreetMap
   mediante `OpenStreetMapImageryProvider` para V0.2.
   - No requiere cuenta ni token y ofrece nombres, localidades y caminos útiles para
     orientarse durante esta etapa temprana.
   - Mostrar siempre la atribución requerida por OpenStreetMap.
   - Respetar la [Tile Usage Policy](https://operations.osmfoundation.org/policies/tiles/):
     solo navegación interactiva, caché HTTP normal, sin precarga masiva ni modo
     offline y sin tests automatizados que soliciten teselas reales.
   - El navegador solicita las teselas directamente; no se almacenan en el repo ni
     se retransmiten mediante la API.
   - Revaluar el proveedor antes de un despliegue público o cuando el tráfico deje de
     ser propio de desarrollo. Las alternativas relevadas son Sentinel-2 o imágenes
     comerciales mediante Cesium ion, un proveedor OSM con SLA o teselas propias.
   - La elección queda encapsulada en la configuración del viewer y no requiere ADR:
     es reversible y no modifica las fronteras arquitectónicas.
2. **Assets de Cesium con Vite — resuelto**: seguir la configuración oficial mínima
   de Cesium para Vite con el paquete `cesium` y `vite-plugin-static-copy`.
   - Copiar `Workers`, `ThirdParty`, `Assets` y `Widgets` desde
     `node_modules/cesium/Build/Cesium` hacia `dist/cesiumStatic` durante el build;
     en desarrollo, el plugin los sirve sin duplicarlos físicamente.
   - Definir `CESIUM_BASE_URL` como `/cesiumStatic/` mediante `define` en
     `vite.config.ts` e importar `cesium/Build/Cesium/Widgets/widgets.css` desde el
     frontend.
   - Usar imports ESM nombrados desde `cesium` y el `Viewer` completo; RW-001 necesita
     sus widgets, por lo que `@cesium/engine` solo no aporta una ventaja concreta.
   - Configurar explícitamente OpenStreetMap y el elipsoide para que construir el
     `Viewer` no solicite por defecto imágenes o terreno de Cesium ion.
   - No usar `vite-plugin-cesium`: es una abstracción comunitaria específica y menos
     transparente que la configuración publicada y mantenida por Cesium.
   - La ruta absoluta supone que la aplicación se sirve desde `/`, igual que el proxy
     `/api` actual. Si se despliega bajo un subpath, ambas rutas deberán configurarse
     juntas.
   - No requiere ADR: implementa ADR-005 sin cambiar fronteras arquitectónicas.
3. **Bounding box de Córdoba — resuelto**: usar la capa oficial de provincias del IGN,
   registro INDEC `14`, redondeada hacia afuera a cuatro decimales (`west -65.7720`,
   `south -35.0002`, `east -61.7708`, `north -29.5004`). Es una ayuda de navegación,
   no un límite legal. Fundamento y fuente en
   [coordinate-reference-systems.md](../../research/geography/coordinate-reference-systems.md).
4. **Flujo de datos de región — resuelto**: `data/regions/cordoba.json` se lee en el
   backend, se valida mediante los tipos geográficos del core y se expone en
   `GET /api/regions/cordoba`. El frontend no lee el archivo directamente. Esto
   establece la frontera `dataset → API → frontend` sin agregar persistencia ni
   lógica específica de Córdoba al core.
