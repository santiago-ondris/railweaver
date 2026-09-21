# RailWeaver

RailWeaver is a railway planning and simulation sandbox. First world: Córdoba, Argentina (a dataset, not a core dependency).

Before substantial work read:

- `docs/vision/product-vision.md`
- `docs/architecture/overview.md`
- `docs/status/project-status.md`
- the active spec in `docs/specs/active/`

Session workflow (how to start and close a session): `docs/process/agent-workflow.md`.

`RailWeaver_Project_Kickoff.md` is the living, project-level description. `docs/decisions/` holds the ADRs.

Rules:

- Never invent railway behavior. Research first (`docs/research/`), document assumptions and simplifications.
- Keep the simulation core (`src/RailWeaver.Core`) independent from API, persistence and visualization.
- Deterministic safety rules cannot depend on AI.
- Do not reopen decisions recorded in ADRs without a concrete reason; new architectural decisions require an ADR.
- Avoid speculative infrastructure: add dependencies only when a concrete requirement justifies them.
- Work from a spec; one milestone at a time.
- Language (ADR-008): Spanish for `docs/`, README, CHANGELOG and the kickoff; English for everything else (code, comments, file names, commits, branches, agent files).
- After substantial work: update `docs/status/project-status.md`, `CHANGELOG.md` when relevant, and review the kickoff.

Commands (from repo root):

```bash
dotnet build && dotnet test          # backend
npm --prefix src/web run lint        # frontend lint
npm --prefix src/web run build       # frontend typecheck + build
docker compose up -d --wait          # PostGIS
```
