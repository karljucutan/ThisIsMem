# Glossary and Synonym Resolution Tool - Implementation Guide

## Overview

This document details the implementation flow, approval workflow, and code changes for the PostgreSQL-backed Glossary tool.

**Approval Strategy: Option B (Safe)**
- Require manual user approval before using synonyms in searches
- Agent asks user to confirm/define unknown terms
- User provides feedback via chat
- Glossary updated with `IsApproved: false` (pending)
- Only approved synonyms are used for auto-translation in future queries

---

## User Flow

```
User asks: "what's the policy on late fees?"
    ↓
Agent resolves "late fees" → NOT FOUND in glossary
    ↓
Agent responds to user:
"I don't recognize 'late fees'. 
Did you mean one of these?
- Late Fee Penalty
- Overdue Charge
- Or provide a new term definition"
    ↓
User responds: "yes, it's Late Fee Penalty"
    ↓
Agent calls RegisterTermCommand:
  OfficialTerm: "Late Fee Penalty"
  InitialSynonyms: ["late fees"]
  IsApproved: false (pending)
    ↓
Database updated
    ↓
Agent now searches rules using "Late Fee Penalty"
```

---

## Implementation Changes Needed

### 1. Update ResolveTermResult Model

Add new fields to support user approval workflow:

```csharp
public record ResolveTermResult
{
    public bool Found { get; set; }
    public string? OfficialTerm { get; set; }
    public string? Definition { get; set; }
    public List<string> Synonyms { get; set; } = [];
    
    // NEW: Suggestions for disambiguation
    public List<string> SuggestedTerms { get; set; } = [];
    public bool RequiresUserApproval { get; set; } // Flag for agent
    public string? UserInput { get; set; } // Original term user asked about
}
```

**When to set these fields:**
- `Found = true`: Exact or approved synonym match found
- `Found = false` + `RequiresUserApproval = true`: Unknown term, show disambiguation UI
- `SuggestedTerms`: Fuzzy matches from glossary_terms table (top 3)
- `UserInput`: The original term the user asked about (for registration later)

### 2. ResolveTermCommandHandler - If Not Found, Ask User

**Key behavior:** When term is not found, return `RequiresUserApproval = true` with suggested terms from fuzzy match. Do NOT auto-register.

```csharp
public sealed class ResolveTermCommandHandler
{
    private readonly ApplicationDbContext _db;

    public ResolveTermCommandHandler(ApplicationDbContext db)
    {
        _db = db;
    }

    public async Task<ResolveTermResult> Handle(ResolveTermCommand command)
    {
        var userInput = command.UserInput.ToLower();

        // Try exact match on approved synonyms
        var exactMatch = await _db.TermSynonyms
            .Include(ts => ts.GlossaryTerm)
            .FirstOrDefaultAsync(ts => ts.Synonym.ToLower() == userInput && ts.IsApproved);

        if (exactMatch != null)
        {
            return new ResolveTermResult
            {
                Found = true,
                OfficialTerm = exactMatch.GlossaryTerm.OfficialTerm,
                Definition = exactMatch.GlossaryTerm.Definition,
                Synonyms = await GetSynonyms(exactMatch.GlossaryTermId)
            };
        }

        // Try fuzzy match on official terms
        var fuzzyMatches = await _db.GlossaryTerms
            .Where(gt => EF.Functions.Like(gt.OfficialTerm, $"%{userInput}%") && gt.Status == "active")
            .Take(3)
            .Select(gt => gt.OfficialTerm)
            .ToListAsync();

        // NOT FOUND — return suggestions for user to approve
        return new ResolveTermResult
        {
            Found = false,
            UserInput = userInput,
            SuggestedTerms = fuzzyMatches,
            RequiresUserApproval = true
        };
    }

    private async Task<List<string>> GetSynonyms(int glossaryTermId)
    {
        return await _db.TermSynonyms
            .Where(ts => ts.GlossaryTermId == glossaryTermId && ts.IsApproved)
            .Select(ts => ts.Synonym)
            .ToListAsync();
    }
}
```

### 3. Frontend - Disambiguation UI

When `RequiresUserApproval = true`, show:
- Suggested existing terms (user can click to approve)
- Option to provide new term definition

