using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using BrulAssistant.Core.Dto;

namespace BrulAssistant.Core.Parsers;

public class BusinessRulesParser
{
    public RuleCollectionDto ParseGroupedRuleFile(string filePath)
    {
        var collection = new RuleCollectionDto();

        if (!File.Exists(filePath))
            throw new FileNotFoundException(\$"Target rules ledger not found at: {filePath}");

        string fileContent = File.ReadAllText(filePath);

        // 1. Isolate and parse the top-level collection YAML frontmatter
        int firstDash = fileContent.IndexOf("---");
        int secondDash = fileContent.IndexOf("---", firstDash + 3);

        if (firstDash != -1 && secondDash != -1)
        {
            string frontmatter = fileContent.Substring(firstDash + 3, secondDash - (firstDash + 3));
            ParseCollectionFrontmatter(frontmatter, collection);
            
            // Remove the collection frontmatter from string processing to avoid cross-contamination
            fileContent = fileContent.Substring(secondDash + 3);
        }

        // 2. Squeeze out individual rules using the rule YAML block dividers
        string[] ruleChunks = fileContent.Split(new[] { "```yaml" }, StringSplitOptions.RemoveEmptyEntries);

        foreach (var chunk in ruleChunks)
        {
            // Skip text blocks that don't contain rule item closures
            if (!chunk.Contains("```")) continue;

            var ruleItem = new RuleItemDto();
            
            // Isolate the rule-level embedded metadata block
            int endBlock = chunk.IndexOf("```");
            string ruleYamlMeta = chunk.Substring(0, endBlock);
            ParseRuleMetadata(ruleYamlMeta, ruleItem);

            // Isolate the remaining content payload for section stripping
            string payload = chunk.Substring(endBlock + 3);

            // Extract Sections dynamically by structural headers
            ruleItem.PolicySummary = ExtractSectionContent(payload, "### Policy Summary", ["### Acceptance Criteria", "### 🧪 Gherkin"]);
            ruleItem.Details.AcceptanceCriteria = ExtractSectionContent(payload, "### Acceptance Criteria", ["### 🧪 Gherkin", "---"]);
            ruleItem.Details.GherkinTestCases = ExtractSectionContent(payload, "### 🧪 Gherkin Test Cases", ["---"]);

            collection.Rules.Add(ruleItem);
        }

        return collection;
    }

    private void ParseCollectionFrontmatter(string frontmatter, RuleCollectionDto collection)
    {
        var lines = frontmatter.Split('\n');
        for (int i = 0; i < lines.Length; i++)
        {
            var line = lines[i].Trim();
            if (string.IsNullOrEmpty(line)) continue;

            var parts = line.Split(':', 2);
            if (parts.Length < 2) continue;

            string key = parts[0].Trim().ToLower();
            string val = parts[1].Trim().Trim('"');

            switch (key)
            {
                case "id": collection.Id = val; break;
                case "title": collection.Title = val; break;
                case "type": collection.Type = val; break;
                case "source": collection.Source = val; break;
                case "domain": collection.Domain = val; break;
                case "created": collection.Created = val; break;
                case "lastreviewed": collection.LastReviewed = val; break;
                case "version": int.TryParse(val, out int v); collection.Version = v; break;
                case "author":
                    if (i + 1 < lines.Length && lines[i + 1].Contains("name:"))
                    {
                        collection.AuthorName = lines[i + 1].Split(':', 2)[1].Trim().Trim('"');
                    }
                    break;
            }
        }
    }

    private void ParseRuleMetadata(string ruleMeta, RuleItemDto ruleItem)
    {
        var lines = ruleMeta.Split('\n');
        foreach (var line in lines)
        {
            var parts = line.Split(':', 2);
            if (parts.Length < 2) continue;

            string key = parts[0].Trim().ToLower();
            string val = parts[1].Trim().Trim('"');

            switch (key)
            {
                case "id": ruleItem.Id = val; break;
                case "title": ruleItem.Title = val; break;
                case "category": ruleItem.Category = val; break;
                case "canonicalslug": ruleItem.CanonicalSlug = val; break;
                case "tags":
                    ruleItem.Tags = val.Trim('[', ']').Split(',').Select(t => t.Trim()).ToList();
                    break;
            }
        }
    }

    private string ExtractSectionContent(string payload, string currentHeader, string[] nextHeaders)
    {
        int startIndex = payload.IndexOf(currentHeader);
        if (startIndex == -1) return string.Empty;

        startIndex += currentHeader.Length;
        int endIndex = payload.Length;

        foreach (var nextHeader in nextHeaders)
        {
            int possibleEnd = payload.IndexOf(nextHeader, startIndex);
            if (possibleEnd != -1 && possibleEnd < endIndex)
            {
                endIndex = possibleEnd;
            }
        }

        return payload.Substring(startIndex, endIndex - startIndex).Trim();
    }
}
