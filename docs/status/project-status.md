# RailWeaver Status

Last updated: 2026-09-23 (RW-007 completada, v0.7.0)

## Current milestone

M0 — Real World Skeleton, completada con V0.5 — Candidate corridor prototype
([RW-005](../specs/completed/RW-005-candidate-corridor.md)). Release `v0.5.0`.

M1 — Primer tren, en curso. V0.7 completada con
[RW-007 — Curvas](../specs/completed/RW-007-curvature.md). Siguiente: RW-008.
El autor comprobó Córdoba → Nono con parámetros de montaña y de pasajeros,
confirmó curvas naturales, rótulos legibles en las Altas Cumbres y el progreso y
los errores visibles durante la búsqueda.

## Working

- Solución .NET 10: `RailWeaver.Core`, `RailWeaver.Api` (health, regiones,
  infraestructura ferroviaria, red, rutas, elevación, perfiles y terreno),
  `RailWeaver.Core.Tests` y `RailWeaver.Api.Tests` (136 tests, incluye frontera del
  core, formatos, datasets y endpoints).
- Frontend React + TS + Vite en `src/web`, con visor CesiumJS, OpenStreetMap neutralizado, cámara inicial, red ferroviaria de Córdoba y capas controlables a partir de datasets servidos por la API. Sigue `DESIGN.md`: tokens generados, fuentes autoalojadas, marca y favicon. El código se organiza por módulos alineados con el backend y lo formatea Prettier (ADR-010).
- Dataset ferroviario OSM de Córdoba versionado: 2.374 tramos, 169 estaciones, respuesta cruda de Overpass, metadatos y licencia ODbL 1.0; extractor offline reproducible en `tools/`.
- PostGIS 3.5 / PostgreSQL 17 vía `compose.yaml` (sin uso desde el código todavía).
- Documentación canónica, ADR-001…010, plantillas de specs e investigación.
- Sistema visual definido en `DESIGN.md` (ADR-009): dirección híbrida, tema claro, tokens OKLCH; aplicado a `src/web` en RW-002. Marca (logo) en `assets/brand/`.
- Elevación determinista en el core; formato DEM por bloques zlib y caché acotada en
  la API; endpoints de cota, perfil y terreno; relieve Cesium, cota por clic y perfil
  longitudinal en el frontend.
- Búsqueda de corredores en `RailWeaver.Core.Planning` con A*, malla de 16 vecinos,
  pendiente estricta, radio mínimo por trocha y métricas; endpoint HTTP y herramienta
  en el visor. La búsqueda arma rectas y arcos, velocidades por curva y rasante con
  compensación de pendiente.
- RW-006 completada: el core construye una red
  por trocha, deduce 504 tramos sin dato por conexión, segmenta 3.953 aristas,
  calcula diagnósticos y encuentra rutas entre estaciones con inversiones de
  marcha contadas. La API expone resumen/rutas y agrega el perfil del terreno si
  está disponible. El frontend incorpora herramienta Ruta y capa Diagnóstico de red.
  Los nueve recorridos de referencia y los conteos del diagnóstico coinciden con
  la spec; pasan 128 tests .NET, lint, formato y build del frontend. El autor
  confirmó el funcionamiento en el visor. El CI de `main` terminó en verde.
- RW-007 completada: los diez casos de referencia con DEM real coinciden con la
  spec y respondieron en ≤ 4,42 s. La verificación visual del autor incluyó casos
  válidos e inválidos, curvas, controles y mensajes de progreso y error. La prueba
  de tiempo de topología de RW-006 falló intermitentemente en Debug (~1,1 s contra
  un umbral de 1 s) durante la medición y pasó al repetirla.

## Next

M1 — Primer tren: diseñar un ramal, conectarlo a la red existente y ver a un tren
recorrerlo, con un tiempo de viaje que se pueda explicar ([Kickoff](../../RailWeaver_Project_Kickoff.md) §38–39).
Pasos propuestos, con intenciones cortas en [`specs/backlog/`](../specs/backlog/):

1. RW-008 — Tren V1 y tiempo de recorrido (V0.8).
2. RW-009 — Motor de simulación por eventos y primera circulación (V0.9).
3. RW-010 — Construir: corredor unido a la red mediante un empalme (V0.10).

RW-007 es una aproximación territorial de fidelidad V2: no incluye clotoides ni
proyecto ejecutivo de obras o costos de suelo.

## Known problems

- La imagen oficial `postgis/postgis` es solo amd64: en Apple silicon corre emulada (ADR-003).
- El mapa base es OSM neutralizado, no un mapa base propio (RW-002, decisión 3). El contorno de región puede mostrar cortes mínimos a algunas distancias.
- El bundle inicial de CesiumJS es grande (aprox. 4,36 MB minificado / 1,18 MB gzip); se optimizará cuando exista una medición de carga representativa.
- OSM representa 40 `way` con `gauge=1000;1676`; alrededor de Córdoba Mitre hay evidencia bitrocha, pero el etiquetado actual no forma una continuidad bitrocha completa hasta Alta Córdoba.
- Un corredor excepcionalmente largo exploró unos 5 millones de estados y tardó
  aproximadamente 300 s en la máquina del autor. Los diez casos de referencia de
  RW-007 tardaron ≤ 4,42 s; queda perfilar y optimizar búsquedas extensas si se
  vuelven un caso de uso habitual.

## Open research questions

- Aspectos de señal del RITO vigente y licencia de FC Nefa ([nota](../research/visual-identity/color-semantics-and-heritage-lettering.md)).

## Open decisions (sin ADR todavía)

- Acceso a PostGIS desde .NET (Npgsql directo, Dapper, EF Core + NetTopologySuite) — previsto en RW-010, solo si guardar la red propuesta en archivos o en la sesión no alcanza.
- Framework de tests de frontend (Vitest) — previsto en RW-009, cuando aparezca la lógica de animación y línea de tiempo.
- Licencia del proyecto.

## Recent architectural decisions

- ADR-001 Modular monolith · ADR-002 .NET 10 · ADR-003 PostgreSQL/PostGIS · ADR-004 React/TS/Vite · ADR-005 CesiumJS · ADR-006 Discrete-event simulation · ADR-007 Córdoba como dataset · ADR-008 Convención de idioma · ADR-009 Sistema visual · ADR-010 Estructura y formato del frontend.
