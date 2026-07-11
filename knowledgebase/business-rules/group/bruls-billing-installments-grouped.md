---
id: bruls-billing-installments-grouped-v1
title: BRULS Billing & Installments Grouped Rules
type: collection
source: BRULS
domain: PremiumAccounting
created: 2026-06-26
lastReviewed: 2026-06-26
version: 1
author:
  name: Karl Jucutan
description: Premium accounting rules covering installment bill date calculation, overdue grace period and cancellation triggers, payment voiding and ledger reversal, partial payment allocation hierarchy, and paid-in-full discount clawback.
---

## BRULS PremiumAccounting Grouped Rules

## Group Overview

This grouped file contains related PremiumAccounting rules organized for progressive disclosure.

```yaml
id: Rule-106
title: Installment Bill Date Calculation
category: PremiumAccounting
tags: [accounting, billing]
created: 2026-06-26
lastReviewed: 2026-06-26
version: 1
canonicalSlug: rule-106-installment-bill-date
description: The `BillDate` (invoice generation date) must be set exactly 30 days prior to the installment's `DueDate`.
```

## Rule-106: Installment Bill Date Calculation

### Acceptance Criteria

* **AC-1:** The accounting engine must automatically calculate the offset value backwards from the schedule target data model.
* **AC-2:** If the calculated `BillDate` lands on a Saturday or Sunday, it must remain on that date (no weekend shifting).

### Gherkin Test Cases

* **Scenario A (Standard Month):**
  * Given Installment `DueDate` = July 1st
  * When the rule is evaluated
  * Then Invoice `BillDate` = June 1st
* **Scenario B (Leap Year boundary):**
  * Given Installment `DueDate` = March 2nd, 2024
  * When the rule is evaluated
  * Then Invoice `BillDate` = February 1st, 2024

```yaml
id: Rule-107
title: Overdue Grace Period and Cancellation Trigger
category: PremiumAccounting
tags: [payments, cancellation]
created: 2026-06-26
lastReviewed: 2026-06-26
version: 1
canonicalSlug: rule-107-overdue-grace-period
description: If an installment payment is not fully received by midnight of its `DueDate`, the policy enters an automated 15-day cancellation notice cycle.
```

## Rule-107: Overdue Grace Period and Cancellation Trigger

### Acceptance Criteria

* **AC-1:** The grace period countdown triggers on `DueDate + 1 day`.
* **AC-2:** A formal Notice of Cancellation for Non-Payment (NOC) must print and mail automatically on day 2.

### Gherkin Test Cases

* **Scenario A (On-Time Payment):**
  * Given `DueDate` = September 10th; Full payment processed September 9th
  * When the rule is evaluated
  * Then Policy Status = Active / Current
* **Scenario B (Unpaid Trigger):**
  * Given `DueDate` = September 10th; Cash balance is $0.00 on September 11th
  * When the rule is evaluated
  * Then Policy Status = Pending Cancellation; NOC document queued for printing

```yaml
id: Rule-108
title: Payment Voiding and Ledger Reversal
category: PremiumAccounting
tags: [payments, ledger]
created: 2026-06-26
lastReviewed: 2026-06-26
version: 1
canonicalSlug: rule-108-payment-voiding-ledger-reversal
description: When a posted payment is voided (e.g., due to a Bounced Check or Non-Sufficient Funds / NSF notice), all applied billing credits must be completely rolled back, and an operational penalty fee applied.
```

## Rule-108: Payment Voiding and Ledger Reversal

### Acceptance Criteria

* **AC-1:** The invoice status associated with the original payment must immediately revert from "Paid" back to "Past Due".
* **AC-2:** An automated $25.00 NSF service fee must be appended to the next customer statement invoice object.

### Gherkin Test Cases

* **Scenario A (NSF Void Event):**
  * Given A $150.00 payment for Installment #2 is marked as Voided/NSF by the bank
  * When the rule is evaluated
  * Then Balance Due = $150.00 (reopened) + $25.00 (NSF Fee) = $175.00 Total Due

```yaml
id: Rule-109
title: Partial Payment Allocation Hierarchy
category: PremiumAccounting
tags: [payments, allocation]
created: 2026-06-26
lastReviewed: 2026-06-26
version: 1
canonicalSlug: rule-109-partial-payment-allocation
description: Inbound payments that do not cover the full balance of a multi-item invoice must be applied to past-due fee balances first, before any remaining funds are allocated to the premium principle balance.
```

## Rule-109: Partial Payment Allocation Hierarchy

### Acceptance Criteria

* **AC-1:** Cash application bucket sequence must strictly follow: 1) Statutory Fees, 2) Installment Fees, 3) Past-Due Premium, 4) Current Premium.

### Gherkin Test Cases

* **Scenario A (Partial Split):**
  * Given Total Invoice = $110.00 ($10 fee + $100 premium); Cash Received = $50.00
  * When the rule is evaluated
  * Then Allocation Result = $10.00 wipes out the fee; remaining $40.00 reduces premium balance to $60.00

```yaml
id: Rule-110
title: Paid-In-Full Discount Clawback
category: PremiumAccounting
tags: [discounts, billing]
created: 2026-06-26
lastReviewed: 2026-06-26
version: 1
canonicalSlug: rule-110-paid-in-full-discount-clawback
description: If a policy was issued with a 5% "Paid-in-Full" premium discount, and a subsequent endorsement or change increases the policy premium, the account must be evaluated to see if installment billing must be initialized.
```

## Rule-110: Paid-In-Full Discount Clawback

### Acceptance Criteria

* **AC-1:** The customer has 14 calendar days from the endorsement generation to pay the newly added premium balance in full, or the 5% discount is completely stripped away and regular installment schedules are applied.

### Gherkin Test Cases

* **Scenario A (Prompt Payment):**
  * Given Endorsement adds $100.00 to premium; customer pays $100.00 within 5 days
  * When the rule is evaluated
  * Then Action = Maintain active 5% Paid-In-Full discount status
* **Scenario B (Non-Payment):**
  * Given Endorsement adds $100.00; balance remains unpaid after 14 days
  * When the rule is evaluated
  * Then Action = Remove 5% discount; recalculate total balance and break into monthly installments
