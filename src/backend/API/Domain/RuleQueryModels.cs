namespace API.Domain;

/// <summary>
/// Query/result DTOs: Result shapes returned by search and retrieval features.
/// Separate aggregate for tool outputs and query responses.
/// </summary>
public sealed class RuleQueryResult
{
    // Layer 1: Quick answer (required)
    public string AnswerSummary { get; set; } = string.Empty;
    public ConfidenceLevel Confidence { get; set; } = ConfidenceLevel.Medium;
    public List<RuleReference> TopSources { get; set; } = [];

    // Layer 2: Supporting details (optional expand)
    public string? Rationale { get; set; }
    public List<MatchedFragment> SupportingMatches { get; set; } = [];
    public List<string> RelatedRuleIds { get; set; } = [];

    // Layer 3: Full context (optional expand)
    public List<RuleMetadata> RuleMetadata { get; set; } = [];
    public string? FullSourceMarkdown { get; set; }
}

public sealed class RuleReference
{
    public string RuleId { get; set; } = string.Empty;
    public string Title { get; set; } = string.Empty;
    public string Domain { get; set; } = string.Empty;
    public string FilePath { get; set; } = string.Empty;
}

public sealed class MatchedFragment
{
    public string Quote { get; set; } = string.Empty;
    public double RelevanceScore { get; set; }
    public string SourcePath { get; set; } = string.Empty;
    public string Heading { get; set; } = string.Empty;
    public string Section { get; set; } = string.Empty;  // e.g. "PolicySummary", "AcceptanceCriteria"
}

public sealed class RuleMetadata
{
    public string Id { get; set; } = string.Empty;
    public string Title { get; set; } = string.Empty;
    public string Domain { get; set; } = string.Empty;
    public List<string> Tags { get; set; } = [];
    public string? Owner { get; set; }
    public string LastReviewed { get; set; } = string.Empty;
    public int Version { get; set; }
}

public enum ConfidenceLevel
{
    Low = 1,
    Medium = 2,
    High = 3
}
