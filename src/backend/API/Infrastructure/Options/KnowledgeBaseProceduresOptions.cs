namespace API.Infrastructure.Options;

public sealed class KnowledgeBaseProceduresOptions
{
    public const string SectionName = KnowledgeBaseOptions.SectionName + ":Procedures";

    public string DocumentKey { get; set; } = "incident-response-reference-guide";

    public string IncidentResponsePdfPath { get; set; } = string.Empty;

    public string CollectionName { get; set; } = "incident-response-reference-guide";
}