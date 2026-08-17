using ProcurementConcierge.Web.Components;
using ProcurementConcierge.Web.Services;

var builder = WebApplication.CreateBuilder(args);

// Blazor Server (Razor Components) UI
builder.Services.AddRazorComponents()
    .AddInteractiveServerComponents();

// Typed HttpClient pointing at the separately hosted Procurement Concierge API project.
var apiBaseAddress = builder.Configuration["ProcurementConciergeApi:BaseAddress"]
    ?? "https://localhost:7197";

builder.Services.AddHttpClient<IProcurementConciergeApiClient, ProcurementConciergeApiClient>(client =>
{
    client.BaseAddress = new Uri(apiBaseAddress);

    // A single procurement request can chain several sequential local-LLM calls
    // (request analysis, planning, reflection, coaching, recommendation, executive
    // insights), each of which can take 20-30+ seconds against a local Ollama model.
    // The default HttpClient timeout (100s) is easily exceeded, causing the request to
    // be silently cancelled and nothing to render. Match/exceed the API's own LLM call
    // timeout (120s, see ProcurementConcierge.Api/Program.cs) with headroom for multiple
    // sequential calls in a single request.
    client.Timeout = TimeSpan.FromMinutes(5);
});

var app = builder.Build();

// Configure the HTTP request pipeline.
if (!app.Environment.IsDevelopment())
{
    app.UseHsts();
}

app.UseHttpsRedirection();

app.UseAntiforgery();

app.UseStaticFiles();

app.MapRazorComponents<App>()
    .AddInteractiveServerRenderMode();

app.Run();
