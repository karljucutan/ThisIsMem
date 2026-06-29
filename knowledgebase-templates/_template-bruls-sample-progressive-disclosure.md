---
id: bruls-sample-progressive-disclosure-v1
title: Sample Progressive Disclosure Rules
type: collection
source: BRULS
domain: SampleDomain
created: 2026-06-26
lastReviewed: 2026-06-26
version: 1
author:
  name: Documentation Team
description: This file demonstrates Layer 1, Layer 2, and Layer 3 progressive disclosure for rules.
tags: [sample, documentation, progressive-disclosure]
appliesTo: [SampleDomain]
---

## Progressive Disclosure Layers Reference

This file demonstrates how rules are structured across three layers of disclosure:

- **Layer 1 (Fast/Discovery):** YAML frontmatter only — rule ID, title, category, tags, canonical slug
- **Layer 2 (Medium/Description):** Add Description and Acceptance Criteria sections
- **Layer 3 (Full/Details):** Add Gherkin Test Cases, Examples, Exceptions, Implementation Notes

---

## Rule-Sample-001: Basic Rule with All Three Layers

### Layer 1: Frontmatter (YAML metadata)

```yaml
id: Rule-Sample-001
title: Verification Status Must Precede Binding
category: SampleDomain.Verification
tags: [verification, binding, compliance]
created: 2026-06-26
lastReviewed: 2026-06-26
version: 1
canonicalSlug: rule-sample-001-verification-before-binding
```

### Layer 2: Description and Acceptance Criteria

### Description

A business application cannot proceed to "Bound" or "Active" status until the applicant's identity and address have been verified through the third-party verification service (e.g., IDology, Equifax). The system must receive a "PASS" or "CONDITIONAL PASS" verification result before enabling the bind/issue workflow.

#### Acceptance Criteria

- **AC-1:** The system must block the "Issue Policy" button if verification status is "PENDING" or "FAILED".
- **AC-2:** Once verification service returns "PASS", the bind workflow automatically becomes available.
- **AC-3:** If verification is "CONDITIONAL PASS", the system prompts for manager override before allowing binding.
- **AC-4:** Verification status must be persisted in the policy record and included in audit logs.

### Layer 3: Full Technical Details

#### Gherkin Test Cases

**Scenario A: Verification PASS — Auto-enable binding**
```gherkin
Given Applicant submits policy application
  And Third-party verification returns "PASS" status
When the policy status is evaluated
Then Bind/Issue button becomes enabled
  And User can immediately proceed to binding workflow
```

**Scenario B: Verification PENDING — Block binding**
```gherkin
Given Applicant submits policy application
  And Verification service is still processing (returns "PENDING")
When the policy status is evaluated
Then Bind/Issue button remains disabled
  And System displays message: "Verification in progress. Please try again in 5 minutes."
```

**Scenario C: Verification FAILED — Block with escalation**
```gherkin
Given Applicant submits policy application
  And Third-party verification returns "FAILED" (identity mismatch)
When the policy status is evaluated
Then Bind/Issue button is disabled
  And Application is routed to Manual Review queue
  And Underwriter receives notification: "Verification failed for applicant John Doe"
```

**Scenario D: Conditional PASS — Require override**
```gherkin
Given Applicant submits policy application
  And Third-party verification returns "CONDITIONAL PASS" (partial match on address)
When the policy status is evaluated
Then Bind/Issue button shows "Requires Manager Override"
  And Only users with role "UnderwritingManager" can click the override button
  And Override action is logged with timestamp and user ID
```

#### Examples

**Example 1: Happy path (PASS)**
```
Application ID: APP-2026-001234
Applicant: Jane Smith
SSN: 123-45-6789
Submitted: 2026-06-26 10:30 AM
Verification Request: IDology IdentityCheck
Verification Result: PASS (confidence: 99%)
Policy Status Transition: Quote → Verification Passed → Ready to Bind
Expected Behavior: Bind button enabled immediately
```

**Example 2: Conditional path (requires override)**
```
Application ID: APP-2026-001235
Applicant: Robert Johnson
Address on file: 123 Main St, Apt 4B, New York, NY 10001
Address returned by verification: 123 Main St, Apartment 4B, New York, NY 10001
Verification Result: CONDITIONAL PASS (address format variance)
Expected Behavior: Prompt manager to review and explicitly approve
Manager Action: "Override - Address variance acceptable"
Policy Status Transition: Quote → Verification Conditional → Manager Override → Ready to Bind
```

