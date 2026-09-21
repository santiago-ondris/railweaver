# RW-002 — Migración de `src/web` al sistema visual

- Status: Active
- Milestone: transversal a M0 (antes de V0.3). Release objetivo: `v0.3.0` junto con el primer trabajo de V0.3, o `v0.2.1` si se publica sola.

## Goal

Que el frontend existente siga [`DESIGN.md`](../../../DESIGN.md) ([ADR-009](../../decisions/ADR-009-visual-system.md)) antes de que crezca, y dejar armado el circuito que mantiene alineados el sistema y el código:
- tokens generados desde `DESIGN.md`;
- fuentes autoalojadas;
- marca y favicon;
- lint del sistema en CI.

## Scope

1. **Tokens desde `DESIGN.md`**
   - `src/web/src/styles/tokens.generated.css`, generado con el CLI oficial (`@google/design.md` con versión fija) usando `export --format css-vars --prefix rw`.
   - El archivo generado se commitea. Un script `npm run tokens` lo regenera.
   - El export solo cubre colores, espaciado y radios. La tipografía se declara a mano en `src/web/src/styles/typography.css`, como variables que nombran los tokens de `DESIGN.md` (`--rw-font-ui`, `--rw-text-label-size`, etc.).
2. **Fuentes autoalojadas**
   - Schibsted Grotesk (variable), Fragment Mono y Big Shoulders Stencil, vía paquetes de Fontsource (OFL-1.1).
   - Sin requests a Google Fonts.
   - Solo los pesos y subsets latinos que se usan.
3. **Estilos de la app reescritos contra los tokens**
   - `index.css` pasa a usar solo variables `--rw-*`, sin colores ni fuentes literales.
   - Se eliminan el fondo oscuro, Inter, el glassmorphism (`backdrop-filter`), las sombras en paneles, los radios de 7–12 px y el badge en forma de pill.
4. **Shell según DESIGN.md › Layout, solo con lo que existe hoy**
   - Barra superior: marca + región activa.
   - Riel lateral: el control de capas actual.
   - Mapa.
   - Barra de estado: CRS y atribución.
   - No se agregan las fases del loop, el panel de detalle ni el dock de análisis: todavía no hay funcionalidad detrás.
5. **Marca**
   - Wordmark + `railweaver-mark.svg` en la barra superior, reemplazando el cuadrado "RW".
   - `favicon.svg` en `index.html`.
   - Fuente única en `assets/brand/`: no se commitean copias dentro de `src/web`.
6. **Mapa base neutro.** Se mantienen las teselas de OpenStreetMap de RW-001 y se neutralizan con las propiedades de `ImageryLayer` de Cesium (saturación, brillo, contraste y gamma) hasta aproximar `map-ground`. Ver la decisión 3.
7. **Contorno de región con la semántica del sistema.** El bounding box deja de ser ámbar, porque el ámbar significa advertencia. Pasa a `primary`: trazo fino, sin relleno o con relleno mínimo.
8. **Estados de carga y error del visor**
   - Carga: texto y un indicador sobrio, sin animación decorativa (se permite la mínima necesaria, respetando `prefers-reduced-motion`).
   - Error: patrón de alerta con glifo cuadrado, qué pasó y cómo resolverlo.
9. **CI**
   - Job que corre `design.md lint` (versión fija) y falla si hay errores.
   - Chequeo de que `tokens.generated.css` está al día (regenerar y comparar con `git diff --exit-code`).

## Non-goals

- Funcionalidad nueva: fases, panel de detalle, gráficos, perfil o coordenadas del cursor.
- Tema oscuro, librería de íconos, librería de gráficos, Tailwind o librerías de UI.
- Cambiar de proveedor de mapa base o pasar a teselas vectoriales (ver la decisión 3).
- Relieve o hillshade (depende del DEM de V0.4).
- Diseño responsive por debajo de 1180 px. Solo se evita que se rompa: sin scroll horizontal del documento y con controles alcanzables.
- Tests de frontend con Vitest: no hay lógica nueva que testear.

## Inputs

- `DESIGN.md`, `assets/brand/*.svg`.
- El frontend actual de RW-001 (`App.tsx`, `GeographicViewer.tsx`, `index.css`).

## Outputs

- La app abre con el shell claro del sistema, la marca, las tipografías propias y el mapa neutralizado.
- `tokens.generated.css`, `typography.css`, el script `tokens` y el job de CI del sistema de diseño.

## Acceptance criteria

