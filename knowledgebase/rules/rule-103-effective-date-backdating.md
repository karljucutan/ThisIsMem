---
id: rule-103
title: Effective Date Backdating Limitations
type: rule
source: BRULS
category: NewBusiness
domain: NewBusiness
tags: [dates, backdating]
created: 2026-06-26
lastReviewed: 2026-06-26
version: 1
canonicalSlug: rule-103-effective-date-backdating
---

## Rule-103: Effective Date Backdating Limitations

* **Rule Statement:** A new business policy cannot be issued with a requested effective date that is backdated more than 3 calendar days prior to the current submission timestamp.

### 📋 Acceptance Criteria — Rule-103

* **AC-1:** The system must compare the requested `PolicyEffectiveDate` against the current system date (`DateTime.UtcNow`).
* **AC-2:** Backdating within the 3-day window requires an electronic No-Known-Loss Letter (NKLL) signed by the applicant.

### 🧪 Gherkin Test Cases — Rule-103

* **Scenario A (Acceptable Backdate):**
  * Given Current Date = June 15th; Requested Effective Date = June 13th (2 days back)
  * When the rule is evaluated
  * Then Status Action = Allow binding (Pending NKLL verification)
* **Scenario B (Invalid Backdate):**
  * Given Current Date = June 15th; Requested Effective Date = June 10th (5 days back)
  * When the rule is evaluated
  * Then Status Action = BLOCK ISSUANCE; System rejects input date
