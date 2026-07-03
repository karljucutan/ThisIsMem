---
id: rule-112
title: Agent of Record Change Authorization
type: rule
source: BRULS
category: NewBusiness
domain: NewBusiness
tags: [agency, authorization]
created: 2026-06-26
lastReviewed: 2026-06-26
version: 1
canonicalSlug: rule-112-agent-of-record-change-authorization
description: An agent of record change request must include signed authorization from the policyholder before it can be applied.
---

## Rule-112: Agent of Record Change Authorization

### Acceptance Criteria

* **AC-1:** The system must verify that the AOR request includes a signed instruction from the policyholder.
* **AC-2:** Requests lacking authorization must remain pending and cannot update the producing agent on the policy.

### Gherkin Test Cases

* **Scenario A (Authorized Request):**
  * Given a signed AOR request is received
  * When the request is reviewed
  * Then Status Action = Apply agent change
* **Scenario B (Missing Signature):**
  * Given an unsigned AOR request is received
  * When the request is reviewed
  * Then Status Action = HOLD REQUEST; require signed authorization