using System.ComponentModel;

namespace API.Features.Rules.Commands;

/// <summary>
/// Progressive disclosure levels for rule retrieval.
/// Minimal: Layer 1 only (frontmatter) — fast, minimal payload
/// Standard: Layer 1 + 2 (frontmatter + Description + Acceptance Criteria)
/// Complete: Layer 1 + 2 + 3 (full technical details including Gherkin, Examples, Exceptions, Implementation Notes)
/// </summary>
public enum DisclosureLevel
{
    Minimal,
    Standard,
    Complete,
}

/// <summary>
/// Result DTO for rule search commands using progressive disclosure layers.
/// Layer 1 (required): Quick answer with confidence and top sources.
/// Layer 2 (optional): Supporting details including rationale and matched fragments.
/// Layer 3 (optional): Full context with complete metadata and source markdown.
/// </summary>
public sealed class SearchRulesResult
{
    [Description("[Layer 1] Concise summary answering the search query.")]
    public string AnswerSummary { get; set; } = string.Empty;
    
    [Description("[Layer 1] Confidence level of the answer (Low, Medium, High).")]
    public ConfidenceLevel Confidence { get; set; } = ConfidenceLevel.Medium;
    
    [Description("[Layer 1] Top matching rules that address the query.")]
    public List<RuleReference> TopSources { get; set; } = [];

    [Description("[Layer 2] Explanation of why these rules are relevant.")]
    public string? Rationale { get; set; }
    
    [Description("[Layer 2] Text fragments from rules that matched the search query.")]
    public List<MatchedFragment> SupportingMatches { get; set; } = [];
    
    [Description("[Layer 2] IDs of related rules that may provide additional context.")]
    public List<string> RelatedRuleIds { get; set; } = [];

    [Description("[Layer 3] Complete metadata for each matching rule.")]
    public List<RuleMetadata> RuleMetadata { get; set; } = [];
    
    [Description("[Layer 3] Full source markdown content of the rule.")]
    public string? FullSourceMarkdown { get; set; }
}

/// <summary>
/// A reference to a specific rule as a top source match.
/// </summary>
public sealed class RuleReference
{
    [Description("Unique identifier for the rule.")]
    public string RuleId { get; set; } = string.Empty;
    
    [Description("Human-readable title of the rule.")]
    public string Title { get; set; } = string.Empty;
    
    [Description("Domain or business area this rule applies to.")]
    public string Domain { get; set; } = string.Empty;
    
    [Description("File path where this rule is stored in the knowledge base.")]
    public string FilePath { get; set; } = string.Empty;
}

/// <summary>
/// A matched text fragment from a rule that meets the search criteria.
/// </summary>
public sealed class MatchedFragment
{
    [Description("The excerpt of text that matched the search query.")]
    public string Quote { get; set; } = string.Empty;
    
    [Description("Relevance score (0.0 to 1.0) indicating how well this fragment matched the query.")]
    public double RelevanceScore { get; set; }
    
    [Description("Full file path where this fragment was found.")]
    public string SourcePath { get; set; } = string.Empty;
    
    [Description("The heading/title of the rule this fragment belongs to.")]
    public string Heading { get; set; } = string.Empty;
    
    [Description("Section name within the rule (e.g. \"Description\", \"AcceptanceCriteria\").")]
    public string Section { get; set; } = string.Empty;
}

/// <summary>
/// Full metadata for a rule including domain, version, and ownership information.
/// </summary>
public sealed class RuleMetadata
{
    [Description("Unique identifier for the rule.")]
    public string Id { get; set; } = string.Empty;
    
    [Description("Human-readable title of the rule.")]
    public string Title { get; set; } = string.Empty;
    
    [Description("Domain or business area this rule applies to (e.g. \"Billing\", \"Underwriting\").")]
    public string Domain { get; set; } = string.Empty;
    
    [Description("List of tags associated with this rule for categorization.")]
    public List<string> Tags { get; set; } = [];
    
    [Description("Name of the person responsible for this rule.")]
    public string? Owner { get; set; }
    
    [Description("Date when this rule was last reviewed (ISO 8601 format).")]
    public string LastReviewed { get; set; } = string.Empty;
    
    [Description("Version number of this rule.")]
    public int Version { get; set; }
}

/// <summary>
/// Confidence level for rule search results.
/// </summary>
public enum ConfidenceLevel
{
    Low = 1,
    Medium = 2,
    High = 3
}

/// <summary>
/// Command for searching rules by content. Returns structured results with progressive disclosure layers.
/// Designed for both agent framework tool invocation and manual REST API testing.
/// </summary>
public record SearchRulesCommand(
    [Description("The search query text. Searches across rule titles, descriptions, and acceptance criteria.")]
    string Query,
    
    [Description("Optional domain filter to limit results to a specific business area (e.g. \"Billing\", \"Underwriting\").")]
    string? Domain = null,
    
    [Description("Maximum number of results to return. Defaults to 5.")]
    int TopResults = 5,

    [Description("Disclosure level for parsing the rule source. Minimal returns Layer 1 only, Standard returns Layers 1 and 2, Complete returns Layers 1, 2, and 3.")]
    DisclosureLevel DisclosureLevel = DisclosureLevel.Minimal
);
