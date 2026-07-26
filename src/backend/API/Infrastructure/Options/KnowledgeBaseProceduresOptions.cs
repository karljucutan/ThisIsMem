namespace API.Infrastructure.Options;

public sealed class KnowledgeBaseProceduresOptions
{
    public const string SectionName = KnowledgeBaseOptions.SectionName + ":Procedures";

    public string DocumentKey { get; set; } = "peripheral-noradrenaline";

    public string PeripheralNoradrenaline { get; set; } = string.Empty;

    public string CollectionName { get; set; } = "peripheral-noradrenaline";
}