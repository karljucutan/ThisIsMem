using API.Features.Rules.Commands;
using API.Features.Rules.MafTools;
using API.Infrastructure.Options;
using Scalar.AspNetCore;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
// Learn more about configuring OpenAPI at https://aka.ms/aspnet/openapi
builder.Services.AddOpenApi();

builder.Services.Configure<KnowledgeBaseOptions>(builder.Configuration.GetSection(KnowledgeBaseOptions.SectionName));

// Register rule search services (CQRS pattern)
builder.Services.AddScoped<SearchRulesCommandHandler>();

var app = builder.Build();

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
    app.MapScalarApiReference();
}

app.UseHttpsRedirection();

// Map feature endpoints
app.MapSearchRulesEndpoint();
app.MapSearchRulesAgent();

app.Run();
