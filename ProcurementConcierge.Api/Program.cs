using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using ProcurementConcierge.Api.Agents;
using ProcurementConcierge.Api.Configuration;
using ProcurementConcierge.Api.Data;
using ProcurementConcierge.Api.Services;
using ProcurementConcierge.Api.Services.Interfaces;

var builder = WebApplication.CreateBuilder(args);

const string WebClientCorsPolicy = "WebClient";

// Controllers + OpenAPI/Swagger
builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();

// Allow the separate Blazor Web frontend project to call this API during local development.
builder.Services.AddCors(options =>
{
    options.AddPolicy(WebClientCorsPolicy, policy =>
    {
        var allowedOrigins = builder.Configuration.GetSection("Cors:AllowedOrigins").Get<string[]>()
            ?? new[] { "https://localhost:7100", "http://localhost:5100" };

        policy.WithOrigins(allowedOrigins)
            .AllowAnyHeader()
            .AllowAnyMethod();
    });
});

builder.Services.AddSwaggerGen(options =>
{
    options.SwaggerDoc("v1", new Microsoft.OpenApi.OpenApiInfo
    {
        Title = "Procurement Adoption Concierge API",
        Version = "v1",
        Description = "Agentic AI proof of concept that guides users towards the correct procurement process and compliance requirements. This tool does not create purchase orders - it provides guidance, compliance risk, and required approvals."
    });
});

// Azure OpenAI configuration
builder.Services.Configure<AzureOpenAIOptions>(
    builder.Configuration.GetSection(AzureOpenAIOptions.SectionName));

// Ollama configuration - the default local AI Reasoning Layer provider (no Azure/OpenAI
// dependency required). Swapping to Azure OpenAI in the future only requires registering
// AzureOpenAIService as ILLMService instead of OllamaLLMService below.
builder.Services.Configure<OllamaSettings>(
    builder.Configuration.GetSection(OllamaSettings.SectionName));

// Resolves which Ollama model to use per reasoning role (Planner/Coach/Insights/Default),
// preparing the architecture for future per-role Azure OpenAI deployment switching.
builder.Services.AddSingleton<IModelSelectionService, ModelSelectionService>();

builder.Services.AddHttpClient<ILLMService, OllamaLLMService>((sp, client) =>
{
    var ollamaSettings = sp.GetRequiredService<IOptions<OllamaSettings>>().Value;
    client.BaseAddress = new Uri(ollamaSettings.BaseUrl);
    client.Timeout = TimeSpan.FromSeconds(120);
});

builder.Services.AddHttpClient<IModelHealthCheckService, OllamaModelHealthCheckService>((sp, client) =>
{
    var ollamaSettings = sp.GetRequiredService<IOptions<OllamaSettings>>().Value;
    client.BaseAddress = new Uri(ollamaSettings.BaseUrl);
    client.Timeout = TimeSpan.FromSeconds(10);
});

// Local SQLite database for interaction insight logging
var interactionsConnectionString = builder.Configuration.GetConnectionString("InteractionsDatabase")
    ?? "Data Source=interactions.db";
builder.Services.AddDbContext<InteractionDbContext>(options =>
    options.UseSqlite(interactionsConnectionString));

// Semantic Kernel and Azure OpenAI direct usage removed: the AI Reasoning Layer now goes
// exclusively through ILLMService (OllamaLLMService by default), keeping business services
// fully decoupled from any specific LLM provider or SDK.

// Application services
builder.Services.AddSingleton<IPolicyService, PolicyService>();
builder.Services.AddSingleton<ICountryRuleService, CountryRuleService>();
builder.Services.AddSingleton<IRequestHistoryStore, InMemoryRequestHistoryStore>();
builder.Services.AddScoped<IRequestAnalysisService, RequestAnalysisService>();
builder.Services.AddScoped<IComplianceService, ComplianceService>();
builder.Services.AddScoped<IRecommendationService, RecommendationService>();
builder.Services.AddScoped<IRequestQualityService, RequestQualityService>();
builder.Services.AddScoped<IPolicyDeviationService, PolicyDeviationService>();
builder.Services.AddScoped<IComplianceScoringService, ComplianceScoringService>();
builder.Services.AddScoped<IAdoptionInsightService, AdoptionInsightService>();
builder.Services.AddScoped<IComplianceInsightService, ComplianceInsightService>();
builder.Services.AddScoped<IProcessDiscoveryService, ProcessDiscoveryService>();
builder.Services.AddScoped<IExecutiveInsightService, ExecutiveInsightService>();
builder.Services.AddScoped<IProcurementConciergeService, ProcurementConciergeService>();
builder.Services.AddScoped<IInteractionLoggingService, InteractionLoggingService>();
builder.Services.AddScoped<IProcessDiscoveryInsightService, ProcessDiscoveryInsightService>();
builder.Services.AddScoped<IProcurementHealthService, ProcurementHealthService>();
builder.Services.AddScoped<IProcOpsDependencyService, ProcOpsDependencyService>();
builder.Services.AddScoped<IProcurementCoachingService, ProcurementCoachingService>();
builder.Services.AddScoped<ICountryGovernanceService, CountryGovernanceService>();
builder.Services.AddScoped<IControlTowerService, ControlTowerService>();
builder.Services.AddScoped<IControlTowerAgentService, ControlTowerAgentService>();
builder.Services.AddSingleton<ICoupaSimulationService, CoupaSimulationService>();

// Agentic AI architecture: planner, orchestrator, and plannable agents.
builder.Services.AddScoped<IAgentPlanningService, AgentPlanningService>();
builder.Services.AddScoped<IAgentOrchestrator, AgentOrchestrator>();
builder.Services.AddScoped<IReflectionAgent, ReflectionAgent>();
builder.Services.AddScoped<IEscalationAgent, EscalationAgent>();
builder.Services.AddScoped<IAgent, RequestAnalysisAgent>();
builder.Services.AddScoped<IAgent, PolicyAgent>();
builder.Services.AddScoped<IAgent, CountryGuidanceAgent>();
builder.Services.AddScoped<IAgent, ComplianceAgent>();
builder.Services.AddScoped<IAgent, RecommendationAgent>();
builder.Services.AddScoped<IAgent, ProcOpsDependencyAgent>();
builder.Services.AddScoped<IAgent, ProcessDiscoveryAgent>();
builder.Services.AddScoped<IAgent, GovernanceAgent>();

// Future extension point: register additional IProcurementAgent implementations here, e.g.:
// builder.Services.AddScoped<IProcurementAgent, ComplianceAgent>();
// builder.Services.AddScoped<IProcurementAgent, SpendIntelligenceAgent>();
// builder.Services.AddScoped<IProcurementAgent, SupplierOnboardingAgent>();
// builder.Services.AddScoped<IProcurementAgent, ProcurementPerformanceAgent>();

var app = builder.Build();

// Ensure the local SQLite interaction log database and schema exist.
using (var scope = app.Services.CreateScope())
{
    var dbContext = scope.ServiceProvider.GetRequiredService<InteractionDbContext>();
    dbContext.Database.EnsureCreated();
}

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI(options =>
    {
        options.SwaggerEndpoint("/swagger/v1/swagger.json", "Procurement Concierge API v1");
    });
}

app.UseHttpsRedirection();

app.UseCors(WebClientCorsPolicy);

app.UseAuthorization();

app.MapControllers();

app.Run();
