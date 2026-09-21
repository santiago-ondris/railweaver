# Semántica de color de la UI y tipografía patrimonial

Nota de apoyo a [ADR-009](../../decisions/ADR-009-visual-system.md) y a [`DESIGN.md`](../../../DESIGN.md).

## Question

1. ¿Qué dicen las prácticas de HMI industrial (ISA-101) sobre el uso del color, y cuánto de eso es norma y cuánto práctica de la industria?
2. ¿Cómo evitamos que los colores de estado de la UI choquen con el significado real de los aspectos de señal ferroviaria?
3. ¿Se puede usar la tipografía ferroviaria argentina rescatada (FC Nefa y relacionadas) en RailWeaver?

## Sources

- *Going Gray: A New HMI Standard*, control.com. https://control.com/technical-articles/going-gray/ (secundaria).
- *ISA-101: the grey screen that finds faults faster*, LADX. https://ladx.ai/resources/isa-101-hmi-design (secundaria).
- *ISA-101 HMI Design Standard: A Guide*, HMI Library. https://hmilibrary.com/standards/isa-101 (secundaria; distingue lo normativo de su propia interpretación).
- ANSI/ISA-101.01 no fue consultado directamente: es un documento pago.
- *Reglamento Interno Técnico Operativo* (RITO), publicado por el Estado nacional: https://www.argentina.gob.ar/sites/default/files/rito.pdf (primaria; en esta sesión no se pudo extraer el texto del PDF).
- Resumen del RITO básico, ccbrailroad.com.ar: https://www.ccbrailroad.com.ar/rito/ritobase.html (secundaria).
- *Señalización ferroviaria argentina*, Wikipedia (secundaria).
- Ares, F. y Osores, O. *Tipografía histórica ferroviaria. Estudio y rescate del patrimonio tipográfico argentino*, UNLP. https://papelcosido.fba.unlp.edu.ar/ojs/index.php/aei/article/view/514/913
- *La tipografía de los ferrocarriles argentinos, una historia por recuperar*, Gràffica. https://graffica.info/la-tipografia-de-los-ferrocarriles-argentinos/
- Ficha de FC Nefa en Behance (Fabio Ares): https://www.behance.net/gallery/15826259/Tipografia-Historica-Ferroviaria-FCNefa (no se pudo abrir: 403).
- Catálogo comercial de Fabio Ares (p. ej. *Provincial Railway* en Fontspring): https://www.fontspring.com/fonts/fabio-ares/provincial-railway

## Real-world behavior

**HMI industrial.**
- Las fuentes secundarias coinciden en el enfoque de ISA-101 y del ASM Consortium: el estado normal va en grises apagados sobre fondo gris claro (ni blanco ni oscuro), y el color se reserva para condiciones anormales y cambios de estado.
- También coinciden en que el color nunca va solo: se acompaña de forma, posición o texto, por la deficiencia de visión del color.
- Las fuentes **no coinciden** en si la norma fija colores concretos por prioridad de alarma. Una atribuye a la norma el mapeo P1–P4 (rojo, naranja, amarillo, azul). Otra aclara que ISA-101 no especifica colores y que ese mapeo es práctica habitual (ASM, EEMUA 201, *High Performance HMI Handbook*).
- Ninguna fuente define valores RGB normativos.

**Señales ferroviarias argentinas.**
- El RITO clasifica las señales fijas en mecánicas (semáforos de brazo) y luminosas.
- Según fuentes secundarias, en las luminosas el rojo indica peligro, el amarillo precaución y el verde vía libre.
- Existen aspectos compuestos y señales de maniobra cuyo detalle no se pudo verificar en esta sesión.

**Tipografía patrimonial.**
- El proyecto *Tipografía Histórica Ferroviaria* (Ares y Osores, desde 2012) rescata letras de ferrocarriles argentinos.
- FC Nefa sale de planos de Ferrocarriles Argentinos (1976–1983). Es una palo seco geométrica modular en mayúsculas, con versión stencil.
- El proyecto declara la intención de distribuir las fuentes gratis entre organizaciones vinculadas al ferrocarril.
- Parte del catálogo del autor se vende comercialmente.
- No se encontró una licencia publicada para FC Nefa.

## RailWeaver abstraction

- **Gris es normal; color es desvío o servicio.** Tomamos la filosofía y no pretendemos cumplir ISA-101: RailWeaver no es un HMI de proceso real.
- **Estados propios con forma fija:** advertencia (ámbar, triángulo), alarma (rojo, cuadrado), acción requerida (azul, rombo), deshabilitado (círculo contorneado). Es una convención de RailWeaver y no está tomada de la norma.
- **El verde no significa "éxito" en la UI.** En ferrocarril el verde significa vía libre; usarlo como "guardado OK" mezclaría dos lenguajes. La confirmación usa tinta, un glifo de check y texto.
- **Los aspectos reales de señal se dibujarán solo dentro de símbolos de señal**, con su significado real, cuando exista la investigación de señalización. Hasta entonces no se representan.
- **El patrimonio se reduce a la letra.** Se usa stencil solo para identificadores y mojones kilométricos. Mientras no se confirme la licencia de FC Nefa, se usa *Big Shoulders Stencil* (Google Fonts, SIL OFL) en su lugar.

## Safety invariants

- Ninguna. Esta nota es de presentación.
- Una regla visual nunca puede reemplazar una validación de dominio: un tramo en "alarma" lo determina el core, no la UI.

## Simplifications

- No distinguimos prioridades de alarma más allá de advertencia y alarma.
- No modelamos todavía los aspectos compuestos de señal ni las señales de maniobra.

## Unknowns

- Tabla completa de aspectos luminosos del RITO vigente (y del Reglamento Operativo actual), con artículos. Hay que leer el PDF oficial con una herramienta que extraiga el texto.
- Si el azul de "acción requerida" o el ámbar de advertencia chocan con algún aspecto o luz de maniobra real (por ejemplo, luces azules o violetas en maniobras).
- Licencia de FC Nefa: contactar a Fabio Ares u Octavio Osores antes de incorporarla.
- Texto normativo de ANSI/ISA-101.01 sobre color.

## Confidence

MEDIUM para la filosofía de HMI: varias fuentes secundarias coinciden, aunque no se leyó la norma.
LOW para los aspectos de señal argentinos, que se basan solo en fuentes secundarias.
LOW para la licencia de FC Nefa: no hay información publicada.
