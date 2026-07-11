---
id: rule-104
title: Underwriting Hard-Stop Referral Triggers
type: rule
source: BRULS
category: NewBusiness
domain: NewBusiness
tags: [underwriting, risk]
created: 2026-06-26
lastReviewed: 2026-06-26
version: 1
canonicalSlug: rule-104-underwriting-hard-stop
description: New business quotes with maximum hazard exposures cannot be auto-issued.
---

## Rule-104: Underwriting Hard-Stop Referral Triggers

### Acceptance Criteria

* **AC-1:** The system must scan the risk selection questionnaires for high-risk flags before enabling the "Issue" trigger.

### Gherkin Test Cases

* **Scenario A (Standard Risk):**
  * Given Application answers "No" to all major property hazard exposure questions
  * When the rule is evaluated
  * Then Status Action = Allow instant straight-through auto-issuance
* **Scenario B (High Risk):**
  * Given Application answers "Yes" to "Is there active roofing structural damage?"
  * When the rule is evaluated
  * Then Status Action = BLOCK AUTO-ISSUE; Divert to a mandatory Underwriting approval queue