```tsx
// Example React component (shadcn/ui)
export function TermDisambiguation({ 
  userInput, 
  suggestedTerms, 
  onApprove, 
  onRegisterNew 
}: Props) {
  const [newDefinition, setNewDefinition] = useState("");

  return (
    <div className="border rounded-lg p-4 bg-yellow-50">
      <p className="font-semibold">I don't recognize "{userInput}"</p>
      <p className="text-sm text-gray-600 mb-4">
        Did you mean one of these?
      </p>
      
      <div className="flex flex-col gap-2">
        {suggestedTerms.map(term => (
          <Button 
            key={term}
            variant="outline"
            onClick={() => onApprove(userInput, term)}
          >
            ✓ {term}
          </Button>
        ))}
      </div>
      
      <div className="mt-4 pt-4 border-t">
        <p className="text-sm mb-2">Or provide a new definition:</p>
        <textarea 
          placeholder="Define this term..."
          value={newDefinition}
          onChange={(e) => setNewDefinition(e.target.value)}
          className="w-full p-2 border rounded"
        />
        <Button 
          className="mt-2"
          onClick={() => onRegisterNew(userInput, newDefinition)}
        >
          Register New Term
        </Button>
      </div>
    </div>
  );
}
```

### 4. Backend - On User Approval

When user approves or provides definition, call `RegisterTermCommand` with:
- `OfficialTerm`: the approved term
- `InitialSynonyms`: the original user input
- `IsApproved`: false (pending - safe by default)

Then resolve again and continue with rule search using the official term.

```csharp
public sealed class GlossaryApprovalHandler
{
    private readonly RegisterTermCommandHandler _registerHandler;
    private readonly ResolveTermCommandHandler _resolveHandler;

    public async Task<ApprovalResult> OnUserApproval(
        string userInput, 
        string approvedOfficialTerm,
        string? definition = null)
    {
        // Register the term with user-approved synonym
        var command = new RegisterTermCommand(
            OfficialTerm: approvedOfficialTerm,
            Definition: definition ?? "", 
            Domain: "General",
            InitialSynonyms: new[] { userInput }
        );

        var registerResult = await _registerHandler.Handle(command);
        
        if (!registerResult.Success)
            return new ApprovalResult { Success = false, Message = registerResult.Message };

        // Now resolve again with the approved term
        var resolveResult = await _resolveHandler.Handle(
            new ResolveTermCommand(userInput, IncludeSynonyms: true)
        );
        
        // Continue with rule search using resolveResult.OfficialTerm
        return new ApprovalResult 
        { 
            Success = true, 
            ResolvedTerm = resolveResult.OfficialTerm,
            TermId = registerResult.TermId
        };
    }
}
```

---

## Summary

| Step | Component | Action |
|------|-----------|--------|
| 1 | Agent | Calls `ResolveTermCommand` with user input |
| 2 | ResolveTermCommandHandler | Searches glossary; if not found, returns `RequiresUserApproval = true` |
| 3 | Frontend | Shows disambiguation UI with suggestions + input field |
| 4 | User | Clicks suggested term OR types new definition |
| 5 | Frontend | Calls backend approval endpoint |
| 6 | Backend | Calls `RegisterTermCommand` with user feedback |
| 7 | RegisterTermCommandHandler | Inserts into DB with `IsApproved = false` (safe default) |
| 8 | Backend | Resolves term again and continues rule search |
| 9 | Agent | Uses official term to search rules |
| 10 | Frontend | Shows rule results to user |

---

## Key Implementation Notes

- **No auto-registration**: Agent never writes to glossary without user approval
- **Safe defaults**: `IsApproved = false` on all new synonyms
- **Fuzzy matching**: Use `EF.Functions.Like()` or PostgreSQL `pg_trgm` extension
- **Only approved synonyms used**: In searches, only match against `IsApproved = true` rows
- **Frequency tracking**: Increment `frequency` on each successful resolution (useful for future analytics)
- **Approval workflow**: Future work could auto-approve high-frequency synonyms after threshold

---

## Testing Checklist

- [ ] Exact match: User input matches approved synonym → Found = true
- [ ] Fuzzy match: User input partially matches official term → Suggests top 3
- [ ] No match: User input has no fuzzy matches → Suggests empty, requires user definition
- [ ] User approval flow: User approves suggestion → Term registered, synonym created with IsApproved = false
- [ ] New term registration: User provides definition → Term created, synonym linked
- [ ] Post-approval search: After approval, same term should resolve using new synonym
- [ ] Only approved used: Unapproved synonyms should NOT match in searches
