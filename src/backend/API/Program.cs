using API.Features.Rules.Commands;
using API.Features.Rules.MafAgents;
using API.Features.Rag.Ingestion;
using API.Features.Rag.Inference;
using API.Features.Rag.Inference.MafAgents;
using API.Features.Rag.Shared;
using API.Infrastructure.Extensions;
using API.Infrastructure.Persistence;
using API.Infrastructure.Options;
using Scalar.AspNetCore;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
// Learn more about configuring OpenAPI at https://aka.ms/aspnet/openapi
builder.Services.AddOpenApi();

builder.Services.Configure<KnowledgeBaseOptions>(builder.Configuration.GetSection(KnowledgeBaseOptions.SectionName));
builder.Services.Configure<KnowledgeBaseProceduresOptions>(builder.Configuration.GetSection(KnowledgeBaseProceduresOptions.SectionName));
builder.Services.Configure<RagOptions>(builder.Configuration.GetSection(RagOptions.SectionName));

builder.Services.AddAppCors(builder.Configuration);
builder.Services.AddRagDbContext(builder.Configuration);

builder.Services.AddSingleton<RagIngestionQueue>();
builder.Services.AddSingleton<RagEmbeddingService>();
builder.Services.AddSingleton<RagPdfReader>();
builder.Services.AddScoped<RagIngestionService>();
builder.Services.AddScoped<RagSemanticSearchService>();
builder.Services.AddSingleton<RagSemanticSearchAgentTool>();
builder.Services.AddHostedService<RagIngestionBackgroundService>();

// Register services
builder.Services.AddScoped<SearchRulesCommandHandler>();
builder.Services.AddScoped<ExpandRuleCommandHandler>();

// Register the AI agents
builder.AddRuleAssistantAIAgent();
builder.AddRagAssistantAIAgent();

builder.Services.AddAGUI();

var app = builder.Build();

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    await app.Services.EnsureMemDbCreatedAsync();
    app.MapOpenApi();
    app.MapScalarApiReference();
}

app.UseAppCors();
app.UseHttpsRedirection();

// Map feature endpoints
app.MapSearchRulesEndpoint();
app.MapRulesAssistantAIAgent();
app.MapRagIngestionEndpoint();
app.MapRagSemanticSearchEndpoint();
app.MapRagAssistantAIAgent();

app.Run();
