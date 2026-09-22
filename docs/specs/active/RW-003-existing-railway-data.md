# RW-003 — Existing railway data

- Status: Active
- Milestone / release objetivo: V0.3 — Existing railway data → release `v0.3.0`

## Goal

Que RailWeaver muestre, sobre el mapa de Córdoba, la infraestructura ferroviaria real: vías (con su trocha y estado operacional) y estaciones, extraídas de OpenStreetMap como un dataset versionado y trazable — cerrando el segundo paso del vertical slice de M0 (`existing railway geometry`).

## Scope

1. **Nota de investigación de dominio** (ya resuelta): [cordoba-railway-data.md](../../research/infrastructure/cordoba-railway-data.md). Esta spec ejecuta su estrategia.
2. **Modelo de dominio en `RailWeaver.Core`** (`RailWeaver.Core.Infrastructure`):
   - `TrackGauge` (record struct: milímetros + `TrackGaugeKind` — `Metre`/`Standard`/`Broad`/`Unknown`).
   - `TrackOperationalStatus` (`Active`, `Disused`, `Abandoned`).
   - `TrackUsage` (`MainLine`, `BranchLine`, `Siding`, `Yard`, `IndustrialSpur`, `Unknown`).
   - `TrackSegment` (id, geometría como lista de `GeoCoordinate`, trocha, estado, uso, nombre opcional, referencia de línea/ramal, **si la trocha fue leída del dato o inferida por ramal**).
   - `StationType` (`Station`, `Halt`, `Junction`) y `RailwayStation` (id, nombre, ubicación, tipo, trocha).
   - Validación: un `TrackSegment` requiere ≥ 2 coordenadas válidas no idénticas consecutivas; se rechaza en construcción, igual que hoy `GeoCoordinate`/`GeoBoundingBox`.
3. **Pipeline de extracción offline** (`tools/extract-osm-railway.py` o script equivalente documentado):
   - Consulta Overpass QL acotada al bounding box de Córdoba de RW-001 (`west -65.7720, south -35.0002, east -61.7708, north -29.5004`), para `way["railway"]` (`rail`, `disused`, `abandoned`, `narrow_gauge`) y `node["railway"~"station|halt"]`.
   - **Se guarda la respuesta cruda de Overpass tal cual llega**, sin editar, junto con fecha de descarga y la query exacta usada — para poder auditar o volver a derivar el dataset sin tener que bajar todo de nuevo ni depender de que OSM no haya cambiado.
   - Normalización de trocha: si el tag `gauge` está presente, se usa. Si falta, **se infiere por ramal/operador cuando la asignación es unívoca** (ex Belgrano → 1000 mm, ex Mitre/San Martín → 1676 mm) y el segmento queda marcado como `GaugeInferred = true`. Si no se puede inferir con confianza, `Unknown`. La tabla ramal → trocha vive embebida en el propio script de extracción (constante documentada, con comentario que cita [cordoba-railway-data.md](../../research/infrastructure/cordoba-railway-data.md) como fuente), revisable a mano si aparecen casos ambiguos.
   - **Tramo de doble trocha en Córdoba capital** (conexión Córdoba Mitre ↔ Alta Córdoba / Empalme Garita, por donde el Tren de las Sierras ingresa a la estación Mitre): la extracción debe inspeccionar cómo lo modela OSM (habitualmente dos `way` superpuestos con distinta trocha) y documentar el resultado junto al dataset. No es una decisión de diseño: es un hecho a confirmar contra el dato real al correr el pipeline, y el modelo de dominio ya lo soporta sin cambios (dos `TrackSegment` con geometría compartida y trocha distinta).
   - Normalización de estado operacional a `Active` / `Disused` / `Abandoned` según tags `railway=rail|disused|abandoned`.
   - Descarte de ruido de mapeo ajeno a la red real (maquetas, funiculares no ferroviarios, etc.), documentando cada regla de descarte.
4. **Persistencia versionada como dataset**, igual que `data/regions/cordoba.json`:
   - `data/regions/cordoba/tracks.geojson` y `data/regions/cordoba/stations.geojson`, separados: cada uno tiene su propia forma de datos y se valida contra un tipo del core distinto (`TrackSegment` vs. `RailwayStation`), igual que el core ya los modela como tipos separados.
   - `data/regions/cordoba/railway.source.json` (o similar) con: fecha de extracción, query Overpass, licencia (`ODbL-1.0`), atribución obligatoria, y referencia al archivo con la respuesta cruda.
   - Aviso de licencia ODbL explícito junto al dataset.
5. **Exposición en la API**: `GET /api/regions/{id}/railway` devuelve tramos y estaciones validados por el core, siguiendo el mismo patrón que `RegionFileStore`/`GET /api/regions/{id}` (archivo → validación en core → DTO de respuesta).
6. **Visualización en el frontend (CesiumJS)**, siguiendo `DESIGN.md`:
   - Capa `ExistingRailwayLayer`, activable/desactivable desde el control de capas existente.
   - Vías: **la infraestructura sigue siendo ink**, no un estado de desviación (`warning`/`alarm` es para deviaciones del plan, no para historia de la traza). `Active` en trazo sólido; `Disused`/`Abandoned` en `ink-muted`/`ink-subtle` con trazo punteado, distinguibles entre sí.
   - Estaciones: marcador sobrio consistente con el resto del sistema visual; click/tap muestra nombre, tipo, trocha y estado en un panel o tooltip.
   - Atribución en pantalla a OpenStreetMap / OpenRailwayMap (barra de estado, junto a la atribución ya existente de RW-001/RW-002).
