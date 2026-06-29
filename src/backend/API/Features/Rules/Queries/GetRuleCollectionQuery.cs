using System;
using System.IO;
using API.Domain;
using API.Infrastructure.Parsers;

namespace API.Features.Rules.Queries;

/// <summary>
/// Request to retrieve a rule collection with progressive disclosure support.
/// Minimal: Layer 1 only (frontmatter) - fast, minimal payload
/// Standard: Layer 1 + 2 (frontmatter + summaries)
/// Complete: Layer 1 + 2 + 3 (full technical details)
/// </summary>
public record GetRuleCollectionQuery(
    string FilePath,
    DisclosureLevel Level = DisclosureLevel.Standard
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

        // Parse with appropriate disclosure level method
        return query.Level switch
        {
            DisclosureLevel.Minimal => _parser.ParseRuleCollectionMinimal(fullPath),
            DisclosureLevel.Standard => _parser.ParseRuleCollectionStandard(fullPath),
            DisclosureLevel.Complete => _parser.ParseRuleCollectionComplete(fullPath),
            _ => _parser.ParseRuleCollectionStandard(fullPath),
        };
    }
}
