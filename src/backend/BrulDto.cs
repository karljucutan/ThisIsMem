public sealed class BrulDto
{
    // Layer 1: Top YAML Frontmatter Metadata
    public string Id { get; set; } = string.Empty;
    public string Title { get; set; } = string.Empty;
    public string Type { get; set; } = string.Empty;
    public string Source { get; set; } = string.Empty;
    public string Domain { get; set; } = string.Empty;
    public string Created { get; set; } = string.Empty;
    public string LastReviewed { get; set; } = string.Empty;
    public int Version { get; set; }
    public string AuthorName { get; set; } = string.Empty;

    // Layers 2 & 3: Grouped Rules parsed from the file body
    public List<RuleItemDto> Rules { get; set; } = new();
}

public sealed class RuleItemDto
{
    // Layer 2: Embedded Metadata & Main Summary
    public string Id { get; set; } = string.Empty;
    public string Title { get; set; } = string.Empty;
    public string Category { get; set; } = string.Empty;
    public List<string> Tags { get; set; } = new();
    public string CanonicalSlug { get; set; } = string.Empty;
    public string PolicySummary { get; set; } = string.Empty;

    // Layer 3: Lazy-loaded technical disclosures
    public RuleDetailsDto Details { get; set; } = new();
}

public sealed class RuleDetailsDto
{
    public string AcceptanceCriteria { get; set; } = string.Empty;
    public string GherkinTestCases { get; set; } = string.Empty;
}