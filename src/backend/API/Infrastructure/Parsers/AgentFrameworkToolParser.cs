using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using API.Domain;
using YamlDotNet.Serialization;
using YamlDotNet.Serialization.NamingConventions;

namespace API.Infrastructure.Parsers;

/// <summary>
/// Parses BRULS markdown files with YAML frontmatter into Agent Framework tool format.
/// Implements progressive disclosure: Layer 1 (frontmatter only), Layer 2 (sections), Layer 3 (full content).
/// Uses YamlDotNet for reliable YAML parsing.
/// </summary>
public sealed class AgentFrameworkToolParser
{
    private readonly IDeserializer _yamlDeserializer;

    public AgentFrameworkToolParser()
    {
        _yamlDeserializer = new DeserializerBuilder()
            .WithNamingConvention(CamelCaseNamingConvention.Instance)
            .IgnoreUnmatchedProperties()
            .Build();
    }

    /// <summary>
    /// Parse a BRULS markdown file into a RuleCollectionDocument.
    /// Supports lazy-loading: pass loadFullContent=false for layer 1+2 only (fast),
    /// or true for layer 3 (full details).
    /// </summary>
    public RuleCollectionDocument ParseRuleCollection(string filePath, bool loadFullContent = false)
    {
        if (!File.Exists(filePath))
            throw new FileNotFoundException($"Rule file not found: {filePath}");

        string fileContent = File.ReadAllText(filePath);

        // Extract and parse YAML frontmatter (Layer 1 - fast)
        var (frontmatterYaml, bodyContent) = ExtractFrontmatter(fileContent);
        
        var collection = ParseCollectionFrontmatter(frontmatterYaml);
        collection.FilePath = filePath;

        // Extract rules from body with embedded YAML blocks (Layers 2 & 3)
        collection.Rules = ParseRulesFromBody(bodyContent, loadFullContent);

        return collection;
    }

    /// <summary>
    /// Extract frontmatter YAML and remaining body content.
    /// Returns (frontmatterYaml, bodyContent).
    /// </summary>
    private (string frontmatter, string body) ExtractFrontmatter(string content)
    {
        const string delimiter = "---";
        int firstDelim = content.IndexOf(delimiter);
        
        if (firstDelim == -1)
            return (string.Empty, content);

        int secondDelim = content.IndexOf(delimiter, firstDelim + delimiter.Length);
        
        if (secondDelim == -1)
            return (string.Empty, content);

        string frontmatter = content.Substring(
            firstDelim + delimiter.Length,
            secondDelim - (firstDelim + delimiter.Length)
        ).Trim();

        string body = content.Substring(secondDelim + delimiter.Length).Trim();

        return (frontmatter, body);
    }

    /// <summary>
    /// Parse collection-level YAML frontmatter using YamlDotNet.
    /// </summary>
    private RuleCollectionDocument ParseCollectionFrontmatter(string yamlContent)
    {
        if (string.IsNullOrWhiteSpace(yamlContent))
            return new RuleCollectionDocument();

        try
        {
            var yamlData = _yamlDeserializer.Deserialize<Dictionary<string, object>>(yamlContent);
            
            var collection = new RuleCollectionDocument
            {
                Id = GetStringValue(yamlData, "id"),
                Title = GetStringValue(yamlData, "title"),
                Type = GetStringValue(yamlData, "type"),
                Source = GetStringValue(yamlData, "source"),
                Domain = GetStringValue(yamlData, "domain"),
                Created = GetStringValue(yamlData, "created"),
                LastReviewed = GetStringValue(yamlData, "lastReviewed"),
                Version = GetIntValue(yamlData, "version"),
                Summary = GetStringValue(yamlData, "summary"),
                Priority = GetStringValue(yamlData, "priority"),
            };

            if (yamlData.ContainsKey("author") && yamlData["author"] is Dictionary<object, object> authorDict)
            {
                collection.AuthorName = authorDict.ContainsKey("name") 
                    ? authorDict["name"]?.ToString() ?? string.Empty 
                    : string.Empty;
            }

            if (yamlData.ContainsKey("tags"))
            {
                collection.Tags = GetListValue(yamlData, "tags");
            }

            if (yamlData.ContainsKey("appliesTo"))
            {
                collection.AppliesTo = GetListValue(yamlData, "appliesTo");
            }

            return collection;
        }
        catch (Exception ex)
        {
            throw new InvalidOperationException("Failed to parse collection frontmatter YAML", ex);
        }
    }

