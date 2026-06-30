using API.Domain;
using API.Infrastructure.Parsers;
using Microsoft.Extensions.Options;
using API.Infrastructure.Options;

namespace API.Features.Rules.Commands;

/// <summary>
/// Core handler for searching rules across the knowledge base.
/// Implements progressive disclosure with scoring and matching.
/// Used by both REST endpoints and agent tool handlers.
/// </summary>
public sealed class SearchRulesCommandHandler
{
    private readonly AgentFrameworkToolParser _parser;
    private readonly string _knowledgeBasePath;

    public SearchRulesCommandHandler(IOptions<KnowledgeBaseOptions> options)
    {
        _parser = new AgentFrameworkToolParser();
        _knowledgeBasePath = options.Value.Path;
    }

    public List<SearchRulesResult> Handle(SearchRulesCommand query)
    {
        var results = new List<SearchRulesResult>();
        var knowledgeBaseDir = new DirectoryInfo(_knowledgeBasePath);

        if (!knowledgeBaseDir.Exists)
            return results;

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
                return new SearchRulesResult
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
