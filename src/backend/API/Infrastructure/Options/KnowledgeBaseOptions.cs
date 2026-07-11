namespace API.Infrastructure.Options;

public sealed class KnowledgeBaseOptions
{
    public const string SectionName = "KnowledgeBase";

    public string BusinessRulesPath { get; set; } = string.Empty;
    public string ProceduresPath { get; set; } = string.Empty;
}
