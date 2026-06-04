# Code Style

Implementation-level rules for code-writing agents (Backend Dev, Frontend Dev, Test Engineer). Binding during `impl`, verified during `impl-review`. Governs how code is written; architecture-level rules are out of scope. All code in English.

## 1. Engineering Principles

- Follow SOLID, KISS, DRY, YAGNI.
- Small atomic functions, single clear responsibility.
- Readability over cleverness.
- No hidden coupling, global state, or action at a distance.

## 2. Naming

- Names reveal intent. Abbreviations only when domain-standard.
- Casing follows language convention. No Hungarian or type prefixes.
- Booleans read as predicates (`is`, `has`, `should`).
- One concept, one name across the codebase.

## 3. Scope Discipline

- Touch only what the task requires; every changed line traces to the task.
- Match surrounding code style.
- Do not refactor adjacent code the task did not ask for.
- Remove only the code and dependencies your change introduced.

## 4. Functions and Modules

- Guard clauses over deep nesting. Keep nesting shallow.
- Pass explicit parameters; no implicit globals or ambient state.
- A function needing a paragraph to explain itself is too big — split it.

## 5. Root Cause Over Patch

- Fix the underlying cause, not the symptom.
- No temporary workarounds, no masking of failures.

## 6. Error Handling

- No swallowed errors. Empty catch block forbidden.
- Validate input at system boundaries; reject bad state early.
- Errors carry context (what failed, with which inputs).
- Do not use errors/exceptions as control flow for expected cases.

## 7. Comments and Documentation

- Comments explain WHY, not WHAT. Code needing a comment to be understood should usually be rewritten.
- Doc comments mandatory on every public/exported type and member. Internal code documents only what a clear name cannot carry.
- Use the language-native doc format (XML-doc C#, docstrings Python, JSDoc TypeScript, etc.).
- Update doc comments when code changes.
- No commented-out code, no dead code kept "in case".
- A `TODO` uses marker `// TODO(sprint-<NNN-slug>): <reason>` with a matching entry in the project stub registry.

## 8. References to Project Documents

- Code (comments, doc strings, string literals) must not reference project documents (rules, ADR, PRD, UX spec, decisions log, sprint files).
- Code is the SSoT for behavior; in-code document references rot once those documents move.
- Do not quote or paraphrase document text in code; replace with a brief standalone rationale (e.g. `not deterministic by design`).
- Exception: `TODO` markers may carry a reference, since they are tracked and removed.

## 9. Types and Contracts

- Explicit types at boundaries. Untyped escape hatches (`any` and equivalents) only with written justification.
- Make illegal states unrepresentable where the language makes it cheap.
- Honor declared API and interface contracts exactly.

## 10. State

- Prefer immutable, local data. No shared mutable state without explicit synchronization.
- No hidden side effects in functions that look pure.
- Keep variable scope and lifetime minimal.

## 11. Determinism

- Prefer logic producing the same outputs for the same inputs, unless the task requires otherwise.
- Avoid incidental time- or order-dependent behavior in core logic.
- When nondeterminism is intended, isolate it behind a small interface; test the deterministic part separately.

## 12. No Hardcoded Values

- Tuning/configuration values live in external config files, not code.
- UI visual values (color, typography, spacing, radii) bind to design tokens. No raw hex, px, or pt in UI code.

## 13. Dependencies

- A new third-party dependency requires explicit user approval before use.
- Prefer the standard library. Pin dependency versions.

## 14. External API Verification

- Do not rely on training data for the API of a library, framework, or runtime.
- Verify signatures and behavior against the pinned version's documentation before use.

## 15. Security

- No secrets in code, logs, or commits.
- Validate and sanitize all external input.
- Use parameterized queries/APIs; never build SQL, shell, or other commands by string concatenation.
- Least privilege for every credential, token, access scope.

## 16. Logging

- Logs are structured and carry context.
- Never log secrets or PII.
- Meaningful log levels: error (failures), warn (recoverable anomalies), info (state changes), debug (detail).

## 17. Tests

- Tests for a new system are written before its implementation (verification-driven): expected output compared against actual before the work is marked complete.
- Every acceptance criterion has test coverage.
- Tests verify observable behavior, not implementation detail.
- No meaningless assertions, no tests written only to inflate coverage.
- Deterministic: no `sleep`, wall-clock timing, random seeds, or execution-order reliance.
- Isolated: no real external APIs, databases, or file I/O; use dependency injection.
- No hardcoded test data: build fixtures from named constants or factories (exception: boundary-value tests where the literal is the point).
- Test files named `<system>_<feature>_test.<ext>`; test functions `test_<scenario>_<expected>`.
- A test mutating global/static state saves and restores it in setup/teardown.
- Structure each test as Arrange — Act — Assert.
- Global test coverage must not fall below 80%.

## 18. Concurrency

- No data races. Document thread-safety of every shared component.
- Every blocking I/O call has a timeout.
- Keep main/UI threads free: offload CPU-heavy and I/O-bound work. No `sleep`, busy-wait, or synchronous locks on the main/UI thread.

## 19. Formatting

- The project formatter and linter decide style; no manual style debate.
- Style consistent within a file.
- Build, test, and lint must pass before any commit.

## 20. Per-Language Rules

Language- and stack-specific rules are added below as the project stack is fixed. Empty until then.
