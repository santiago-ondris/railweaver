---
name: karpathy-principles
description: >-
  Operational discipline and guidelines for AI coding agents based on Andrej Karpathy's
  principles. Use when planning, implementing, refactoring, or diagnosing code to avoid
  over-engineering, assumptions, and unintended side effects.
---

# Karpathy Principles for AI Coding Agents

Guidelines derived from Andrej Karpathy's observations regarding LLM coding pitfalls.
Apply these principles across all planning, design, and code modification tasks.

## Core Rules

### 1. Think Before Coding
- **Never make silent assumptions.** If requirements or constraints are ambiguous, explicitly surface the trade-offs, state your assumptions, and ask the user for clarification before modifying code.
- **Inspect first, edit second.** Read relevant documentation, ADRs, existing interfaces, and test files before producing any implementation.
- **Respect established architecture.** In RailWeaver, respect ADRs and core boundary constraints (such as `RailWeaver.Core` independence). Never introduce speculative frameworks.

### 2. Simplicity First (Anti-Over-Engineering)
- **Do not add unrequested features or abstractions.** Avoid speculative interfaces, factory wrappers, premature config options, or "just-in-case" flexibility.
- **Write the minimum code required.** Solve the specific problem directly and cleanly.
- **Prefer transparent, readable code over clever meta-programming.**
- **Deterministic safety rules cannot depend on AI.** Keep safety and physical simulation rules simple, transparent, and provable.

### 3. Surgical Changes
- **Touch only what is strictly necessary.** Do not reformat adjacent lines, update unrelated dependencies, or refactor code that is working unless explicitly instructed.
- **Preserve existing style and conventions.** Match naming conventions, formatting, error-handling patterns, and language rules (e.g. English for code, Spanish for `docs/`).
- **No drive-by cleanups.** If you notice technical debt or minor styling inconsistencies outside the scope of your current task, note them to the user rather than silently modifying them.

### 4. Goal-Driven Execution
- **Define verifiable success criteria before coding.** Transform vague requirements into concrete verification steps (e.g. unit tests, CLI checks, lint commands).
- **Test-first when appropriate.** When fixing a bug or adding domain logic, reproduce the issue with a failing test first, then implement the fix until the test passes.
- **Loop until green.** Run automated tests and linters locally before concluding work:
  ```bash
  dotnet build && dotnet test
  npm --prefix src/web run lint
  npm --prefix src/web run build
  ```
