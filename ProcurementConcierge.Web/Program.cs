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