#### Exceptions

- **Exception 1: Verification service timeout** — If third-party service does not respond within 30 seconds, treat as "PENDING" and queue for async retry. User sees "Verification in progress" message.
- **Exception 2: Network failure during verification** — Log error, set status to "PENDING_RETRY", attempt automatic retry up to 3 times over 15 minutes. Notify user if verification cannot complete.
- **Exception 3: Invalid SSN format** — Reject verification request before sending to third party. Display validation error: "Please verify SSN format (XXX-XX-XXXX)".
- **Exception 4: Duplicate application** — If applicant has another active application with matching SSN, warn underwriter but allow proceeding if this is intentional (e.g., endorsement).

#### Implementation Notes

**Technology Stack:**
- Verification API: IDology IdentityCheck REST API (https://api.idology.com/v4/verification)
- Timeout: 30 seconds
- Retry policy: 3 attempts with exponential backoff (5s, 10s, 15s)
- Logging: All verification requests/responses to database table `VerificationAudit` with encrypted PII

**Database Changes:**
- Add column `VerificationStatus` to `ApplicationPolicy` table (enum: PENDING, PASS, CONDITIONAL_PASS, FAILED)
- Add column `VerificationTimestamp` (DateTime, UTC)
- Add column `VerificationReference` (string, for audit trail)

**Error Handling:**
- 400 Bad Request → Log, display "Invalid application data"
- 401 Unauthorized → Log critical, page on-call support immediately
- 503 Service Unavailable → Retry with exponential backoff, display "Verification temporarily unavailable"

**UI/UX Behavior:**
- Show real-time status badge next to applicant name: "Verification: PENDING" (spinner), "Verification: PASS" (green checkmark)
- Display verification timestamp to user for transparency
- Show manager override dialog with reason field (required)
- Email confirmation to applicant once verification passes

**Security Considerations:**
- PII (SSN, address) must be encrypted in transit and at rest
- Verification API key stored in secure secret vault, rotated quarterly
- All verification results logged to audit table with user/timestamp for compliance
- Mask SSN in UI after verification completes (show as XXX-XX-6789)

---

## Rule-Sample-002: Simpler Rule (Minimal Layer 2 & 3)

### Layer 1: Frontmatter

```yaml
id: Rule-Sample-002
title: Minimum Premium Threshold
category: SampleDomain.Pricing
tags: [pricing, threshold, business-rules]
created: 2026-06-26
lastReviewed: 2026-06-26
version: 1
canonicalSlug: rule-sample-002-minimum-premium-threshold
```

### Layer 2: Description

### Description

All policies must have a minimum annual premium of $250.00. Quotes below this threshold cannot be issued.

#### Acceptance Criteria

- **AC-1:** Premium calculation engine must check total premium against $250 minimum before enabling bind.
- **AC-2:** If premium is below minimum, display error: "Minimum premium is $250. Current quote is $[amount]."

### Layer 3: Details

#### Gherkin Test Cases

**Scenario A: Premium above threshold**
```gherkin
Given Annual premium = $500.00
When validation is executed
Then Quote is allowed to proceed to binding
```

**Scenario B: Premium below threshold**
```gherkin
Given Annual premium = $150.00
When validation is executed
Then Quote is rejected with error message
  And Bind button is disabled
```

#### Implementation Notes

- Check performed in `PremiumCalculationService.ValidateMinimum()`
- Configuration: `AppSettings.MinimumPremiumUsd = 250m`

---

## How to Use This Template

**When creating a new rule:**

1. Copy the Rule YAML block (Layer 1) and fill in id, title, category, tags, canonicalSlug
2. Add "### Description" and "### Acceptance Criteria" (Layer 2)
3. Add the Layer 3 sections if applicable:
   - "### Gherkin Test Cases"
   - "### Examples"
   - "### Exceptions"
   - "### Implementation Notes"

**Progressive disclosure mapping:**
- **Client requests Layer 1 only** → Parser returns YAML + empty sections = fast discovery
- **Client requests Layer 2** → Parser returns YAML + Description + Acceptance Criteria = medium detail
- **Client requests Layer 3** → Parser returns everything = full technical context

