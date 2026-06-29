---
id: bruls-newbusiness-issuance-grouped-v1
title: BRULS NewBusiness Issuance Grouped Rules
type: collection
source: BRULS
domain: newbusiness-issuance
created: 2026-06-26
lastReviewed: 2026-06-26
version: 1
author:
  name: Karl Jucutan
description: Issuance rules for new business covering minimum down payment requirements before a policy can be bound or issued.
---

## BRULS NewBusiness Issuance Grouped Rules

## Group Overview

This grouped file contains related NewBusiness issuance rules organized for progressive disclosure.

```yaml
id: Rule-101
title: Minimum Down Payment Collection
category: NewBusiness
tags: [payments, binding]
created: 2026-06-26
lastReviewed: 2026-06-26
version: 1
canonicalSlug: rule-101-minimum-down-payment
description: A new business policy cannot proceed to a "Bound" or "Issued" status until the initial down payment is fully collected and verified.
```

## Rule-101: Minimum Down Payment Collection

### Acceptance Criteria

- AC-1: The binding engine must verify a successful credit card authorization token or cleared ACH reference before switching the policy status from Quote to Bound.
- AC-2: No temporary binder documents may be generated while the payment status is "Pending".

### Gherkin Test Cases

- Scenario A (Payment Cleared):
  - Given Quote Premium = $1,200.00; Down Payment Received = $100.00
  - When the rule is evaluated
  - Then Status Action = Allow Bind / Issue Policy
- Scenario B (Payment Failed):
  - Given Quote Premium = $1,200.00; Down Payment Received = $0.00 (Declined)
  - When the rule is evaluated
  - Then Status Action = BLOCK BINDING; Hard validation error thrown
