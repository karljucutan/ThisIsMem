using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using API.Domain;
using API.Infrastructure.Options;
using API.Infrastructure.Parsers;
using Microsoft.Extensions.Options;

namespace API.Features.Rules.Queries;

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
    public string Section { get; set; } = string.Empty;  // e.g. "Summary", "AcceptanceCriteria"
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

/// <summary>
/// Example: Search rules by content (to be used by Agent Framework for tool invocation).
/// Returns structured results with progressive disclosure layers.
/// </summary>
public record SearchRulesQuery(
    string Query,
    string? Domain = null,
    int TopResults = 5
);

public sealed class SearchRulesHandler
{
    private readonly AgentFrameworkToolParser _parser;
    private readonly string _knowledgeBasePath;

    public SearchRulesHandler(IOptions<KnowledgeBaseOptions> options)
    {
        _parser = new AgentFrameworkToolParser();
        _knowledgeBasePath = options.Value.Path;
    }

    public List<RuleQueryResult> Handle(SearchRulesQuery query)
    {
        var results = new List<RuleQueryResult>();
        var knowledgeBaseDir = new DirectoryInfo(_knowledgeBasePath);

        if (!knowledgeBaseDir.Exists)
        {
            return results;
        }

        var markdownFiles = knowledgeBaseDir.GetFiles("*.md", SearchOption.AllDirectories);
        var matches = new List<(RuleCollectionDocument collection, RuleItem rule, MatchedFragment fragment)>();

        // Search across all rules
        foreach (var file in markdownFiles)
        {
            try
            {
                // Standard = Layer 1 + 2: required to score and return Description and AcceptanceCriteria
                var collection = _parser.ParseRuleCollectionStandard(file.FullName);

                if (!string.IsNullOrEmpty(query.Domain) && collection.Domain != query.Domain)
                    continue;

                foreach (var rule in collection.Rules)
                {
                    // Simple substring matching (can be enhanced with semantic search)
                    var searchLower = query.Query.ToLower();
                    double relevance = CalculateRelevance(
                        query.Query, 
                        rule.Title,
                        rule.Description,
                        rule.AcceptanceCriteria
                    );

                    if (relevance > 0)
                    {
                        var fragment = new MatchedFragment
                        {
                            Quote = rule.Description,
                            RelevanceScore = relevance,
                            SourcePath = file.FullName,
                            Heading = rule.Title,
                            Section = "Description"
                        };

                        matches.Add((collection, rule, fragment));
                    }
                }
            }
            catch
            {
                continue;
            }
        }

        // Build results from top matches
        var topMatches = matches
            .OrderByDescending(m => m.fragment.RelevanceScore)
            .Take(query.TopResults)
            .GroupBy(m => m.rule.Id)
            .Select(g =>
            {
                var first = g.First();
                return new RuleQueryResult
                {
                    AnswerSummary = first.rule.Description,
                    Confidence = ConfidenceLevel.High,
                    TopSources =
                    [
                        new RuleReference
                        {
                            RuleId = first.rule.Id,
                            Title = first.rule.Title,
                            Domain = first.collection.Domain,
                            FilePath = first.collection.FilePath
                        }
                    ],
                    SupportingMatches = g.Select(m => m.fragment).ToList(),
                    RuleMetadata =
                    [
                        new RuleMetadata
                        {
                            Id = first.rule.Id,
                            Title = first.rule.Title,
                            Domain = first.collection.Domain,
                            Tags = first.rule.Tags,
                            LastReviewed = first.collection.LastReviewed,
                            Version = first.collection.Version
                        }
                    ]
                };
            })
            .ToList();

        return topMatches;
    }

    private static double CalculateRelevance(string query, params string[] fields)
    {
        double score = 0;
        var queryLower = query.ToLower();

        foreach (var field in fields)
        {
            if (string.IsNullOrEmpty(field)) continue;

            var fieldLower = field.ToLower();
            
            // Exact match
            if (fieldLower.Contains(queryLower))
                score += 1.0;

            // Word matches
            var queryWords = queryLower.Split([' ', ',', '.', '-'], StringSplitOptions.RemoveEmptyEntries);
            var matchedWords = queryWords.Count(w => fieldLower.Contains(w));
            score += matchedWords * 0.5;
        }

        return Math.Min(score / fields.Length, 1.0);  // Normalize to 0-1
    }
}
