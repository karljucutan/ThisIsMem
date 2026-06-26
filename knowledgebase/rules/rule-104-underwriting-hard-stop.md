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
---

## Rule-104: Underwriting Hard-Stop Referral Triggers

* **Rule Statement:** New business quotes containing maximum hazard exposures (e.g., active structural damage, knob-and-tube wiring, or open commercial liability claims) cannot be auto-issued.

### 📋 Acceptance Criteria — Rule-104

* **AC-1:** The system must scan the risk selection questionnaires for high-risk flags before enabling the "Issue" trigger.

### 🧪 Gherkin Test Cases — Rule-104

* **Scenario A (Standard Risk):**
  * Given Application answers "No" to all major property hazard exposure questions
  * When the rule is evaluated
  * Then Status Action = Allow instant straight-through auto-issuance
* **Scenario B (High Risk):**
  * Given Application answers "Yes" to "Is there active roofing structural damage?"
  * When the rule is evaluated
  * Then Status Action = BLOCK AUTO-ISSUE; Divert to a mandatory Underwriting approval queue
