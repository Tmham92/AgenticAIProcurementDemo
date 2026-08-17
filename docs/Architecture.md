# ProcurementConcierge – Architecture Overview

## Project Structure (3-project solution)

| Project | Role |
|---|---|
| **`ProcurementConcierge.Contracts`** | Shared DTOs/contracts (no dependencies): request/response models, agent plumbing types, domain models used across API and Web. |
| **`ProcurementConcierge.Api`** | ASP.NET Core Web API — all business logic, agents, orchestration, LLM integration, EF Core/SQLite persistence, controllers. |
| **`ProcurementConcierge.Web`** | Blazor Server app (`@rendermode InteractiveServer`) — UI only, talks to the API via a typed `HttpClient` (`IProcurementConciergeApiClient`). No business logic lives here. |

## Services (Api project)

Grouped by responsibility, all registered via DI in `Program.cs` (interfaces in `Services/Interfaces`):

- **Intake/analysis**: `RequestAnalysisService` (LLM-based field extraction + regex fallback), `ProcurementCoachingService` (LLM-based request-quality coaching + deterministic fallback), `RequestQualityService`/`RequestQualityHeuristics` (deterministic scoring).
- **Policy/compliance**: `PolicyService` (loads `policies.json`), `CountryRuleService` (loads `countryRules.json`), `PolicyDeviationService`, `ComplianceService`, `ComplianceScoringService`.
- **Guidance**: `AdoptionGuidanceService` — the "Procurement Adoption Coach" that deterministically classifies a `GuidanceLevel` and asks the LLM (with fallback) for ultra-short status/next-action/approval/tip text.
- **ProcOps/Coupa**: `ProcOpsDependencyService`, `CoupaSimulationService` (simulates preferred suppliers, draft requisition, approval route — no real Coupa integration).
- **Analytics/insights**: `AdoptionInsightService`, `ComplianceInsightService`, `ExecutiveInsightService`, `ProcessDiscoveryService`/`ProcessDiscoveryInsightService`, `CountryGovernanceService`, `ProcurementHealthService`, `ControlTowerService`/`ControlTowerAgentService`.
- **Persistence**: `InteractionLoggingService` (writes to SQLite via `InteractionDbContext`), `InMemoryRequestHistoryStore` (legacy in-memory history for dashboards).
- **AI infrastructure**: `OllamaLLMService` (`ILLMService` impl), `AzureOpenAIService` (placeholder, unused, for future swap), `ModelSelectionService` (`IModelSelectionService`), `OllamaModelHealthCheckService`.
- **Orchestration**: `AgentPlanningService` (`IAgentPlanningService`), `AgentOrchestrator` (`IAgentOrchestrator`).
- **Top-level facade**: `ProcurementConciergeService` (`IProcurementConciergeService`) — the single entry point called by the API controller; composes orchestrator output + coaching + Coupa simulation + logging + insights into the final `ProcurementResponse`.

## Agents (`Api/Agents`, all implement `IAgent`)

Each agent wraps one or more services and reads/writes `AgentRunContext.WorkingMemory`:

- `RequestAnalysisAgent` → wraps `IRequestAnalysisService`
- `PolicyAgent` → wraps `IPolicyService` + `IPolicyDeviationService`
- `CountryGuidanceAgent` → wraps `ICountryRuleService`
- `ComplianceAgent` → wraps `IComplianceService` + `IComplianceScoringService`
- `ProcOpsDependencyAgent` → wraps `IProcOpsDependencyService`
- `RecommendationAgent` → wraps `IAdoptionGuidanceService` (produces the final `UserGuidanceResponse`)
- `ProcessDiscoveryAgent` → wraps `IProcessDiscoveryService` (historical trend questions)
- `GovernanceAgent` → wraps `ICountryGovernanceService`-style logic for broader risk

Plus two special agents run after the main plan:
- `ReflectionAgent` (`IReflectionAgent`) — decides whether the goal was achieved.
- `EscalationAgent` (`IEscalationAgent`) — decides the `EscalationLevel` (Automatic/Review/etc.) based on context.

All are registered in DI as `IAgent` (resolved into `AgentOrchestrator` via `IEnumerable<IAgent>`, keyed by `Name`).

## Orchestration Flow

