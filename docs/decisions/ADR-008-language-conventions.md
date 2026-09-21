# ADR-008 — Convención de idioma

- Status: Accepted
- Date: 2026-09-21

## Context

El proyecto mezclaba idiomas: documentación en castellano, `AGENTS.md` en inglés, código en inglés. Mezclar sin regla degrada la documentación a largo plazo.

## Decision

- **Castellano:** documentación en `docs/`, `README.md`, `CHANGELOG.md` y el kickoff.
- **Inglés:** todo lo demás — código, identificadores, comentarios de código, nombres de archivos y carpetas, mensajes de commit, nombres de branches, mensajes de API/logs, archivos de configuración de agentes (`AGENTS.md`, `CLAUDE.md`).
- Términos técnicos ferroviarios sin traducción establecida (p. ej. *interlocking*, *headway*, *movement authority*) pueden quedar en inglés dentro de la documentación en castellano.

## Consequences

- El vocabulario de dominio del código coincide con las fuentes de referencia (EULYNX, railML, ERA).
- Los textos visibles al usuario en la UI quedan fuera de esta regla; su idioma se decidirá al abordar internacionalización.

## Alternatives considered

- **Todo en inglés:** mayor alcance si el proyecto se abre; menos natural para el autor y para el dominio local.
- **Todo en castellano:** identificadores en castellano chocarían con la terminología de las fuentes.
