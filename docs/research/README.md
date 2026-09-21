# Investigación de dominio

Antes de implementar un concepto ferroviario relevante debe existir una nota de investigación mínima. Organizar por área (`terrain-routing/`, `signalling/`, `operations/`, `rolling-stock/`, …), creando la carpeta cuando haya una primera nota.

Fuentes preferidas: documentación ferroviaria oficial, CNRT y normativa argentina, organismos ferroviarios, EULYNX, railML, ERA, literatura académica/industrial. Fuentes secundarias ayudan a entender, pero no pueden ser la única base de una regla crítica.

## Plantilla

```markdown
# <Tema>

## Question
## Sources
## Real-world behavior
## RailWeaver abstraction
## Safety invariants
## Simplifications
## Unknowns
## Confidence
LOW | MEDIUM | HIGH — y por qué
```

Notebooks y prototipos en Python van en `research/notebooks/` (raíz del repo) cuando exista el primero.