```
ConciergeController.Post
  → ProcurementConciergeService.ProcessRequestAsync(request)
	  1. AgentOrchestrator.RunAsync(goal)
		 a. AgentPlanningService.CreatePlanAsync(userRequest)
			  - LLM planner (ModelType.Planner) proposes AgentPlan{Goal, Tasks[]}
			  - Falls back to keyword-based FallbackPlan(...) on failure/empty
			  - EnsureMandatoryAgents(...) guarantees PolicyAgent + ComplianceAgent
				run whenever RequestAnalysisAgent is planned
		 b. Executes agents in plan-priority order, each reading/writing
			AgentRunContext.WorkingMemory (analysis, policy, countryRule,
			deviations, complianceScoreResult, evaluation, procOpsAssessment,
			userGuidance...), recording an ExecutionStep per agent
		 c. ReflectionAgent.ReflectAsync(context) → ReflectionResult
		 d. EscalationAgent.Decide(context) → EscalationDecision
		 e. Returns AgentExecutionResult{Success, Steps, Outputs}
	  2. ProcurementConciergeService then, sequentially (outside the dynamic plan):
		 - Calls ProcurementCoachingService.CoachAsync (pre-policy coaching)
		 - Folds orchestrator's Steps into response.AgentExecutionPath (reasoning trace)
		 - Populates category/country/spend/compliance/guidance fields onto ProcurementResponse
		 - Calls RequestQualityService.Assess (deterministic quality scoring)
		 - Calls CoupaSimulationService (preferred suppliers, draft requisition, approval route)
		 - Records to InMemoryRequestHistoryStore + InteractionLoggingService (SQLite)
		 - Calls ExecutiveInsightService.GetInsightsAsync
		 - Runs any additional IProcurementAgent implementations (extension point)
	  3. Returns the fully composed ProcurementResponse
```

Every step (coaching, each orchestrated agent, logging, insight synthesis, extension agents) is recorded via `RecordStep(...)` into `response.AgentExecutionPath`/`ExecutionStepDetails` — this is the auditable reasoning trace shown in the Web UI's "Agent Trace" tab.

## Data Models (Contracts + Api/Models)

- **Request/response**: `ProcurementRequest` (just `Message`), `ProcurementResponse` (large aggregate: category/country/spend/confidences, compliance fields, `PolicyDeviations`, `ProcOpsAssessment`, `Guidance` (`UserGuidanceResponse`), `ProcurementCoaching`, `RequestQuality`, `CoupaSimulation`, `AgentExecutionPath`/`ExecutionStepDetails`, escalation info, `NeedsHumanReview`).
- **Guidance**: `UserGuidanceResponse` (Status/NextAction/Approval/Tip/AdoptionFriendlinessScore/GuidanceLevel), `GuidanceLevel` enum (Easy/ApprovalRequired/ReviewRequired/HighRisk).
- **Domain**: `ProcurementAnalysis` (Api/Models — extracted fields + confidences + computed `NeedsHumanReview`), `ProcurementPolicy`, `CountryRule`, `ComplianceEvaluation`, `ComplianceScoreResult`, `PolicyDeviationDetail`, `ProcOpsAssessment`.
- **Coupa simulation**: `CoupaSimulationResult`, `Requisition`, `Supplier`, `Approval`.
- **Coaching/quality**: `ProcurementCoachingAssessment`, `RequestQualityAssessment`.
- **Orchestration plumbing**: `AgentGoal`, `AgentPlan`, `PlannedTask`, `AgentExecutionResult`, `ExecutionStep`, `ExecutionStepDetail`, `AgentExecutionContext`, `ReflectionResult`, `EscalationDecision`, `EscalationLevel`.
- **Analytics**: `AdoptionInsights`, `ComplianceInsights`, `ExecutiveInsights`, `ProcessDiscoveryInsight`, `CountryGovernanceReport`, `ControlTowerDashboard`, `ProcurementHealthDashboard`, `ProcOpsMetrics`, `InteractionRecord` (also persisted via EF Core in `Api/Models/InteractionRecord.cs` + `InteractionDbContext`).

## API Endpoints

