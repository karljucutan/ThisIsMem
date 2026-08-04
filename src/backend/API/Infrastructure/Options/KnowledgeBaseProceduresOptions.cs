namespace API.Infrastructure.Options;

public sealed class KnowledgeBaseProceduresOptions
{
    public const string SectionName = KnowledgeBaseOptions.SectionName + ":Procedures";

    public List<KnowledgeBaseProcedureDocumentOptions> Documents { get; set; } = [];

    public KnowledgeBaseProcedureDocumentOptions? GetDocument(string documentKey)
        => Documents.SingleOrDefault(x => string.Equals(x.DocumentKey, documentKey, StringComparison.OrdinalIgnoreCase));
}

public sealed class KnowledgeBaseProcedureDocumentOptions
{
    public string DocumentKey { get; set; } = string.Empty;

    public string DocumentPath { get; set; } = string.Empty;

    public string CollectionName { get; set; } = string.Empty;
}