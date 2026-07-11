---
id: rule-111
title: Quote Expiration Window
type: rule
source: BRULS
category: NewBusiness
domain: NewBusiness
tags: [quotes, lifecycle]
created: 2026-06-26
lastReviewed: 2026-06-26
version: 1
canonicalSlug: rule-111-quote-expiration-window
description: A new business quote expires 30 calendar days after issuance unless it is re-quoted or formally extended by underwriting.
---

## Rule-111: Quote Expiration Window

### Acceptance Criteria

* **AC-1:** The system must calculate the quote expiration date from the original quote creation timestamp.
* **AC-2:** Expired quotes must block bind and issue actions until a new quote is generated or the expiration is extended.

### Gherkin Test Cases

* **Scenario A (Valid Quote):**
  * Given Quote Created Date = June 1st
  * When the policy is bound on June 20th
  * Then Status Action = Allow issuance
* **Scenario B (Expired Quote):**
  * Given Quote Created Date = June 1st
  * When the policy is bound on July 5th
  * Then Status Action = BLOCK ISSUANCE; quote must be reissued or extended