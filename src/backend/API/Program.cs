using API.Features.Rules.Commands;
using API.Features.Rules.MafTools;
using API.Infrastructure.Options;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
// Learn more about configuring OpenAPI at https://aka.ms/aspnet/openapi
builder.Services.AddOpenApi();

builder.Services.Configure<KnowledgeBaseOptions>(builder.Configuration.GetSection(KnowledgeBaseOptions.SectionName));

// Register rule search services (CQRS pattern)
builder.Services.AddScoped<SearchRulesCommandHandler>();             // Core search handler: query execution

// TODO: Register MAF Agent Framework when available
// Uncomment when Microsoft.AgentFramework NuGet is added
//
// builder.Services.AddMafAgent()
//     .RegisterTool<SearchRulesToolHandler>(
//         name: SearchRulesToolDefinition.ToolName,
//         description: SearchRulesToolDefinition.ToolDescription
//     );
//
// See MAF_INTEGRATION.md for detailed setup instructions

var app = builder.Build();

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
}

app.UseHttpsRedirection();

// Map feature endpoints
app.MapSearchRulesEndpoint();
app.MapSearchRulesAgent();

app.Run();
