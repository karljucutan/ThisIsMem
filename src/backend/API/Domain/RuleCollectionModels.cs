namespace API.Domain;

/// <summary>
/// Collection-side DTOs: RuleCollectionDocument aggregate and children
/// Contains Layer 1..Layer 2 models used for discovery and progressive disclosure.
/// </summary>
public sealed class RuleCollectionDocument
{
    // Layer 1: Collection-level frontmatter metadata
    public string Id { get; set; } = string.Empty;
    public string Title { get; set; } = string.Empty;
    public string Type { get; set; } = string.Empty;
    public string Source { get; set; } = string.Empty;
    public string Domain { get; set; } = string.Empty;
    public string Created { get; set; } = string.Empty;
    public string LastReviewed { get; set; } = string.Empty;
    public int Version { get; set; }
    public string AuthorName { get; set; } = string.Empty;
    public string? Summary { get; set; }
    public List<string> Tags { get; set; } = [];
    public List<string> AppliesTo { get; set; } = [];
    public string? Priority { get; set; }

    // Layer 2: Rules extracted from document
    public List<RuleItem> Rules { get; set; } = [];

    // File metadata for layer 3
    public string FilePath { get; set; } = string.Empty;
}

public sealed class RuleItem
{
    // Layer 1: Rule-level frontmatter metadata (fast parse)
    public string Id { get; set; } = string.Empty;
    public string Title { get; set; } = string.Empty;
    public string Category { get; set; } = string.Empty;
    public string CanonicalSlug { get; set; } = string.Empty;
    public string? Description { get; set; }
    public List<string> Tags { get; set; } = [];

    // Layer 2: Summary and acceptance criteria (medium disclosure)
    public string PolicySummary { get; set; } = string.Empty;
    public string AcceptanceCriteria { get; set; } = string.Empty;

    // Layer 3: Full details (lazy-loaded on demand)
    public RuleDetails? Details { get; set; }

    // Source tracking for traceability
    public RuleSource Source { get; set; } = new();
}

public sealed class RuleDetails
{
    // Layer 3: Full technical details (expand)
    public string GherkinTestCases { get; set; } = string.Empty;
    public string Examples { get; set; } = string.Empty;
    public string Exceptions { get; set; } = string.Empty;
    public string ImplementationNotes { get; set; } = string.Empty;
}

public sealed class RuleSource
{
    public string FilePath { get; set; } = string.Empty;
    public string HeadingPath { get; set; } = string.Empty;  // e.g. "# Billing Rules / ## Rule-106"
    public int LineNumber { get; set; }
}
