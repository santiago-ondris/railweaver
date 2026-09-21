# Sistemas de referencia de coordenadas para RailWeaver

## Question

¿Qué sistema de referencia debe usar RailWeaver en sus fronteras, qué papel tienen
POSGAR 2007 y las proyecciones Gauss-Krüger argentinas, y qué extensión debe usar
RW-001 para encuadrar inicialmente la provincia de Córdoba?

## Sources

- Instituto Geográfico Nacional (IGN), [POSGAR 07](https://www.ign.gob.ar/NuestrasActividades/Geodesia/Posgar07), consultado el 2026-09-21. Describe la adopción de POSGAR 2007 como marco geodésico nacional, su vínculo con IGS05/SIRGAS y el uso del elipsoide WGS84.
- IGN, [Sistemas de proyección](https://www.ign.gob.ar/NuestrasActividades/ProduccionCartografica/sistemas-de-proyeccion), consultado el 2026-09-21. Describe las siete fajas Gauss-Krüger oficiales, separadas cada 3°.
- IGN, [Definición de sistemas de coordenadas y proyecciones oficiales en la República Argentina (EPSG)](https://ramsac.ign.gob.ar/posgar07_pg_web/documentos/Informe_sobre_codigos_oficiales_EPSG.pdf), consultado el 2026-09-21. Identifica POSGAR 2007 geográfico como EPSG:5340 y sus proyecciones por faja.
- IGN, [metadatos de la capa Provincia](https://www.ign.gob.ar/capas-sig/metadata/provincia.pdf), consultado el 2026-09-21. Declara geometría poligonal, referencia EPSG:4326, actualización mensual y libre uso con cita al IGN.
- IGN, [servicio_fondos, capa Provincias](https://ide.ign.gob.ar/geoservicios/rest/services/servicio_fondos/MapServer/0), consultado el 2026-09-21. El registro con código INDEC `14` identifica a Córdoba. Una [consulta de extensión](https://ide.ign.gob.ar/geoservicios/rest/services/servicio_fondos/MapServer/0/query?where=IN1%3D%2714%27&returnExtentOnly=true&outSR=4326&f=json) en EPSG:4326 devolvió oeste `-65.771983796999962`, sur `-35.000134763999938`, este `-61.770893426999962` y norte `-29.500422581999938`.
- IETF, [RFC 7946 — The GeoJSON Format](https://www.rfc-editor.org/rfc/rfc7946), agosto de 2016. Define GeoJSON sobre WGS84, en grados decimales, con posiciones en orden longitud-latitud y bounding boxes en orden oeste-sur-este-norte.
- IGN, [marco legal institucional](https://www.ign.gob.ar/AreaInstitucional/MarcoLegal/Leyes), consultado el 2026-09-21. Registra POSGAR 2007 como marco planimétrico nacional y RN-Ar como marco altimétrico nacional.

## Real-world behavior

Un sistema de referencia geográfico expresa una posición mediante ángulos sobre un
elipsoide. Es apropiado para intercambio de datos y visualización global, pero sus
grados no son unidades lineales uniformes: un grado de longitud representa distintas
distancias según la latitud.

WGS84 es el sistema global utilizado por GPS, GeoJSON y herramientas de visualización
web. EPSG:4326 identifica su CRS geográfico 2D. GeoJSON usa la variante OGC:CRS84 del
mismo datum para fijar el orden longitud-latitud. En formatos que usan arreglos, el
orden de ejes puede ser una fuente de errores: GeoJSON exige `[longitud, latitud]`,
aunque otras definiciones formales o APIs pueden presentar los ejes de otro modo.

POSGAR 2007 (EPSG:5340) es el marco de referencia geodésico oficial de Argentina.
Está materializado en el territorio, vinculado a IGS05/SIRGAS y usa el elipsoide
WGS84. Para cartografía y cálculos planos, Argentina usa proyecciones Gauss-Krüger
en siete fajas de 3°:

- Faja 3, EPSG:5345, meridiano central -66°.
- Faja 4, EPSG:5346, meridiano central -63°.

La extensión de Córdoba publicada por el IGN va aproximadamente desde -65.77° hasta
-61.77° de longitud. Por lo tanto, la provincia cruza el límite nominal de -64.5°
entre las fajas 3 y 4. La faja 4 cubre la mayor parte de Córdoba, pero no es una
proyección única correcta por definición para todo trabajo dentro de la provincia.

La referencia vertical es un problema distinto del CRS horizontal. Una altura GPS
sobre el elipsoide WGS84 no equivale automáticamente a una cota sobre el nivel medio
del mar. Argentina posee el marco altimétrico RN-Ar y modelos de geoide; el dataset
DEM de V0.4 deberá declarar qué altura entrega y qué transformaciones requiere.

## RailWeaver abstraction

### Fronteras del sistema y viewer

- Usar coordenadas geográficas WGS84/EPSG:4326 en grados decimales para el core, los
  archivos de región, la API y la integración con Cesium.
- Modelar las coordenadas con campos nombrados `latitude` y `longitude`; no exponer
  pares numéricos cuyo orden sea implícito.
- Si posteriormente se importa o exporta GeoJSON, adaptar explícitamente al orden
  `[longitude, latitude]` definido por RFC 7946.
- Representar el bounding box con campos nombrados `west`, `south`, `east`, `north`.
  Para RW-001, rechazar cajas que crucen el antimeridiano (`west > east`). Córdoba no
  necesita ese caso y el rechazo explícito evita interpretar una caja pequeña como
  una caja de casi todo el mundo.

### Bounding box inicial de Córdoba

Usar la capa oficial de provincias del IGN, registro INDEC `14`, y redondear su
extensión hacia afuera a cuatro decimales:

| Campo | Valor |
|---|---:|
| `west` | -65.7720 |
| `south` | -35.0002 |
| `east` | -61.7708 |
| `north` | -29.5004 |

Cuatro decimales representan un orden de magnitud cercano a diez metros en latitud.
Esto es mucho más preciso de lo necesario para encuadrar una provincia y evita que
los numerosos decimales del servicio aparenten una exactitud no justificada. El
redondeo hacia afuera garantiza que la caja siga conteniendo la geometría consultada.
La caja es una ayuda de navegación, no el límite legal ni un polígono provincial.

El archivo de región debe citar al Instituto Geográfico Nacional, registrar la URL
del servicio y la fecha de consulta. La licencia indicada por los metadatos permite
el libre uso con cita adecuada al IGN.

### Cálculos futuros

- Para distancias preliminares entre puntos o sobre corredores extensos, preferir
  algoritmos geodésicos sobre el elipsoide antes que tratar grados como metros.
- Para ingeniería local, áreas, offsets o cálculos planos, transformar los datos a
  un CRS proyectado apropiado para el área concreta. Elegir automáticamente una faja
  Gauss-Krüger solo es válido si toda el área de trabajo y la precisión requerida lo
  permiten.
- Un corredor que cruce una frontera de faja necesita una estrategia explícita:
  cálculo geodésico, una proyección local adecuada o tratamiento por segmentos. Esa
  decisión pertenece a la futura spec de routing, no a RW-001.
- Las pendientes no deben calcularse hasta conocer tanto la distancia horizontal
  como el datum vertical y la calidad del DEM.

## Safety invariants

- Nunca calcular distancias, áreas o pendientes interpretando diferencias de grados
  como metros.
- Todo dataset debe declarar CRS horizontal; si contiene elevación, también debe
  declarar datum vertical y unidad.
- Toda transformación debe declarar CRS de origen y destino y debe poder probarse
  con coordenadas conocidas.
- No intercambiar silenciosamente latitud y longitud.
- Una bounding box de navegación no puede presentarse como límite administrativo o
  legal.

## Simplifications

- RW-001 soportará únicamente bounding boxes que no crucen el antimeridiano.
- No se implementarán transformaciones POSGAR/Gauss-Krüger en RW-001.
- WGS84/EPSG:4326 se tratará como suficiente para la precisión visual e intercambio
  de esta milestone. No implica equivalencia centimétrica entre todas las
  realizaciones históricas de WGS84 y POSGAR 2007.
- El bounding box de Córdoba se deriva del polígono del IGN, pero RW-001 no incluirá
  ni dibujará ese polígono: mostrará solamente su rectángulo envolvente.

## Unknowns

- Qué algoritmo geodésico y biblioteca usará el routing para longitudes de corredor.
- Qué CRS proyectado o estrategia multi-faja requerirán los cálculos ferroviarios de
  mayor precisión.
- Qué datum vertical y precisión ofrece el DEM que se elija para V0.4.
- Si una futura importación de datos históricos necesitará transformar desde
  POSGAR 94 o Campo Inchauspe 69.

## Confidence

**HIGH** para la elección de WGS84 en fronteras, el carácter oficial de POSGAR 2007,
los códigos de las fajas y la extensión inicial de Córdoba: todos provienen de IGN,
IETF o registros EPSG citados. **MEDIUM** para la estrategia de cálculos futuros,
porque depende de requisitos de precisión, escala del corredor y datasets que aún no
fueron elegidos.
