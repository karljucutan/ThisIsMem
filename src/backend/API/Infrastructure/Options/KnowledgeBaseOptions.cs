namespace API.Infrastructure.Options;

public sealed class KnowledgeBaseOptions
{
    public const string SectionName = "KnowledgeBase";

    public string Path { get; set; } = string.Empty;
}
