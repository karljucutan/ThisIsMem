using API.Domain;
using API.Infrastructure.Options;
using API.Infrastructure.Parsers;
using Microsoft.Extensions.Options;
using System.ComponentModel;

namespace API.Features.Rules.Commands;

/// <summary>
/// Command for expanding a known rule by exact rule id.
/// </summary>
public record ExpandRuleCommand(
    [Description("Exact rule identifier to expand, such as 'rule-103'.")]
    string RuleId,

    [Description("Disclosure level for expansion. Standard returns Layers 1 and 2; Complete returns Layers 1, 2, and 3.")]
    DisclosureLevel DisclosureLevel = DisclosureLevel.Standard
);

/// <summary>
/// Expands a known rule by exact rule id using the same markdown parser as search.
/// </summary>
public sealed class ExpandRuleCommandHandler
{
    private readonly AgentFrameworkToolParser _parser;
    private readonly string _knowledgeBasePath;

    public ExpandRuleCommandHandler(IOptions<KnowledgeBaseOptions> options)
    {
        _parser = new AgentFrameworkToolParser();
        _knowledgeBasePath = options.Value.BusinessRulesPath;
    }

    public List<SearchRulesResult> Handle(ExpandRuleCommand command)
    {
        var results = new List<SearchRulesResult>();

        if (string.IsNullOrWhiteSpace(command.RuleId))
            return results;

        var knowledgeBaseDir = new DirectoryInfo(_knowledgeBasePath);
        if (!knowledgeBaseDir.Exists)
            return results;

        var targetRuleId = Normalize(command.RuleId);
        var markdownFiles = knowledgeBaseDir.GetFiles("*.md", SearchOption.AllDirectories);

        foreach (var file in markdownFiles)
        {
            try
            {
                var collection = command.DisclosureLevel switch
                {
                    DisclosureLevel.Minimal => _parser.ParseRuleCollectionMinimal(file.FullName),
                    DisclosureLevel.Standard => _parser.ParseRuleCollectionStandard(file.FullName),
                    DisclosureLevel.Complete => _parser.ParseRuleCollectionComplete(file.FullName),
                    _ => _parser.ParseRuleCollectionStandard(file.FullName),
                };

                var matchedRule = collection.Rules.FirstOrDefault(rule => IsMatch(rule, targetRuleId));
                if (matchedRule is not null)
                {
                    results.Add(BuildResult(collection, matchedRule, file.FullName, command.DisclosureLevel));
                    return results;
                }

                if (string.Equals(Normalize(collection.Id), targetRuleId, StringComparison.OrdinalIgnoreCase))
                {
                    var fallbackRule = collection.Rules.FirstOrDefault() ?? new RuleItem
                    {
                        Id = collection.Id,
                        Title = collection.Title,
                        Description = collection.Description,
                        Tags = [.. collection.Tags],
                        Source = new RuleSource
                        {
                            FilePath = collection.FilePath,
                            HeadingPath = collection.Title,
                            LineNumber = 1
                        }
                    };

                    results.Add(BuildResult(collection, fallbackRule, file.FullName, command.DisclosureLevel));
                    return results;
                }
            }
            catch
            {
                continue;
            }
        }

        return results;
    }

    private static bool IsMatch(RuleItem rule, string normalizedRuleId)
    {
        return string.Equals(Normalize(rule.Id), normalizedRuleId, StringComparison.OrdinalIgnoreCase)
            || string.Equals(Normalize(rule.CanonicalSlug), normalizedRuleId, StringComparison.OrdinalIgnoreCase);
    }

    private static string Normalize(string value)
    {
        return value.Trim().ToLowerInvariant().Replace("_", "-").Replace(" ", "-");
    }

    private static SearchRulesResult BuildResult(RuleCollectionDocument collection, RuleItem rule, string sourceFilePath, DisclosureLevel disclosureLevel)
    {
        var supportingMatches = new List<MatchedFragment>();

        if (!string.IsNullOrWhiteSpace(rule.Description))
        {
            supportingMatches.Add(new MatchedFragment
            {
                Quote = rule.Description,
                RelevanceScore = 1.0,
                SourcePath = sourceFilePath,
                Heading = rule.Title,
                Section = "Description"
            });
        }

        if (!string.IsNullOrWhiteSpace(rule.AcceptanceCriteria) && disclosureLevel is DisclosureLevel.Standard or DisclosureLevel.Complete)
        {
            supportingMatches.Add(new MatchedFragment
            {
                Quote = rule.AcceptanceCriteria,
                RelevanceScore = 1.0,
                SourcePath = sourceFilePath,
                Heading = rule.Title,
                Section = "AcceptanceCriteria"
            });
        }

        if (disclosureLevel == DisclosureLevel.Complete && rule.Details is not null)
        {
            AddDetailFragmentIfPresent(supportingMatches, rule, sourceFilePath, "GherkinTestCases", rule.Details.GherkinTestCases);
            AddDetailFragmentIfPresent(supportingMatches, rule, sourceFilePath, "Examples", rule.Details.Examples);
            AddDetailFragmentIfPresent(supportingMatches, rule, sourceFilePath, "Exceptions", rule.Details.Exceptions);
            AddDetailFragmentIfPresent(supportingMatches, rule, sourceFilePath, "ImplementationNotes", rule.Details.ImplementationNotes);
        }

        return new SearchRulesResult
        {
            AnswerSummary = rule.Description,
            Confidence = ConfidenceLevel.High,
            Rationale = "Exact rule id match.",
            TopSources =
            [
                new RuleReference
                {
                    RuleId = rule.Id,
                    Title = rule.Title,
                    Domain = collection.Domain,
                    FilePath = collection.FilePath
                }
            ],
            SupportingMatches = supportingMatches,
            RuleMetadata =
            [
                new RuleMetadata
                {
                    Id = rule.Id,
                    Title = rule.Title,
                    Domain = collection.Domain,
                    Tags = rule.Tags,
                    LastReviewed = collection.LastReviewed,
                    Version = collection.Version
                }
            ],
            FullSourceMarkdown = disclosureLevel == DisclosureLevel.Complete && File.Exists(sourceFilePath)
                ? File.ReadAllText(sourceFilePath)
                : null
        };
    }

    private static void AddDetailFragmentIfPresent(List<MatchedFragment> supportingMatches, RuleItem rule, string sourceFilePath, string section, string content)
    {
        if (string.IsNullOrWhiteSpace(content))
            return;

        supportingMatches.Add(new MatchedFragment
        {
            Quote = content,
            RelevanceScore = 1.0,
            SourcePath = sourceFilePath,
            Heading = rule.Title,
            Section = section
        });
    }
}
