---
id: bruls-endorsement-underwriting-grouped-v1
title: BRULS Endorsement Underwriting Grouped Rules
type: collection
source: BRULS
domain: endorsement-underwriting
created: 2026-06-26
lastReviewed: 2026-06-26
version: 1
author:
  name: Karl Jucutan
description: Underwriting rules for endorsement changes that increase insured exposure beyond threshold, requiring referral for approval before issuance.
---

## BRULS Endorsement Underwriting Grouped Rules

## Group Overview

This grouped file contains underwriting rules for endorsement changes.

```yaml
id: Rule-301
title: Endorsement Exposure Increase Referral
category: Endorsement.Underwriting
tags: [endorsement, underwriting, exposure]
created: 2026-06-26
lastReviewed: 2026-06-26
version: 1
canonicalSlug: rule-301-endorsement-exposure-increase-referral
description: Endorsements that increase insured exposure beyond threshold must be referred for underwriting approval before issuance.
```

## Rule-301: Endorsement Exposure Increase Referral

### Acceptance Criteria

* **AC-1:** Exposure delta is computed at endorsement rating time.
* **AC-2:** If delta exceeds threshold, endorsement status is set to UnderwritingReview.

### Gherkin Test Cases

* **Scenario A (Within Threshold):**
  * Given Endorsement exposure increase is below referral threshold
  * When the rule is evaluated
  * Then Endorsement can continue to issuance
* **Scenario B (Above Threshold):**
  * Given Endorsement exposure increase is above referral threshold
  * When the rule is evaluated
  * Then Endorsement is routed to underwriting review queue
