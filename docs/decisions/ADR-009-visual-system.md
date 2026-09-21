# ADR-009 — Sistema visual: dirección híbrida, tema claro y DESIGN.md

- Status: Accepted
- Date: 2026-09-21

## Context

El frontend arrancó sin una línea visual definida: `src/web` usa fondo oscuro e Inter, justo la estética genérica que producen los agentes por defecto. Varios agentes van a trabajar sobre la UI y necesitan una referencia única y legible por máquina para no derivar cada uno hacia su propio estilo.

Se investigaron herramientas para dar criterio visual a los agentes:
- Formatos: `DESIGN.md` de Google Labs y `PRODUCT.md` de *impeccable*.
- Generadores: *hue* y generadores de tokens en OKLCH con control de contraste APCA.
- Auditorías de patrones genéricos: *impeccable* y *avoid-ai-design*.
- Referencias de dominio: HMI de alto rendimiento (ISA-101), dibujo técnico ferroviario (perfil longitudinal, gráfico de marcha), el patrón de paneles de *Rail Route* y la tipografía histórica de los ferrocarriles argentinos.

Se prototiparon tres direcciones sobre la misma pantalla (fase Planificar, corredor Córdoba → Alta Gracia):
- **A · Sala de control:** HMI gris, color solo para desvíos.
- **B · Plano técnico:** papel milimetrado, tinta azul marino, rótulo.
- **C · Híbrida.**

## Decision

1. **Dirección visual C (híbrida):**
   - La base y la disciplina de color vienen de A: lo normal es gris y el color se reserva para estados y servicios.
   - Los gráficos y el rótulo vienen de B.
   - El patrimonio argentino aparece solo como letra stencil en identificadores y mojones kilométricos.
2. **Tema claro como principal.** No se implementa tema oscuro hasta que una spec lo pida; los roles de tokens lo permiten sin rediseñar.
3. **Tipografías (SIL OFL, autoalojadas al implementarse):**
   - Schibsted Grotesk para la UI.
   - Fragment Mono para los datos.
   - Big Shoulders Stencil para los identificadores.
4. **`DESIGN.md` en la raíz** con el formato de Google Labs (versión alpha) como fuente única del sistema visual:
   - Tokens en OKLCH en el front matter y reglas de uso en el cuerpo.
   - Está en inglés porque es un archivo para agentes (ADR-008).
   - Se valida con `npx @google/design.md lint DESIGN.md`.
   - Las advertencias `orphaned-tokens` en colores de mapa, servicios y bordes son esperadas, porque esos tokens no se asocian a componentes.
5. **Semántica de color:**
   - Advertencia: ámbar con triángulo.
   - Alarma: rojo con cuadrado.
   - Acción requerida: azul con rombo.
   - El verde no se usa para "éxito", porque en ferrocarril significa vía libre.
   - Los aspectos reales de señal solo aparecerán dentro de símbolos de señal, después de investigarlos. Ver [la nota de investigación](../research/visual-identity/color-semantics-and-heritage-lettering.md).

## Consequences

- Los agentes leen `DESIGN.md` antes de tocar `src/web` (referenciado desde `AGENTS.md`).
- `src/web` queda desalineado hasta una spec de migración. Esa spec tiene que resolver:
  - Cómo generar las variables CSS desde `DESIGN.md`.
  - Cómo autoalojar las fuentes.
  - Qué mapa base neutro reemplaza las teselas raster de OpenStreetMap adoptadas en RW-001.
- El logo queda pendiente y se diseñará a partir de este sistema.
- `DESIGN.md` está en alpha: si el formato cambia, se adapta el archivo sin reabrir esta decisión.
- Sumar auditorías automáticas en CI (`design.md lint`, `impeccable detect`) queda para la spec de migración, cuando haya código que auditar.

## Alternatives considered

- **Tema oscuro como principal:** la "sala de control oscura" es más un cliché visual que una práctica real (ISA-101 recomienda gris claro). Además, el autor usa herramientas claras a diario y el relieve se lee mejor en claro.
- **Dirección A pura:** creíble, pero sin lenguaje propio para perfiles y gráficos de marcha.
- **Dirección B pura:** muy propia del proyectista, pero con menos disciplina de estados para simulación y operación.
- **Patrimonio más explícito** (colores nacionales, logotipos históricos): descartado para no caer en la caricatura.
- **Generar el sistema con *hue* o *UI/UX Pro Max*:** parten de una marca o de estilos genéricos. RailWeaver necesitaba un lenguaje derivado de su dominio.
- **FC Nefa como tipografía de identificadores:** preferida conceptualmente, pero sin licencia confirmada.
