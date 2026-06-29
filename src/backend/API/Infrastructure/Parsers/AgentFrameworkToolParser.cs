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
/// Uses YamlDotNet for reliable YAML parsing with typed deserialization.
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
    /// Parse Layer 1 only (frontmatter: id, title, category, tags, slug).
    /// Fastest disclosure level for discovery and browsing.
    /// </summary>
    public RuleCollectionDocument ParseRuleCollectionMinimal(string filePath)
    {
        return ParseRuleCollectionInternal(filePath, loadLayer2: false, loadLayer3: false);
    }

    /// <summary>
    /// Parse Layer 1 + 2 (frontmatter + Description + Acceptance Criteria).
    /// Balanced disclosure for standard queries and summaries.
    /// </summary>
    public RuleCollectionDocument ParseRuleCollectionStandard(string filePath)
    {
        return ParseRuleCollectionInternal(filePath, loadLayer2: true, loadLayer3: false);
    }

    /// <summary>
    /// Parse Layer 1 + 2 + 3 (full details including Gherkin Test Cases, Examples, Exceptions, Implementation Notes).
    /// Complete disclosure for detailed technical reference.
    /// </summary>
    public RuleCollectionDocument ParseRuleCollectionComplete(string filePath)
    {
        return ParseRuleCollectionInternal(filePath, loadLayer2: true, loadLayer3: true);
    }

    /// <summary>
    /// Internal method: Parse BRULS markdown file with specified disclosure layers.
    /// </summary>
    private RuleCollectionDocument ParseRuleCollectionInternal(string filePath, bool loadLayer2, bool loadLayer3)
    {
        if (!File.Exists(filePath))
            throw new FileNotFoundException($"Rule file not found: {filePath}");

        string fileContent = File.ReadAllText(filePath);

        // Extract and parse YAML frontmatter (Layer 1 - always fast)
        var (frontmatterYaml, bodyContent) = ExtractFrontmatter(fileContent);
        
        var collection = ParseCollectionFrontmatter(frontmatterYaml);
        collection.FilePath = filePath;

        // Extract rules from body with embedded YAML blocks (Layers 2 & 3 based on flags)
        collection.Rules = ParseRulesFromBody(bodyContent, loadLayer2, loadLayer3, collection);

        return collection;
    }

    /// <summary>
    /// Extract frontmatter YAML and remaining body content.
    /// Returns (frontmatterYaml, bodyContent).
    /// </summary>
    private static (string frontmatter, string body) ExtractFrontmatter(string content)
    {
        const string delimiter = "---";
        int firstDelim = content.IndexOf(delimiter);
        
        if (firstDelim == -1)
            return (string.Empty, content);

        int secondDelim = content.IndexOf(delimiter, firstDelim + delimiter.Length);
        
        if (secondDelim == -1)
            return (string.Empty, content);

        string frontmatter = content[
            (firstDelim + delimiter.Length)..secondDelim].Trim();

        string body = content[(secondDelim + delimiter.Length)..].Trim();

        return (frontmatter, body);
    }

    /// <summary>
    /// Parse collection-level YAML frontmatter using typed deserialization.
    /// </summary>
    private RuleCollectionDocument ParseCollectionFrontmatter(string yamlContent)
    {
        if (string.IsNullOrWhiteSpace(yamlContent))
            return new RuleCollectionDocument();

        try
        {
            var frontmatter = _yamlDeserializer.Deserialize<CollectionFrontmatterDto>(yamlContent) 
                ?? new CollectionFrontmatterDto();
            
            return new RuleCollectionDocument
            {
                Id = frontmatter.Id ?? string.Empty,
                Title = frontmatter.Title ?? string.Empty,
                Type = frontmatter.Type ?? string.Empty,
                Source = frontmatter.Source ?? string.Empty,
                Domain = frontmatter.Domain ?? string.Empty,
                Created = frontmatter.Created ?? string.Empty,
                LastReviewed = frontmatter.LastReviewed ?? string.Empty,
                Version = frontmatter.Version,
                AuthorName = frontmatter.Author?.Name ?? string.Empty,
                Description = frontmatter.Description ?? string.Empty,
                Priority = frontmatter.Priority,
                Tags = frontmatter.Tags ?? [],
                AppliesTo = frontmatter.AppliesTo ?? [],
            };
        }
        catch (Exception ex)
        {
            throw new InvalidOperationException("Failed to parse collection frontmatter YAML", ex);
        }
    }

    /// <summary>
    /// Parse rules from markdown body. Supports Layer 2 (Acceptance Criteria) and Layer 3 (full content).
    /// </summary>
    private List<RuleItem> ParseRulesFromBody(string bodyContent, bool loadLayer2, bool loadLayer3, RuleCollectionDocument collection)
    {
        var rules = new List<RuleItem>();

        // Split by YAML code blocks (```yaml ... ```)
        var yamlBlockPattern = @"```yaml\s*([\s\S]*?)\s*```";
        var matches = Regex.Matches(bodyContent, yamlBlockPattern);

        if (matches.Count == 0)
        {
            var singleRule = new RuleItem
            {
                Id = collection.Id,
                Title = collection.Title,
                Category = string.Empty,
                CanonicalSlug = string.Empty,
                Description = collection.Description,
                Tags = [.. collection.Tags],
                Source = new RuleSource
                {
                    FilePath = collection.FilePath,
                    HeadingPath = collection.Title,
                    LineNumber = 1,
                },
            };

            PopulateRuleSections(singleRule, bodyContent, loadLayer2, loadLayer3);
            rules.Add(singleRule);
            return rules;
        }

        foreach (Match match in matches)
        {
            if (!match.Success) continue;

            string yamlBlock = match.Groups[1].Value;
            var ruleItem = ParseRuleMetadata(yamlBlock);

            if (string.IsNullOrEmpty(ruleItem.Id))
                continue;

            // Extract content sections from markdown (after the YAML block)
            int blockEnd = match.Index + match.Length;
            string contentAfterBlock = bodyContent[blockEnd..];

            PopulateRuleSections(ruleItem, contentAfterBlock, loadLayer2, loadLayer3);

            rules.Add(ruleItem);
        }

        return rules;
    }

    /// <summary>
    /// Parse rule-level YAML metadata block using typed deserialization.
    /// </summary>
    private RuleItem ParseRuleMetadata(string yamlBlock)
    {
        try
        {
            var metadata = _yamlDeserializer.Deserialize<RuleMetadataDto>(yamlBlock) 
                ?? new RuleMetadataDto();
            
            return new RuleItem
            {
                Id = metadata.Id ?? string.Empty,
                Title = metadata.Title ?? string.Empty,
                Category = metadata.Category ?? string.Empty,
                CanonicalSlug = metadata.CanonicalSlug ?? string.Empty,
                Description = metadata.Description ?? string.Empty,
                Tags = metadata.Tags ?? [],
            };
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
    private static string ExtractMarkdownSection(string content, string startHeader, string[] endHeaders)
    {
        return ExtractMarkdownSection(content, [startHeader], endHeaders);
    }

    private static string ExtractMarkdownSection(string content, string[] startHeaders, string[] endHeaders)
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

        return content[startIndex..endIndex].Trim();
    }

    private static void PopulateRuleSections(RuleItem ruleItem, string contentAfterBlock, bool loadLayer2, bool loadLayer3)
    {
        // Layer 2: Acceptance Criteria only.
        if (loadLayer2)
        {
            ruleItem.AcceptanceCriteria = ExtractMarkdownSection(
                contentAfterBlock,
                ["### Acceptance Criteria", "### 📋 Acceptance Criteria"],
                ["### Gherkin", "### Gherkin Test Cases", "## ", "```yaml"]
            );
        }

        // Layer 3: Full content (only if requested)
        if (loadLayer3)
        {
            ruleItem.Details = new RuleDetails
            {
                GherkinTestCases = ExtractMarkdownSection(
                    contentAfterBlock,
                    ["### Gherkin Test Cases", "### Gherkin Test Cases"],
                    ["```yaml", "## "]
                ),
                Examples = ExtractMarkdownSection(
                    contentAfterBlock,
                    ["### Examples", "### Examples"],
                    ["```yaml", "## "]
                ),
                Exceptions = ExtractMarkdownSection(
                    contentAfterBlock,
                    ["### Exceptions", "### Exceptions"],
                    ["```yaml", "## "]
                ),
                ImplementationNotes = ExtractMarkdownSection(
                    contentAfterBlock,
                    ["### Implementation Notes", "### Implementation Notes"],
                    ["```yaml", "## "]
                ),
            };
        }
    }

    /// <summary>
    /// Helper to safely extract string values from YAML dictionary.
    /// Internal DTO for collection frontmatter deserialization.
    /// </summary>
    private sealed class CollectionFrontmatterDto
    {
        public string? Id { get; set; }
        public string? Title { get; set; }
        public string? Type { get; set; }
        public string? Source { get; set; }
        public string? Domain { get; set; }
        public string? Created { get; set; }
        public string? LastReviewed { get; set; }
        public int Version { get; set; }
        public string? Description { get; set; }
        public string? Priority { get; set; }
        public AuthorDto? Author { get; set; }
        public List<string>? Tags { get; set; }
        public List<string>? AppliesTo { get; set; }
    }

    /// <summary>
    /// Internal DTO for author information.
    /// </summary>
    private sealed class AuthorDto
    {
        public string? Name { get; set; }
    }

    /// <summary>
    /// Internal DTO for rule metadata deserialization.
    /// </summary>
    private sealed class RuleMetadataDto
    {
        public string? Id { get; set; }
        public string? Title { get; set; }
        public string? Category { get; set; }
        public string? CanonicalSlug { get; set; }
        public string? Description { get; set; }
        public List<string>? Tags { get; set; }
    }
}
