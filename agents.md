# Trading Dashboard — agent context

## Stack
 - ASP.NET for backend
 - Postgres + MartenDB for database and ORM
 - React/TS/MUI for frontend

## Agent rules
1. Ask for clarification when requirements are unclear

## Project rules
1. **New code must include tests** — every non-trivial change ships with automated coverage appropriate to the layer (unit / integration / e2e as the repo already uses).
2. **TDD by default** — write a **failing** test first, implement the minimum to pass, then **refactor** (red → green → refactor). Only skip when the task explicitly calls for a different approach.
3. **Test layout mirrors source** — test project folder structure must mirror the code under test (same relative paths / namespaces as far as the test stack allows).
4. **Minimal diffs** — change only what the task requires; no drive-by refactors or unrelated formatting.
5. **Match existing patterns** — naming, file layout, abstractions, and style consistent with surrounding code.
