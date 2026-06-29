---
id: rule-105
title: Missing Mandatory Regulatory Signatures
type: rule
source: BRULS
category: NewBusiness
domain: NewBusiness
tags: [signatures, compliance]
created: 2026-06-26
lastReviewed: 2026-06-26
version: 1
canonicalSlug: rule-105-mandatory-regulatory-signatures
description: No policy may be bound or legally issued without fully executed digital signatures on mandatory state-specific disclosure forms.
---

## Rule-105: Missing Mandatory Regulatory Signatures

### Acceptance Criteria

* **AC-1:** Electronic signature tokens must be returned from the DocuSign/e-sign wrapper API and appended to the policy file.

### Gherkin Test Cases

* **Scenario A (Forms Complete):**
  * Given All 3 mandatory state-specific selection forms contain valid signature metadata
  * When the rule is evaluated
  * Then Status Action = Release policy documents for active binding
* **Scenario B (Missing Forms):**
  * Given The applicant skipped signing the mandatory liability limitation form
  * When the rule is evaluated
  * Then Status Action = BLOCK BINDING; Keep status as "Pending Signature"
