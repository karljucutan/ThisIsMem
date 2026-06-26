---
id: rule-102
title: Prior Bad Debt / Uncollectible Balance Block
type: rule
source: BRULS
category: NewBusiness
domain: NewBusiness
tags: [collections, underwriting]
created: 2026-06-26
lastReviewed: 2026-06-26
version: 1
canonicalSlug: rule-102-prior-bad-debt-block
---

## Rule-102: Prior Bad Debt / Uncollectible Balance Block

* **Rule Statement:** New business applications cannot be bound if the applicant has an outstanding, uncollectible balance from a previously canceled policy with the carrier.

### 📋 Acceptance Criteria — Rule-102

* **AC-1:** System must perform an automatic Social Security Number (SSN) or Federal Employer Identification Number (FEIN) lookup across the historical bad debt ledger.
* **AC-2:** If a match is found, the system must freeze the application until the prior debt is fully cleared or a manual manager override is applied.

### 🧪 Gherkin Test Cases — Rule-102

* **Scenario A (Clean History):**
  * Given Applicant has no matching historical records in the collections database
  * When the rule is evaluated
  * Then Status Action = Allow to proceed to binding step
* **Scenario B (Matching Debt):**
  * Given Applicant matches a 2024 canceled policy with a $450.00 outstanding balance
  * When the rule is evaluated
  * Then Status Action = BLOCK ISSUANCE; Route application to Underwriting Review
