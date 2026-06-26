Knowledgebase guidelines

Frontmatter
- Each collection file should start with YAML frontmatter describing collection-level metadata.
- Each rule must include a small YAML block immediately before the rule heading with: `id`, `title`, `category`, `tags`, `created`, `lastReviewed`, `version`, and `canonicalSlug`.

Ingestion notes
- Ingestion should:
  1. Read the collection frontmatter.
  2. Parse each rule block and its markdown body into one document object.
  3. Split examples or acceptance criteria into sub-chunks as needed for embeddings.
  4. Store vectors alongside metadata: `id`, `canonicalSlug`, `file`, `lastReviewed`, `tags`.

Sanitization
- Run a pre-ingest sanitizer that removes obvious secrets or PII before indexing.

Conventions
- Use `Rule-###` stable IDs for rules.
- Keep rule headings unique and human-readable.
- Default and preferred authoring mode is grouped files by domain.
- Keep `/rules` as a small standalone sample set (currently 5 rules) for parser and retrieval examples.
 - Grouped file naming: `<source>-<domain>-grouped.md` (lowercase, hyphen-separated, example: `bruls-newbusiness-grouped.md`).
 - One-rule-per-file naming: `rule-###-<short-slug>.md` (lowercase, example: `rule-204-agent-of-record-change-guardrails.md`).
 - Collection frontmatter `id` pattern: `<source>-<domain>-grouped-v<version>` (lowercase).

Example frontmatter
```yaml
---
id: Rule-101
title: Minimum Down Payment Collection
category: NewBusiness
tags: [payments, binding]
created: 2026-06-26
lastReviewed: 2026-06-26
version: 1
canonicalSlug: rule-101-minimum-down-payment
---
```

Scripts
- A simple ingestion helper is provided at `scripts/ingest-bruls.js`.
