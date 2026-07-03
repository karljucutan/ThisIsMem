---
title: Agent Framework Tool Parser Implementation
date: 2026-06-26
version: 1.0
---

## Agent Framework Tool Parser - Implementation Guide

## Overview

Implemented a **YamlDotNet-based parser** for Microsoft Agent Framework tools that supports **progressive disclosure** across three layers:

Current search flows default to **Layer 1 / Minimal** unless the caller explicitly asks for Standard or Complete.

## Why YamlDotNet?

✅ **Type-safe** - Deserializes YAML directly to .NET objects  
✅ **Standards-compliant** - Handles YAML 1.2 spec correctly  
✅ **Robust error handling** - Built-in validation and exception handling  
✅ **Performance** - Cached deserializer instance across parses  
✅ **Flexibility** - Customizable naming conventions (CamelCase), ignore unmatched properties  
✅ **No regex hacks** - Cleaner than manual string parsing for YAML  

**Alternative considered:** Manual YAML parsing with regex (what BusinessRulesParser does) - ❌ Fragile, harder to maintain, doesn't scale.

## Architecture

```text
Domain/
  └─ RuleAgentTool.cs          # Progressive disclosure DTOs
Infrastructure/Parsers/
  └─ AgentFrameworkToolParser.cs  # YamlDotNet-based parser
Features/Rules/Queries/
  └─ GetRuleCollection.cs      # Vertical slice handlers
```

### Key Design Decisions

1. **Separate from BusinessRulesParser** - Keep existing parser intact for backward compatibility
2. **Explicit disclosure selection** - Parse only what's needed via the `DisclosureLevel` on `SearchRulesCommand` or by calling the appropriate parser method directly
3. **Vertical Slice Architecture** - Queries (request types) + Handlers form cohesive feature slices
4. **Source traceability** - Every rule fragment includes file path, heading, and line number
5. **Agent Framework alignment** - DTOs designed for tool registration and invocation

## Domain Model (RuleAgentTool.cs)

### Progressive Disclosure Layers

```csharp
// Layer 1: Collection Metadata (YAML frontmatter - fast)
RuleCollectionDocument
  ├─ Id, Title, Type, Source
  ├─ Domain, Created, LastReviewed, Version
  ├─ AuthorName, Summary, Tags, AppliesTo, Priority
  └─ FilePath

// Layer 2: Rule Summaries (markdown sections - medium)
RuleItem
  ├─ Id, Title, Category, CanonicalSlug, Tags
  ├─ Summary           // From ### Summary
  ├─ AcceptanceCriteria      // From ### Acceptance Criteria
  └─ Source                  // For traceability

// Layer 3: Full Technical Details (on demand)
RuleDetails
  ├─ GherkinTestCases
  ├─ Examples
  ├─ Exceptions
  └─ ImplementationNotes
```

### Agent Framework Result Shape (SearchRulesResult)

```csharp
// Layer 1: Quick answer (default)
├─ AnswerSummary
├─ Confidence (Low/Medium/High)
└─ TopSources[]

// Layer 2: Supporting details (expand)
├─ Rationale
├─ SupportingMatches[]    // Quoted fragments with source paths
├─ RelatedRuleIds[]

// Layer 3: Full context (expand)
├─ RuleMetadata[]
└─ FullSourceMarkdown
```

## Parser Implementation (AgentFrameworkToolParser.cs)

### Public API

```csharp
// Parse a single file at an explicit disclosure level
RuleCollectionDocument ParseRuleCollectionMinimal(string filePath)
RuleCollectionDocument ParseRuleCollectionStandard(string filePath)
RuleCollectionDocument ParseRuleCollectionComplete(string filePath)

// Explicit minimal example
var layer1Only = parser.ParseRuleCollectionMinimal(filePath);
```

### Parsing Flow

1. **Extract frontmatter** → Find `---` delimiters, separate YAML from body
2. **Deserialize with YamlDotNet** → Type-safe conversion to Dictionary
3. **Extract rules** → Split by ````yaml` code blocks
4. **Parse rule metadata** → Each rule has embedded YAML block
5. **Extract sections** → Dynamically find markdown headings (Summary, Acceptance Criteria, etc.)
6. **Optional Layer 3** → Load test cases, examples, exceptions if requested

### Section Extraction

Supports flexible heading variations:

Uses `ExtractMarkdownSection()` with start/end header arrays for robustness.

## Search Rules Handler

### SearchRulesCommandHandler (Slice Pattern)

```csharp
// Request
var query = new SearchRulesCommand(
  Query: "minimum down payment",
  DisclosureLevel: DisclosureLevel.Minimal
);

// Handler
var handler = new SearchRulesCommandHandler(options);
var results = handler.Handle(query);
```

**Disclosure control:**

The current search handler still computes relevance from rule title, description, and acceptance criteria, then returns Layer 1 fields first with additional layer-capable fields available in the DTO.

Example search implementation with:

## Usage Example

```csharp
// In Program.cs or DI configuration
services.AddScoped<SearchRulesCommandHandler>();

// In endpoint handler
[HttpPost("rules/search")]
public IActionResult SearchRules([FromBody] SearchRulesCommand command)
{
  var results = _handler.Handle(command);
  return Ok(results);
}
```

## Integration with Microsoft Agent Framework

The parser output (`RuleCollectionDocument` and `SearchRulesResult`) is designed for seamless Agent Framework tool registration:

1. **Tool Definition** - Map `SearchRulesHandler` to an Agent Framework tool
2. **Tool Invocation** - Agent passes search query
3. **Structured Response** - Returns `SearchRulesResult` with Layer 1 fields first, while later-layer fields remain available for expansion
4. **Client-side UX** - Frontend can expand later layers on demand when the UI supports it

Example tool registration (pseudocode):

```csharp
agent.AddTool(
    name: "search_business_rules",
    handler: (query) => searchHandler.Handle(new SearchRulesCommand(query)),
    schema: new ToolSchema { ... }
);
```

## Dependencies Added

## Next Steps

1. **Add unit tests** for parser edge cases (missing sections, malformed YAML, etc.)
2. **Register handlers as Agent Framework tools** in `Program.cs`
3. **Create API endpoints** that expose the handlers
4. **Implement semantic search** in `SearchRulesHandler` (replace substring matching)
5. **Add caching** for frequently accessed rule collections
6. **Implement proper error handling** with ProblemDetails responses

## File Structure

```text
src/backend/API/
├── Domain/
│   └── RuleAgentTool.cs          # Progressive disclosure DTOs
├── Infrastructure/Parsers/
│   └── AgentFrameworkToolParser.cs  # YamlDotNet parser
└── Features/Rules/Queries/
    └── GetRuleCollection.cs       # Vertical slice handlers
```

## Backward Compatibility

The original `BusinessRulesParser.cs` and `BrulDto.cs` remain unchanged. The new parser is an alternative implementation with better YAML handling and progressive disclosure support.
