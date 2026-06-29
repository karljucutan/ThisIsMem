using System;
using System.Collections.Generic;
using System.IO;
using API.Domain;
using API.Infrastructure.Parsers;

namespace API.Features.Rules.Queries;

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
                // Parse Layer 1 only (fast discovery)
                var collection = _parser.ParseRuleCollectionMinimal(file.FullName);

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
