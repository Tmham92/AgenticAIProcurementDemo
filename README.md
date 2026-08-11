# Procurement Adoption Concierge

An agentic proof-of-concept that guides users toward the correct procurement process,
surfaces compliance risk, and reduces dependency on Procurement Operations teams.

> **This tool does not create purchase orders.** It analyzes a natural-language
> procurement request and returns guidance: applicable policy, country-specific rules,
> preferred suppliers, required approvals, compliance risk, and a recommended next action.

## Architecture

The solution is split into three projects:

| Project | Purpose |
|---|---|
| `ProcurementConcierge.Contracts` | Shared DTOs and execution-trace models used by both API and Web. |
| `ProcurementConcierge.Api` | ASP.NET Core Web API: agent orchestration, business rules, persistence (SQLite via EF Core), and the AI reasoning layer. |
| `ProcurementConcierge.Web` | Blazor Server frontend that calls the API and renders the full agent reasoning path. |

### Agent Orchestrator

Requests are handled by an **Agent Orchestrator** (`AgentOrchestrator`) that dynamically
plans and runs a sequence of specialized agents (Request Analysis, Policy, Country
Guidance, Compliance, ProcOps Dependency, Recommendation, Governance, Process Discovery,
etc.), then reflects on the outcome and decides whether human intervention is required.
Every step is recorded in an auditable execution trace (`AgentExecutionContext`) that is
displayed in full in the Web UI.

### AI Reasoning Layer vs. Business Rules Layer

The architecture deliberately separates:

- **AI Reasoning Layer** — natural language understanding and narrative generation
  (request analysis/extraction, procurement coaching, agent planning, reflection,
  executive insight summarization). All AI-powered services depend only on the
  `ILLMService` abstraction — never on a specific provider or SDK.
- **Business Rules Layer** — compliance scoring, policy evaluation, approval routing, and
  governance calculations remain fully deterministic and implemented in code, independent
  of any LLM.

This means the underlying model/provider can be swapped via Dependency Injection alone,
with **no changes required to any agent or business service**.

## Local LLM (Ollama)

By default, the AI Reasoning Layer runs **fully locally** via [Ollama](https://ollama.com),
with no Azure or OpenAI dependency.

- `ILLMService` — provider-agnostic interface (`GenerateStructuredResponseAsync<T>`,
  `GenerateTextResponseAsync`) that all agents/services depend on.
- `OllamaLLMService` — the default implementation, calling a local Ollama instance over
  HTTP (`/api/generate`).
- `AzureOpenAIService` — a placeholder implementation for a future Azure OpenAI migration.
  Switching providers only requires changing the DI registration in `Program.cs`.

### Multi-model support

Different reasoning roles can use different models via `IModelSelectionService` /
`ModelType` (`Default`, `Planner`, `Coach`, `Insights`), configured independently in
`appsettings.json`.

### Setup

1. Install [Ollama](https://ollama.com/download) and ensure it is running (default:
   `http://localhost:11434`).
2. Pull a local model, e.g.:
   ```powershell
   ollama pull qwen3:8b
   ```
3. Configure `ProcurementConcierge.Api/appsettings.json`:
   ```json
   "Ollama": {
	 "BaseUrl": "http://localhost:11434",
	 "Model": "qwen3:8b",
	 "DefaultModel": "qwen3:8b",
	 "PlannerModel": "qwen3:8b",
	 "InsightModel": "qwen3:8b",
	 "CoachingModel": "qwen3:8b"
   }
   ```
   > Model tags ending in `:cloud` refer to Ollama's cloud-hosted models, which require
   > authentication (`ollama signin`, or an `Ollama:ApiKey` / `OLLAMA_API_KEY`
   > environment variable) and are **not** fully local.
4. Verify connectivity with the health check endpoint (see below) before running the app.

### Health check

```
GET /api/system/ollama-status
```
Returns whether Ollama is reachable, whether the configured model is loaded, and the
round-trip latency:
```json
{ "connected": true, "model": "qwen3:8b", "latencyMs": 120, "error": null }
```

## Running the solution

1. Open `ProcurementConcierge.slnx` in Visual Studio (or run via CLI below).
2. Start Ollama and confirm a model is pulled (see above).
3. Run both `ProcurementConcierge.Api` and `ProcurementConcierge.Web` (multiple startup
   projects), or via CLI:
   ```powershell
   dotnet run --project ProcurementConcierge.Api
   dotnet run --project ProcurementConcierge.Web
   ```
4. Open the Web app and submit a procurement request, e.g.:
   > "I need a marketing agency in France for a product launch, estimated budget €80,000."

The response includes procurement guidance, compliance risk, required approvals, request
quality coaching, and the full agent reasoning path.

## Data

Interaction history is persisted locally to a SQLite database
(`ProcurementConcierge.Api/interactions.db`) via EF Core, powering adoption, compliance,
process discovery, and executive insight dashboards. The database file is not committed to
source control (see `.gitignore`).

## Key Concepts

- **Request Quality Coaching** — evaluates and improves incomplete requests before policy
  evaluation.
- **Policy & Compliance Evaluation** — deterministic checks for preferred supplier
  availability, required approvals, and policy deviations.
- **ProcOps Dependency Assessment** — determines whether Procurement Operations
  intervention is required.
- **Country Governance** — country-specific procurement guidance and risk.
- **Executive Insights / Control Tower** — adoption, compliance, and process discovery
  analytics, summarized into a CPO-style briefing by the AI reasoning layer.
- **Coupa Simulation** — simulates downstream procure-to-pay system behavior.

## Disclaimer

This is a proof-of-concept for exploring agentic procurement guidance patterns. It is not
intended to create purchase orders or replace Procurement Operations review for
production use.
