---
name: concept-drafting
description: How the user wants reverse-engineered concept/PRD drafts produced for this project
metadata:
  type: feedback
---

For brownfield reverse-engineering tasks: return drafts as structured text only (do NOT write the HTML file); the user runs section-by-section lock-in with the end user in Russian and writes the final HTML later.

- Per section: provide content (English, language.docs=en) + a provenance tag (`source: <path:line>` when extracted, `source: inferred` when behavioral).
- Propose frontmatter (provenance: reverse-engineered, source: primary origin).
- For optional concept sections, include only those with genuine evidence; for the rest say "omit (no evidence)" with a one-line reason and recommend which to include.
- Follow QODDA + Simplicity Default: no invented features, no marketing fluff (e.g. omit core-identity/anti-pillars unless user supplies brand voice / non-goals).

**Why:** User owns the lock-in conversation and final HTML; chat language for them is Russian, docs language is English.
**How to apply:** Whenever asked to draft/reverse-engineer concept or PRD content for this repo.
