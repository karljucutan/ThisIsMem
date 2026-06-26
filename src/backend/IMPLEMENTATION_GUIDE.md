---
title: Agent Framework Tool Parser Implementation
date: 2026-06-26
version: 1.0
---

# Agent Framework Tool Parser - Implementation Guide

## Overview

Implemented a **YamlDotNet-based parser** for Microsoft Agent Framework tools that supports **progressive disclosure** across three layers:

- **Layer 1** (fast): YAML frontmatter only
- **Layer 2** (medium): Add section summaries (policy, acceptance criteria)
- **Layer 3** (full): Add technical details (test cases, examples, exceptions)

## Why YamlDotNet?

✅ **Type-safe** - Deserializes YAML directly to .NET objects  
✅ **Standards-compliant** - Handles YAML 1.2 spec correctly  
✅ **Robust error handling** - Built-in validation and exception handling  
✅ **Performance** - Cached deserializer instance across parses  
✅ **Flexibility** - Customizable naming conventions (CamelCase), ignore unmatched properties  
✅ **No regex hacks** - Cleaner than manual string parsing for YAML  

**Alternative considered:** Manual YAML parsing with regex (what BusinessRulesParser does) - ❌ Fragile, harder to maintain, doesn't scale.

## Architecture

```
Domain/
  └─ RuleAgentTool.cs          # Progressive disclosure DTOs
Infrastructure/Parsers/
  └─ AgentFrameworkToolParser.cs  # YamlDotNet-based parser
Features/Rules/Queries/
  └─ GetRuleCollection.cs      # Vertical slice handlers
```

### Key Design Decisions

1. **Separate from BusinessRulesParser** - Keep existing parser intact for backward compatibility
2. **Lazy-loading support** - Parse only what's needed via `loadFullContent` parameter
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
  ├─ PolicySummary           // From ### Policy Summary
  ├─ AcceptanceCriteria      // From ### Acceptance Criteria
  └─ Source                  // For traceability

// Layer 3: Full Technical Details (on demand)
RuleDetails
  ├─ GherkinTestCases
  ├─ Examples
  ├─ Exceptions
  └─ ImplementationNotes
```

### Agent Framework Result Shape (RuleQueryResult)

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
// Parse a single file (default: Layer 1+2, optional Layer 3)
RuleCollectionDocument ParseRuleCollection(string filePath, bool loadFullContent = false)

// Lazy-loading example
var layer1Only = parser.ParseRuleCollection(filePath, loadFullContent: false);
var fullDetails = parser.ParseRuleCollection(filePath, loadFullContent: true);
```

### Parsing Flow

1. **Extract frontmatter** → Find `---` delimiters, separate YAML from body
2. **Deserialize with YamlDotNet** → Type-safe conversion to Dictionary
3. **Extract rules** → Split by ````yaml` code blocks
4. **Parse rule metadata** → Each rule has embedded YAML block
5. **Extract sections** → Dynamically find markdown headings (Policy Summary, Acceptance Criteria, etc.)
6. **Optional Layer 3** → Load test cases, examples, exceptions if requested

### Section Extraction

Supports flexible heading variations:
- `### Policy Summary` or `### 📋 Policy Summary`
- `### Acceptance Criteria` or `### 📋 Acceptance Criteria`
- `### Gherkin Test Cases` or `### 🧪 Gherkin Test Cases`

Uses `ExtractMarkdownSection()` with start/end header arrays for robustness.

## Vertical Slice Handlers (GetRuleCollection.cs)

### GetRuleCollectionHandler (Slice Pattern)

```csharp
// Request
var query = new GetRuleCollectionQuery(
    FilePath: "bruls-billing-installments-grouped.md",
    IncludeSummaries: true,
    IncludeFullContent: false
);

// Handler
var handler = new GetRuleCollectionHandler(knowledgeBasePath);
var collection = handler.Handle(query);
```

**Disclosure control:**
- `query.IncludeSummaries = false` → Layer 1 only
- `query.IncludeSummaries = true` → Layers 1+2
- `query.IncludeFullContent = true` → Layers 1+2+3

### ListRuleCollectionsHandler

Browse all rules in knowledge base with optional domain/tag filtering.
Always returns Layer 1 (fast).

### SearchRulesHandler

Example search implementation with:
- Substring matching (can be enhanced with semantic search/embeddings)
- Relevance scoring
- Top-N results
- Domain filtering
- Returns `RuleQueryResult` for Agent Framework tool output

## Usage Example

```csharp
// In Program.cs or DI configuration
var knowledgeBasePath = Path.Combine(AppContext.BaseDirectory, "..", "..", "knowledgebase");
services.AddScoped(_ => new AgentFrameworkToolParser());
services.AddScoped(_ => new GetRuleCollectionHandler(knowledgeBasePath));
services.AddScoped(_ => new SearchRulesHandler(knowledgeBasePath));

// In endpoint handler
[HttpGet("rules/{fileName}")]
public IActionResult GetRule(string fileName, [FromQuery] bool fullContent = false)
{
    var query = new GetRuleCollectionQuery(
        FilePath: $"{fileName}.md",
        IncludeSummaries: true,
        IncludeFullContent: fullContent
    );
    
    var collection = _handler.Handle(query);
    return Ok(collection);
}
```

## Integration with Microsoft Agent Framework

The parser output (`RuleCollectionDocument` and `RuleQueryResult`) is designed for seamless Agent Framework tool registration:

1. **Tool Definition** - Map `SearchRulesHandler` to an Agent Framework tool
2. **Tool Invocation** - Agent passes search query
3. **Structured Response** - Returns `RuleQueryResult` with progressive disclosure layers
4. **Client-side UX** - Frontend expands layers on user demand

Example tool registration (pseudocode):
```csharp
agent.AddTool(
    name: "search_business_rules",
    handler: (query) => searchHandler.Handle(new SearchRulesQuery(query)),
    schema: new ToolSchema { ... }
);
```

## Dependencies Added

- **YamlDotNet 15.1.1** - `API.csproj`

## Next Steps

1. **Add unit tests** for parser edge cases (missing sections, malformed YAML, etc.)
2. **Register handlers as Agent Framework tools** in `Program.cs`
3. **Create API endpoints** that expose the handlers
4. **Implement semantic search** in `SearchRulesHandler` (replace substring matching)
5. **Add caching** for frequently accessed rule collections
6. **Implement proper error handling** with ProblemDetails responses

## File Structure

```
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