- [ ] `index.css` y los componentes no contienen colores literales (hex, rgb, hsl u oklch) ni familias tipográficas literales fuera de `styles/`. Se verifica con `grep`. Excepción documentada: los colores que Cesium necesita como `Color`, que se leen de las variables CSS en tiempo de ejecución o se centralizan en un solo módulo con referencia al token.
- [ ] `npm run tokens` regenera `tokens.generated.css` sin diferencias respecto del commit.
- [ ] CI: `design.md lint` sin errores y chequeo de tokens al día.
- [ ] La red del navegador no muestra requests a `fonts.googleapis.com` ni a `fonts.gstatic.com`.
- [ ] La barra superior muestra la marca y el wordmark, y el favicon es `favicon.svg`. `assets/brand/` sigue siendo la única copia versionada de la marca.
- [ ] El mapa base se ve neutro y la atribución de OpenStreetMap sigue visible y legible.
- [ ] El contorno de región no usa colores de estado.
- [ ] El texto pasa WCAG AA sobre sus superficies y el foco de teclado es visible en todos los controles.
- [ ] Carga y error: el de error usa el patrón de alerta (glifo cuadrado + qué pasó + cómo resolverlo) y nada anima con `prefers-reduced-motion`.
- [ ] La auditoría manual contra `DESIGN.md › Do's and Don'ts` no encuentra violaciones. Hacerla con un modelo distinto al que implementó (`agent-workflow.md` §7).
- [ ] `dotnet test`, `npm run lint` y `npm run build` pasan, y el CI queda en verde.
- [ ] `project-status.md` y `CHANGELOG.md` actualizados.

## Relevant domain docs

- [ADR-009](../../decisions/ADR-009-visual-system.md), [ADR-005](../../decisions/ADR-005-cesiumjs-primary-renderer.md).
- [Nota de semántica de color](../../research/visual-identity/color-semantics-and-heritage-lettering.md).
- [RW-001](../completed/RW-001-geographic-viewer.md) (decisiones sobre las teselas OSM y la política de uso).

## Relevant architecture

- [Overview](../../architecture/overview.md): cambio acotado a `src/web`. El core y la API no se tocan.

## Tests

- Sin tests automatizados nuevos de frontend (ver Non-goals).
- Verificación por CI (lint del sistema, tokens al día, lint y build del frontend) y verificación manual en el navegador contra los criterios de aceptación.

## Decisions (confirmadas por el autor el 2026-09-21)

1. **Cadena de tokens: export oficial más tipografía a mano.**
   - `@google/design.md export --format css-vars` genera colores, espaciado y radios con el prefijo `rw`. Convierte OKLCH a hex, lo que funciona en todos los navegadores objetivo.
   - La tipografía no se exporta (v0.4.0), así que se mantiene a mano en un solo archivo que referencia los nombres de `DESIGN.md`.
   - Alternativa descartada: un script propio que parsee el YAML. Es más código para mantener, mientras el CLI oficial ya lo resuelve casi todo.
2. **Fuentes con Fontsource.**
   - Los paquetes `@fontsource-variable/schibsted-grotesk`, `@fontsource/fragment-mono` y `@fontsource-variable/big-shoulders-stencil` son dependencias justificadas por ADR-009 (autoalojar, sin terceros en runtime).
   - Alternativa descartada: copiar los `.woff2` a mano, que se desactualizan sin aviso.
3. **Mapa base: neutralizar OSM en vez de cambiar de proveedor.**
   - `ImageryLayer` de Cesium permite bajar la saturación y ajustar brillo, contraste y gamma sin tocar la configuración de RW-001 ni su política de uso.
   - Es reversible y no suma cuentas ni tokens.
   - El mapa base definitivo (vectorial o propio, con relieve) se decide con el DEM de V0.4 o antes de un despliegue público, como ya prevé RW-001.
4. **Marca desde `assets/brand/`.** Se reutiliza `vite-plugin-static-copy`, que ya existe, o un import de Vite, para servir la marca y el favicon sin duplicar archivos. El mecanismo exacto se elige al implementar y se documenta en esta spec.
5. **CI del sistema de diseño.**
   - `design.md lint` y el chequeo de tokens van en el job `frontend`, o en uno nuevo, con `npx` y versión fija.
   - `impeccable detect` no entra en esta spec: se prueba una vez de forma manual durante la auditoría, y solo se suma al CI si encuentra problemas reales que el lint no ve.

## Open questions

- Valores exactos de saturación, brillo, contraste y gamma del mapa base: se ajustan a ojo contra `map-ground` y se registran acá.
- Si la atribución de OSM y los créditos de Cesium se estilan dentro del widget de Cesium o se mueven a la barra de estado (debe seguir cumpliendo la atribución requerida).
