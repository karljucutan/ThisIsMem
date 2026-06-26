# Progressive Disclosure Implementation Plan

## Goal

Handle many rule files and long rule text without overwhelming users.

Users should get the best answer quickly, then optionally drill into more context.

## Rule organization decision

Default approach: group related rules by domain in one markdown file, not one file per rule.

Why this default:

- Reduces file explosion and maintenance overhead when rules grow.
- Keeps related constraints together, which improves retrieval context.
- Works naturally with progressive disclosure by revealing heading-level depth.

Use one-rule-per-file only when:

- A rule has independent ownership and lifecycle.
- A rule is high-risk and needs isolated review/audit history.
- A rule is reused across multiple domains and should be versioned independently.

## Progressive disclosure structure

Progressive disclosure is implemented with YAML frontmatter plus Markdown headings.

- YAML frontmatter is the discovery layer.
- Markdown headings are the content depth layers.

Example structure:

```md
---
id: pricing-discount-rules
domain: pricing
tags: [discount, eligibility, checkout]
summary: Rules that govern discount eligibility and stacking.
applies_to: [cart, checkout]
priority: high
owner: commerce-team
last_reviewed: 2026-06-01
---

# Pricing Discount Rules

## Policy Summary
High-level statements used for first-pass answers.

## Eligibility Rules
Detailed conditions and exclusions.

## Exceptions
Edge cases and override conditions.

## Examples
Concrete scenarios used as supporting evidence.
```

## UX layers

Layer 1: Answer summary (default)

- 3 to 6 bullet points with the direct answer.
- Include short confidence signal (High, Medium, Low).
- Include top source references only.
- Prefer frontmatter summary plus Policy Summary heading content.

Layer 2: Supporting details (expand)

- Explain why the answer is correct.
- Show which rule fragments matched the question.
- Include per-fragment source path and section heading.
- Pull from headings like Eligibility Rules and Exceptions.

Layer 3: Full context (expand)

- Show larger excerpts around matched fragments.
- Show related rules that may affect edge cases.
- Allow viewing full source markdown when requested.
- Include full heading tree and frontmatter metadata.

## Backend implementation (ASP.NET Core)

1. Retrieval contract

- Return structured results, not only a final text blob.
- Suggested shape:
  - answerSummary
  - confidence
  - topSources[]
  - supportingMatches[] (quote, relevanceScore, sourcePath, heading)
  - relatedRules[]
  - ruleMeta[] (id, domain, tags, owner, lastReviewed)

2. Ranking and trimming

- Rank matched passages by relevance and recency if applicable.
- Send only top N matches in default payload.
- Keep full matches available for detail and full-context layers.

3. Traceability

- Every cited rule must carry sourcePath and heading.
- Keep deterministic IDs for each match so frontend can request details.

4. Progressive endpoints or flags

- Option A: single endpoint with detailLevel query param (summary, details, full).
- Option B: summary endpoint plus details endpoint by answerId.
- Start with Option A for faster delivery unless payload size becomes a problem.

## Frontend implementation (TanStack Start)

1. Default render (summary)

- Render answerSummary first.
- Show confidence badge and top source chips.
- Keep the first screen compact and easy to scan.

2. Expandable sections

- Add collapsible sections:
  - Why this answer
  - Matched excerpts
  - Related rules
  - Full context
- Keep section labels explicit and predictable.

3. On-demand loading

- If using detailLevel, fetch details/full only when user expands.
- If using single payload first, still lazy-render hidden sections.

4. State and routing

- Preserve expanded state during navigation when practical.
- Allow sharing links that open with a specific disclosure level.

## Data and retrieval quality

1. Chunking strategy

- Chunk markdown by heading first, then by paragraph boundaries.
- Store heading path to keep citations human-readable.
- Parse YAML frontmatter separately and index it as retrieval metadata.

2. Metadata

- Attach sourcePath, headingPath, and optional tags per chunk.
- Keep stable identifiers for chunks to support citations.

3. Evaluation

- Build test prompts with known expected rules.
- Track precision of topSources and usefulness of supportingMatches.

## Rollout plan

Phase 1

- Add response contract with summary and topSources.
- Update frontend to show summary + expandable placeholders.

Phase 2

- Add supportingMatches and relatedRules sections.
- Add detail-level fetching on expand.

Phase 3

- Add full-context view and deep links to source documents.
- Add retrieval quality checks in CI or scheduled validation.

## Acceptance criteria

- Users can read a concise answer without scrolling through long rule dumps.
- Users can expand to see supporting evidence and exact sources.
- Every claim shown in details/full levels is traceable to source markdown.
- Initial response remains fast and readable even with many rule files.
