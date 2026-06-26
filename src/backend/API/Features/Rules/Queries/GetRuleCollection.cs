using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using API.Domain;
using API.Infrastructure.Parsers;

namespace API.Features.Rules.Queries;

/// <summary>
/// Request to retrieve a rule collection with progressive disclosure support.
/// Layer 1 (default): YAML frontmatter only - fast, minimal payload
/// Layer 2: Add section summaries (policy, acceptance criteria)
/// Layer 3: Add full technical details (test cases, examples, exceptions)
/// </summary>
public record GetRuleCollectionQuery(
    string FilePath,
    bool IncludeSummaries = true,    // Layer 2
    bool IncludeFullContent = false   // Layer 3
);

/// <summary>
/// Handler for GetRuleCollectionQuery.
/// Demonstrates vertical slice pattern: request, handler, parser, domain objects.
/// </summary>
public sealed class GetRuleCollectionHandler
{
    private readonly AgentFrameworkToolParser _parser;
    private readonly string _knowledgeBasePath;

    public GetRuleCollectionHandler(string knowledgeBasePath)
    {
        _parser = new AgentFrameworkToolParser();
        _knowledgeBasePath = knowledgeBasePath;
    }

    public RuleCollectionDocument Handle(GetRuleCollectionQuery query)
    {
        // Validate and resolve file path
        string fullPath = Path.Combine(_knowledgeBasePath, query.FilePath);
        
        if (!fullPath.StartsWith(Path.GetFullPath(_knowledgeBasePath)))
        {
            throw new UnauthorizedAccessException("Attempted path traversal");
        }

        if (!File.Exists(fullPath))
        {
            throw new FileNotFoundException($"Rule file not found: {query.FilePath}");
        }

        // Parse with requested disclosure level
        // Layer 3 implies Layer 2
        bool loadFull = query.IncludeFullContent || query.IncludeSummaries;
        var collection = _parser.ParseRuleCollection(fullPath, loadFull);

        // If only Layer 1 requested, clear section content
        if (!query.IncludeSummaries && !query.IncludeFullContent)
        {
            foreach (var rule in collection.Rules)
            {
                rule.PolicySummary = string.Empty;
                rule.AcceptanceCriteria = string.Empty;
                rule.Details = null;
            }
        }
        // If Layer 2 requested but not Layer 3, clear Layer 3 details
        else if (query.IncludeSummaries && !query.IncludeFullContent)
        {
            foreach (var rule in collection.Rules)
            {
                rule.Details = null;
            }
        }

        return collection;
    }
}

/// <summary>
/// Example: Retrieve all rule files from knowledge base directory.
/// Returns Layer 1 only (frontmatter) for fast browsing.
/// </summary>
public record ListRuleCollectionsQuery(
    string? Domain = null,  // Filter by domain
    string? Tag = null      // Filter by tag
);

public sealed class ListRuleCollectionsHandler
{
    private readonly AgentFrameworkToolParser _parser;
    private readonly string _knowledgeBasePath;

    public ListRuleCollectionsHandler(string knowledgeBasePath)
    {
        _parser = new AgentFrameworkToolParser();
        _knowledgeBasePath = knowledgeBasePath;
    }

    public List<RuleCollectionDocument> Handle(ListRuleCollectionsQuery query)
    {
        var collections = new List<RuleCollectionDocument>();
        var knowledgeBaseDir = new DirectoryInfo(_knowledgeBasePath);

        if (!knowledgeBaseDir.Exists)
        {
            return collections;
        }

        // Find all markdown files in knowledge base (recursive)
        var markdownFiles = knowledgeBaseDir.GetFiles("*.md", SearchOption.AllDirectories);

        foreach (var file in markdownFiles)
        {
            try
            {
                // Parse Layer 1 only (fast)
                var collection = _parser.ParseRuleCollection(file.FullName, loadFullContent: false);

                // Apply filters
                if (!string.IsNullOrEmpty(query.Domain) && collection.Domain != query.Domain)
                    continue;

                if (!string.IsNullOrEmpty(query.Tag) && !collection.Tags.Contains(query.Tag))
                    continue;

                collections.Add(collection);
            }
            catch
            {
                // Skip files that fail to parse
                continue;
            }
        }

        return collections;
    }
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

    public SearchRulesHandler(string knowledgeBasePath)
    {
        _parser = new AgentFrameworkToolParser();
        _knowledgeBasePath = knowledgeBasePath;
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
                var collection = _parser.ParseRuleCollection(file.FullName, loadFullContent: false);

                if (!string.IsNullOrEmpty(query.Domain) && collection.Domain != query.Domain)
                    continue;

                foreach (var rule in collection.Rules)
                {
                    // Simple substring matching (can be enhanced with semantic search)
                    var searchLower = query.Query.ToLower();
                    double relevance = CalculateRelevance(
                        query.Query, 
                        rule.Title, 
                        rule.PolicySummary, 
                        rule.AcceptanceCriteria
                    );

                    if (relevance > 0)
                    {
                        var fragment = new MatchedFragment
                        {
                            Quote = rule.PolicySummary,
                            RelevanceScore = relevance,
                            SourcePath = file.FullName,
                            Heading = rule.Title,
                            Section = "PolicySummary"
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
                    AnswerSummary = first.rule.PolicySummary,
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

    private double CalculateRelevance(string query, params string[] fields)
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
            var queryWords = queryLower.Split(new[] { ' ', ',', '.', '-' }, StringSplitOptions.RemoveEmptyEntries);
            var matchedWords = queryWords.Count(w => fieldLower.Contains(w));
            score += matchedWords * 0.5;
        }

        return Math.Min(score / fields.Length, 1.0);  // Normalize to 0-1
    }
}