7. **Trazabilidad de confianza**: cada tramo/estación expone si su trocha fue leída o inferida, para que la UI y futuras decisiones de planificación puedan distinguir dato confirmado de suposición.

## Non-goals

- Grafo topológico de ruteo (nodos de conexión, agujas navegables, cálculo de caminos): V0.5.
- Simulación o circulación de trenes sobre esta infraestructura.
- Modelado mecánico de aparatos de vía (máquina de cambio de agujas, estados F0).
- Elevación/pendiente de las vías: se dibujan sobre el elipsoide hasta V0.4.
- Datos de infraestructura fuera del bounding box de Córdoba adoptado en RW-001.
- Edición de la red desde la UI.
- Migración a PostGIS: el dataset sigue siendo un archivo versionado, simétrico a `data/regions/cordoba.json`.

## Inputs

- Extracto Overpass QL de OpenStreetMap para el bounding box de Córdoba (vías y estaciones ferroviarias).
- `data/regions/cordoba.json` existente (bounding box de referencia).

## Outputs

- `data/regions/cordoba/tracks.geojson` y `data/regions/cordoba/stations.geojson` + metadatos de fuente/licencia + respuesta cruda de Overpass archivada.
- `GET /api/regions/cordoba/railway` sirviendo el dataset validado.
- Capa `ExistingRailwayLayer` visible y conmutable en el viewer, con atribución y panel de metadatos por elemento.
- Tipos de dominio nuevos en `RailWeaver.Core.Infrastructure`.

## Acceptance criteria

- [ ] `TrackSegment` rechaza geometrías con menos de 2 puntos o puntos consecutivos idénticos; tests cubren ambos casos.
- [ ] La incompatibilidad de trocha entre tramos queda representada en el modelo (al menos como invariante documentado y verificado por test: dos trochas nominales distintas no se tratan como conectables sin una entidad explícita de transbordo/bitrocha).
- [ ] El archivo de datos de Córdoba carga y valida sin errores contra los tipos del core; un test lo confirma (igual que el test existente para `cordoba.json`).
- [ ] Todo segmento con trocha inferida (no leída del tag `gauge`) queda marcado como tal en el dato y es visible en la respuesta de la API.
- [ ] La respuesta cruda de Overpass usada para generar el dataset está versionada en el repo (o su ubicación/proceso de obtención documentado si su tamaño no lo permite), junto con la fecha y la query exacta.
- [ ] `GET /api/regions/cordoba/railway` responde 200 con tramos y estaciones; 404 si la región no existe, igual que `GET /api/regions/{id}`.
- [ ] El dataset y la respuesta de la API exponen atribución y licencia `ODbL-1.0` de forma explícita.
- [ ] El viewer muestra vías y estaciones de Córdoba, con distinción visual entre `Active`, `Disused` y `Abandoned` sin usar colores de estado (`warning`/`alarm`); la atribución a OpenStreetMap/OpenRailwayMap es visible en pantalla.
- [ ] Click/tap sobre un tramo o estación muestra sus metadatos básicos (nombre, tipo, trocha, estado, si la trocha fue inferida).
- [ ] Está documentado, con evidencia del dato real extraído, cómo OSM modela el tramo de doble trocha Córdoba Mitre / Alta Córdoba, y el dataset lo representa correctamente.
- [ ] `dotnet test`, `npm run lint`, `npm run build` pasan; CI verde.
- [ ] `project-status.md` y `CHANGELOG.md` actualizados; tag `v0.3.0`.

## Relevant domain docs

- [cordoba-railway-data.md](../../research/infrastructure/cordoba-railway-data.md)
- [ADR-007](../../decisions/ADR-007-cordoba-first-dataset.md) (Córdoba como dataset, no dependencia del core)

## Relevant architecture

- [Overview](../../architecture/overview.md): el dataset de Córdoba vive en `data/`, la API lo lee y valida contra el core, el core no conoce OSM ni Overpass. El adapter de extracción (`tools/extract-osm-railway.py`) es un script offline, no un servicio en tiempo de ejecución.
- [DESIGN.md](../../../DESIGN.md): infraestructura = ink; los estados operacionales de la traza (activa/desuso/abandonada) no son deviaciones de plan y no usan `warning`/`alarm`.

## Tests

- Unit tests de `TrackGauge`, `TrackSegment`, `RailwayStation` en `RailWeaver.Core.Tests` (validación de geometría, trocha, invariante de incompatibilidad de trocha).
- Test que carga el dataset real de Córdoba y valida su contenido contra los tipos del core (igual que el test existente de `cordoba.json`).
- Test de la API para `GET /api/regions/cordoba/railway` (200 con contenido esperado, 404 para región inexistente).
- Frontera del core: `CoreBoundaryTests` sigue verificando que `RailWeaver.Core` no referencia ASP.NET Core, EF Core, Npgsql ni ningún renderer.
