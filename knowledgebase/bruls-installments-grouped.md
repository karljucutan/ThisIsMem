---
id: bruls-installments-grouped-v1
title: BRULS Installments Grouped Rules
type: collection
source: BRULS
domain: installments
created: 2026-06-26
lastReviewed: 2026-06-26
version: 1
author:
  name: Karl Jucutan
---

## BRULS Installments Grouped Rules

## Group Summary

This grouped file contains installment and billing rules with progressive disclosure sections.

```yaml
id: Rule-106
title: Installment Bill Date Calculation
category: Installments
tags: [accounting, billing]
created: 2026-06-26
lastReviewed: 2026-06-26
version: 1
canonicalSlug: rule-106-installment-bill-date
```

## Rule-106: Installment Bill Date Calculation

* **Rule Statement:** The `BillDate` (invoice generation date) must be set exactly 30 days prior to the installment `DueDate`.

### 📋 Acceptance Criteria — Rule-106

* **AC-1:** The accounting engine calculates `BillDate = DueDate - 30 days`.
* **AC-2:** If `BillDate` lands on a weekend, no date shifting is applied.

### 🧪 Gherkin Test Cases — Rule-106

* **Scenario A (Standard Month):**
  * Given Installment `DueDate` = July 1st
  * When the rule is evaluated
  * Then Invoice `BillDate` = June 1st
* **Scenario B (Leap Year):**
  * Given Installment `DueDate` = March 2nd, 2024
  * When the rule is evaluated
  * Then Invoice `BillDate` = February 1st, 2024
