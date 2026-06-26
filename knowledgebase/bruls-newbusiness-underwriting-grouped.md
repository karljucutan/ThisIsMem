---
id: bruls-newbusiness-underwriting-grouped-v1
title: BRULS NewBusiness Underwriting Grouped Rules
type: collection
source: BRULS
domain: newbusiness-underwriting
created: 2026-06-26
lastReviewed: 2026-06-26
version: 1
author:
  name: Karl Jucutan
---

## BRULS NewBusiness Underwriting Grouped Rules

## Group Summary

This grouped file contains underwriting rules for new business submissions.

```yaml
id: Rule-102
title: Prior Bad Debt / Uncollectible Balance Block
category: NewBusiness.Underwriting
tags: [collections, underwriting]
created: 2026-06-26
lastReviewed: 2026-06-26
version: 1
canonicalSlug: rule-102-prior-bad-debt-block
```

## Rule-102: Prior Bad Debt / Uncollectible Balance Block

* **Rule Statement:** New business applications cannot be bound if the applicant has outstanding uncollectible balances from prior canceled policies.

### 📋 Acceptance Criteria — Rule-102

* **AC-1:** System must run SSN or FEIN lookup against bad debt ledger.
* **AC-2:** A positive match freezes application until debt is cleared or manager override is applied.

### 🧪 Gherkin Test Cases — Rule-102

* **Scenario A (Clean History):**
  * Given Applicant has no matching debt records
  * When the rule is evaluated
  * Then Status Action = Allow to proceed to binding step
* **Scenario B (Matching Debt):**
  * Given Applicant matches a canceled policy with outstanding debt
  * When the rule is evaluated
  * Then Status Action = BLOCK ISSUANCE and route to underwriting review
