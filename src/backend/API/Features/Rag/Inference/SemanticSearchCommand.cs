using System.ComponentModel;

namespace API.Features.Rag.Inference;

public sealed record SemanticSearchCommand(
    [Description("The semantic search query text.")]
    string Query,

    [Description("Maximum number of chunks to return. Defaults to 5.")]
    int TopResults = 5);