- `POST /api/concierge` — main entry point, returns `ProcurementResponse`.
- `GET /api/system/ollama-status` — local Ollama health/latency check.
- `GET /api/interactions` — all persisted SQLite interaction records.
- `GET /api/governance/countries` — per-country governance reports.
- `GET /api/process-discovery` — historical process discovery insights.
- `GET /api/dashboard/adoption`, `/compliance`, `/executive-insights`, `/health` — control-tower/dashboard analytics.
- `GET /api/control-tower` and `/api/control-tower/recommendations`.
- `GET /api/procops-metrics`.
- Swagger/OpenAPI UI enabled in Development.

## LLM Integration

- **Abstraction**: `ILLMService` with `GenerateStructuredResponseAsync<T>` (JSON-mode) and `GenerateTextResponseAsync`, parameterized by `ModelType` (Planner/Coach/Insights/Default).
- **Default provider**: `OllamaLLMService`, calling a local Ollama instance's `/api/generate` REST endpoint (fully offline, no Azure/OpenAI dependency), with API-key support for Ollama Cloud models.
- **Model routing**: `IModelSelectionService`/`ModelSelectionService` resolves the concrete model name per `ModelType` from `OllamaSettings` (bound from config section `Ollama`), enabling different models per reasoning role.
- **Future swap point**: `AzureOpenAIService` exists as a placeholder `ILLMService` implementation — switching providers is a pure DI registration change in `Program.cs`, with zero changes to business services.
- **Resilience**: every LLM-calling service (`RequestAnalysisService`, `ProcurementCoachingService`, `AdoptionGuidanceService`) wraps the call in try/catch and falls back to deterministic heuristic logic (regex/keyword-based) if the LLM is unreachable, misconfigured, or returns an unusable/empty response — keeping the PoC demonstrable fully offline.
- **Health**: `IModelHealthCheckService`/`OllamaModelHealthCheckService` pings Ollama for the `/api/system/ollama-status` endpoint.

## Memory Implementation

Two distinct "memory" concepts:

1. **Short-term working memory (per-request, in-process)**: `AgentRunContext.WorkingMemory` — a `Dictionary<string, object>` keyed by `MemoryKeys` constants (`Analysis`, `Policy`, `CountryRule`, `PolicyDeviations`, `ComplianceScoreResult`, `ComplianceEvaluation`, `ProcOpsAssessment`, `Recommendation`, `RecommendedNextAction`, `UserGuidance`, `ProcessDiscoveryFindings`, `GovernanceAssessment`). Agents read prerequisites via `context.GetMemory<T>(key)` and publish results via `context.SetMemory(key, value)`, decoupling agents from each other. It is discarded after each request — there is no cross-request conversational memory (this is a single-turn tool, not a chat agent).
2. **Long-term memory (persisted)**: `InteractionLoggingService` writes each processed request as an `InteractionRecord` to a local SQLite database (`interactions.db`) via EF Core's `InteractionDbContext`. This is what powers all the analytics/insight services (executive insights, governance reports, process discovery, control tower). There's also a legacy `InMemoryRequestHistoryStore` used by some of the older insight services, holding `RequestHistoryEntry` objects in-process (lost on restart).

## Planning Implementation

`AgentPlanningService.CreatePlanAsync(userRequest)` implements the "planning" stage of the agentic loop:

1. Sends the user's raw request text plus a system prompt describing the 8 available agents (with prerequisite notes, e.g. "ComplianceAgent requires PolicyAgent to have already run") to the LLM (`ModelType.Planner`), requesting strict JSON: `{ goal, tasks: [{agentName, reason, priority}] }`.
2. Validates the LLM's response (`ToAgentPlan`) — filters out unknown agent names, defaults missing priorities, sorts by priority.
3. On any failure/empty result, falls back to `FallbackPlan(userRequest)` — pure keyword/regex heuristics (e.g., "reject"/"denied"/"procops" → ProcOps-focused plan; "trend"/"pattern" → historical/governance plan; otherwise the full new-request pipeline).
4. `EnsureMandatoryAgents(plan)` is applied to **both** paths as a safety net: if `RequestAnalysisAgent` is in the plan (i.e. it's a new-request flow), it force-inserts `PolicyAgent`/`ComplianceAgent` if the planner omitted them, then renumbers priorities — this was added specifically to prevent `ComplianceScoreResult` from silently being absent from working memory.

`AgentOrchestrator.RunAsync` then simply iterates `plan.Tasks.OrderBy(Priority)`, executing each corresponding `IAgent` and recording outcomes — it has no further "planning" logic itself, it's a pure executor plus reflection/escalation post-processing.
