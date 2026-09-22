# Cómo trabajar con agentes

Guía operativa para sesiones de trabajo con Claude, Codex, Gemini/Antigravity o cualquier otro agente. Principio de base ([Kickoff](../../RailWeaver_Project_Kickoff.md) §28): **el repositorio es la memoria compartida; los agentes son trabajadores descartables.**

## 1. Roles

- **Autor (humano):** decide qué y en qué orden, aprueba planes, revisa resultados, mantiene el repo veraz.
- **Agente:** ejecuta rápido y no recuerda nada entre sesiones. Solo sabe lo que está escrito en el repo. No depender de la memoria propia de ninguna herramienta.
- **La unidad de trabajo es la spec, no la sesión.** Una spec puede llevar varias sesiones; cada sesión avanza una parte concreta de una spec.

Regla: **si hay que explicarle algo largo a un agente, falta un documento.** Primero se escribe el documento, después se trabaja.

## 2. Jerarquía de fuentes

```text
Kickoff              por qué y qué es RailWeaver     (casi no cambia)
ADRs                 cómo decidimos hacerlo          (cambia con motivo)
project-status.md    dónde estamos                   (cambia cada sesión)
spec activa          qué estamos haciendo ahora      (se va tildando)
sesión               una parte concreta de la spec
```

Una sesión que no encaja en esta cadena no debería existir.

## 3. Tipos de sesión

No mezclarlos en la misma sesión.

| Tipo | Resultado en el repo |
|---|---|
| Investigación | nota en `docs/research/` |
| Escritura de spec | spec en `docs/specs/` |
| Implementación | código + tests contra una spec |
| Review | observaciones o correcciones (opcional, bajo demanda) |
| Mantenimiento | docs, dependencias, limpieza |

Todo concepto ferroviario pasa primero por una sesión de investigación. Implementar sin investigación produce comportamiento ferroviario inventado.

## 4. Inicio de sesión

Antes de abrir el agente:

1. `git status` limpio: nada colgado de la sesión anterior.
2. Branch de la spec (crearla desde `main` si no existe):

```bash
git switch -c feature/RW-NNN-descripcion
```

3. Un único objetivo, expresable en una frase.

Prompt de inicio:

```text
Leé AGENTS.md, docs/status/project-status.md y docs/specs/active/<spec>.md.

Objetivo de esta sesión: <una parte concreta del scope>. Nada más.

Antes de escribir código: resumime qué entendiste, proponé un plan y listá
las dudas o decisiones que veas.
```

**Plan antes que código.** Si el plan incluye dependencias nuevas, proyectos nuevos, archivos ajenos al objetivo o trabajo de la milestone siguiente, se corta ahí.

## 5. Durante la sesión

- **Si el agente pregunta algo que es una decisión, no responderla solo en el chat:** registrarla en la spec (open questions) o en un ADR.
- **Señales de desvío:** agrega dependencias, crea carpetas "para el futuro", arregla cosas no pedidas, avanza la siguiente milestone. Frenar y volver a la spec.
- **No aceptar código que no se pueda explicar.** Si algo no se entiende, pedir explicación: es parte del aprendizaje.
- Commits chicos a medida que se completan partes.

## 6. Cierre de sesión

Prompt de cierre:

```text
Cerremos la sesión:
1. Corré dotnet test y los checks del frontend y mostrame el resultado.
2. Tildá en la spec lo que quedó cumplido.
3. Actualizá docs/status/project-status.md.
4. ¿Se tomó alguna decisión que merezca ADR?
5. ¿Corresponde actualizar CHANGELOG.md o el kickoff?
6. Resumí lo que cambió y proponé el mensaje de commit.
```

Luego, el autor:

1. Revisa el diff (`git diff`): al menos qué archivos cambiaron y por qué.
2. Hace el commit.
3. Aplica la prueba: *"si mañana abro otra herramienta, ¿puede continuar solo leyendo el repo?"* Si no, falta actualizar algo.

Al completar una spec:

1. Verificar que pasen todos los checks locales (`dotnet test`, lint y build del frontend).
2. Merge directo a `main` (sin pull requests en GitHub ni esperas):
   ```bash
   git switch main
   git merge feature/RW-NNN-descripcion
   git push origin main
   ```
3. Mover la spec de `docs/specs/active/` a `docs/specs/completed/` y, si corresponde, tag de release (`vX.Y.Z`, ver CHANGELOG).
4. Borrar la branch local de la feature:
   ```bash
   git branch -d feature/RW-NNN-descripcion
   ```

## 7. Cambiar de herramienta

- Todas leen el mismo repo:
  - Codex lee `AGENTS.md`.
  - Claude lee `CLAUDE.md`, que importa `AGENTS.md`.
  - Gemini CLI / Antigravity: configurarlos para leer `AGENTS.md`, o crear un `GEMINI.md` que solo lo referencie.
- **El traspaso entre herramientas pasa siempre por el repo, nunca por copiar conversaciones.** El cierre de sesión es lo que lo hace posible.
- **Review con un modelo distinto (opcional / bajo demanda):** no es obligatoria para cada feature ni parte de los criterios de aceptación estándar; solo se usa si el autor quiere una segunda mirada en temas sensibles o complejos:

```text
Leé AGENTS.md y la spec <spec>. Revisá el diff de esta branch contra main.
¿Cumple los criterios de aceptación? ¿Hay algo fuera de alcance?
¿Algo viola un ADR?
```

- Nunca dos agentes en la misma working tree a la vez. Para trabajo paralelo: git worktrees ([Kickoff](../../RailWeaver_Project_Kickoff.md) §33).

## 8. Mantener el orden

- **Una sola spec activa.** Ideas de otras milestones van como nota corta a `docs/specs/backlog/`, no al código.
- Antes de empezar una spec: *"¿esto avanza la vertical slice de M0 (mapa → vías → origen/destino → corredor → elevación)?"* Si no, no es lo siguiente.
- **Revisión semanal (~15 min):**
  - ¿`project-status.md` refleja la realidad?
  - ¿Lo siguiente sigue siendo lo correcto?
  - ¿Hay cosas en el backlog que ya no tienen sentido?
  - ¿El kickoff quedó desactualizado en algo?
- El orden de [Kickoff](../../RailWeaver_Project_Kickoff.md) §42 es orientativo: cambiarlo está bien si se decide y se escribe.

## 9. Señales de alarma

- Sesiones que terminan sin commit ni actualización de estado.
- Explicarle lo mismo a cada agente nuevo.
- `project-status.md` dice algo distinto de lo que hay en el código.
- Código ferroviario sin nota de investigación detrás.
- Dependencias que ningún ADR ni spec justifica.
- Dos specs "activas".
