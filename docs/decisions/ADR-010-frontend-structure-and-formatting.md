# ADR-010 — Estructura del frontend por módulos y formato con Prettier

- Status: Accepted
- Date: 2026-09-22

## Context

Después de RW-005, `src/web/src` tenía todos los archivos en la raíz. No había un criterio para ubicar clientes HTTP, capas de Cesium y componentes. `GeographicViewer.tsx` concentraba unas 390 líneas y cerca de 30 estados y refs: la inicialización de Cesium, las herramientas de perfil y de corredor, los paneles y las etiquetas. `index.css` reunía en 545 líneas los estilos de todas las pantallas. Tampoco había formatter, así que cada agente escribía con su propio estilo: líneas de más de 200 caracteres y varias sentencias por línea.

El backend ya se organiza por módulo funcional (`Regions`, `Railways`, `Elevation`, `Planning`) tanto en `RailWeaver.Api` como en `RailWeaver.Core`. Como varios agentes trabajan sobre el front, la convención tiene que estar escrita.

## Decision

1. **Carpetas por módulo, con los mismos nombres que el backend.** Dentro de `src/web/src`:

   | Carpeta | Contenido |
   |---|---|
   | `app/` | Shell de la aplicación: `App.tsx` y su CSS. `main.tsx` queda en la raíz porque `index.html` lo referencia. |
   | `viewer/` | Visor geográfico: orquesta el viewer de Cesium, enruta clics y teclas a la herramienta activa y arma el layout. `cesiumSetup.ts` concentra la creación del viewer, el mapa base, el terreno y la cámara. |
   | `regions/`, `railways/`, `elevation/`, `planning/` | Un módulo por área funcional. Cada uno contiene su cliente y sus contratos HTTP (`api.ts`), sus capas de Cesium, componentes y hooks, y su CSS. |
   | `shared/` | Tipos y componentes que usan varios módulos, como `Coordinate` o `DetailRow`. Solo entra lo que ya se usa en más de un módulo. |
   | `styles/` | Tokens de `DESIGN.md`, tipografía, base global y componentes compartidos del sistema visual (alertas, botones, panel de detalle, dock de análisis). |

2. **Nombres de archivo:**
   - Componentes React y clases en `PascalCase.tsx` / `PascalCase.ts`.
   - Hooks en `useCamelCase.ts`.
   - Funciones y utilidades en `camelCase.ts`.
   - El cliente HTTP de cada módulo se llama `api.ts`.
3. **CSS junto al componente.** Cada componente con estilos propios importa su `Componente.css`. Las primitivas compartidas se importan una sola vez desde `main.tsx`. La regla de ADR-009 no cambia: solo tokens `--rw-*`, sin colores ni fuentes literales.
4. **Estado de herramientas en hooks.** Cada herramienta del visor (`useProfileTool`, `useCorridorTool`) es dueña de su estado, sus entidades en el mapa y sus llamadas a la API. El visor solo coordina la exclusión mutua entre herramientas y el enrutamiento de eventos de Cesium.
5. **Dependencias entre módulos:**
   - Los módulos funcionales pueden usar `shared/` y `styles/`.
   - `viewer/` puede usar todos los módulos.
   - Un módulo funcional solo depende de otro para leer sus tipos, como `planning` con `Coordinate` o `elevation` con los perfiles.
6. **Prettier como formatter**, con la versión fijada en `package.json`. Configuración en `src/web/.prettierrc.json`:
   - Sin punto y coma y con comillas simples, que era el estilo que ya tenía el código.
   - 100 columnas.
   - `endOfLine: auto`, porque los checkouts en Windows usan `core.autocrlf`.

   Scripts `npm run format` y `npm run format:check`. CI ejecuta `format:check`. `tokens.generated.css` queda excluido porque CI lo compara contra una exportación nueva de `DESIGN.md`.

## Consequences

- Una funcionalidad nueva del front va en el módulo del backend con el que se comunica. Si no encaja en ninguno, se crea un módulo nuevo con el mismo nombre que tenga en el backend.
- Formatear deja de ser una decisión de cada agente: se corre `npm run format` y listo. Los diffs se revisan más fácil.
- La reorganización no cambió comportamiento. Como el front no tiene tests automáticos, la separación en hooks se verificó con typecheck, lint y build, y el autor la revisa a mano.
- Se eliminaron código y estilos sin uso (`terrainAsElevationProfile` y `.viewer-error`).

## Alternatives considered

- **Carpetas por tipo técnico** (`components/`, `hooks/`, `api/`): separan código que cambia junto y no se corresponden con los módulos del backend.
- **oxfmt** (formatter del mismo toolchain que oxlint): es más rápido y compatible con Prettier, pero más nuevo. Prettier es el estándar y formatea TS, TSX, CSS y JSON. Se puede reevaluar cuando oxfmt madure.
- **CSS Modules o CSS-in-JS:** agregan tooling o dependencias sin un problema concreto de colisiones de clases. Las clases globales con prefijo por componente alcanzan por ahora.