    /// <summary>
    /// Parse rules from markdown body. Supports Layer 2 (summaries) and Layer 3 (full content).
    /// </summary>
    private List<RuleItem> ParseRulesFromBody(string bodyContent, bool loadFullContent)
    {
        var rules = new List<RuleItem>();

        // Split by YAML code blocks (```yaml ... ```)
        var yamlBlockPattern = @"```yaml\s*([\s\S]*?)\s*```";
        var matches = Regex.Matches(bodyContent, yamlBlockPattern);

        foreach (Match match in matches)
        {
            if (!match.Success) continue;

            string yamlBlock = match.Groups[1].Value;
            var ruleItem = ParseRuleMetadata(yamlBlock);

            if (string.IsNullOrEmpty(ruleItem.Id))
                continue;

            // Extract content sections from markdown (after the YAML block)
            int blockEnd = match.Index + match.Length;
            string contentAfterBlock = bodyContent.Substring(blockEnd);

            // Layer 2: Extract summaries and acceptance criteria
            ruleItem.PolicySummary = ExtractMarkdownSection(
                contentAfterBlock,
                "### Policy Summary",
                new[] { "### Acceptance Criteria", "### 📋 Acceptance Criteria" }
            );

            ruleItem.AcceptanceCriteria = ExtractMarkdownSection(
                contentAfterBlock,
                new[] { "### Acceptance Criteria", "### 📋 Acceptance Criteria" },
                new[] { "### Gherkin", "### 🧪 Gherkin Test Cases" }
            );

            // Layer 3: Full content (only if requested)
            if (loadFullContent)
            {
                ruleItem.Details = new RuleDetails
                {
                    GherkinTestCases = ExtractMarkdownSection(
                        contentAfterBlock,
                        new[] { "### Gherkin Test Cases", "### 🧪 Gherkin Test Cases" },
                        new[] { "```yaml", "## " }
                    ),
                    Examples = ExtractMarkdownSection(
                        contentAfterBlock,
                        new[] { "### Examples", "### 📚 Examples" },
                        new[] { "```yaml", "## " }
                    ),
                    Exceptions = ExtractMarkdownSection(
                        contentAfterBlock,
                        new[] { "### Exceptions", "### ⚠️ Exceptions" },
                        new[] { "```yaml", "## " }
                    ),
                    ImplementationNotes = ExtractMarkdownSection(
                        contentAfterBlock,
                        new[] { "### Implementation Notes", "### 💻 Implementation Notes" },
                        new[] { "```yaml", "## " }
                    ),
                };
            }

            rules.Add(ruleItem);
        }

        return rules;
    }

    /// <summary>
    /// Parse rule-level YAML metadata block using YamlDotNet.
    /// </summary>
    private RuleItem ParseRuleMetadata(string yamlBlock)
    {
        try
        {
            var yamlData = _yamlDeserializer.Deserialize<Dictionary<string, object>>(yamlBlock);
            
            var rule = new RuleItem
            {
                Id = GetStringValue(yamlData, "id"),
                Title = GetStringValue(yamlData, "title"),
                Category = GetStringValue(yamlData, "category"),
                CanonicalSlug = GetStringValue(yamlData, "canonicalSlug"),
                Tags = GetListValue(yamlData, "tags"),
            };

            return rule;
        }
        catch
        {
            // If YAML parsing fails, return empty rule item (will be skipped)
            return new RuleItem();
        }
    }

    /// <summary>
    /// Extract markdown section content between headers.
    /// startHeaders: array of possible header variations to start from
    /// endHeaders: array of possible header variations to stop at
    /// </summary>
    private string ExtractMarkdownSection(string content, string startHeader, string[] endHeaders)
    {
        return ExtractMarkdownSection(content, new[] { startHeader }, endHeaders);
    }

    private string ExtractMarkdownSection(string content, string[] startHeaders, string[] endHeaders)
    {
        int startIndex = -1;

        // Find the first matching start header
        foreach (var startHeader in startHeaders)
        {
            startIndex = content.IndexOf(startHeader, StringComparison.OrdinalIgnoreCase);
            if (startIndex != -1)
            {
                startIndex += startHeader.Length;
                break;
            }
        }

        if (startIndex == -1)
            return string.Empty;

        // Find the first matching end header
        int endIndex = content.Length;
        foreach (var endHeader in endHeaders)
        {
            int possibleEnd = content.IndexOf(endHeader, startIndex, StringComparison.OrdinalIgnoreCase);
            if (possibleEnd != -1 && possibleEnd < endIndex)
            {
                endIndex = possibleEnd;
            }
        }

        return content.Substring(startIndex, endIndex - startIndex).Trim();
    }

    /// <summary>
    /// Helper to safely extract string values from YAML dictionary.
    /// </summary>
    private string GetStringValue(Dictionary<string, object>? dict, string key)
    {
        if (dict == null || !dict.ContainsKey(key))
            return string.Empty;

        return dict[key]?.ToString() ?? string.Empty;
    }

    /// <summary>
    /// Helper to safely extract int values from YAML dictionary.
    /// </summary>
    private int GetIntValue(Dictionary<string, object>? dict, string key)
    {
        if (dict == null || !dict.ContainsKey(key))
            return 0;

        if (int.TryParse(dict[key]?.ToString(), out int value))
            return value;

        return 0;
    }

    /// <summary>
    /// Helper to safely extract list values from YAML dictionary.
    /// </summary>
    private List<string> GetListValue(Dictionary<string, object>? dict, string key)
    {
        if (dict == null || !dict.ContainsKey(key))
            return [];

        if (dict[key] is List<object> objList)
        {
            return objList.Select(o => o?.ToString() ?? string.Empty).ToList();
        }

        return [];
    }
}
