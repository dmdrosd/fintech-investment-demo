using Fintech.Web.Api;
using Fintech.Web.Components;
using MudBlazor.Services;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddRazorComponents()
    .AddInteractiveServerComponents();
builder.Services.AddMudServices();
builder.Services.AddHttpClient<InvestmentApiClient>(client =>
{
    client.BaseAddress = new Uri(builder.Configuration["Api:BaseUrl"] ?? "http://localhost:5252/");
    client.DefaultRequestHeaders.Add("X-User-Id", "dev-operator");
    client.DefaultRequestHeaders.Add("X-User-Name", "Development Operator");
    client.DefaultRequestHeaders.Add("X-Roles", "operator,auditor");
});

var app = builder.Build();

if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Error", createScopeForErrors: true);
}

app.UseStaticFiles();
app.UseAntiforgery();

app.MapRazorComponents<App>()
    .AddInteractiveServerRenderMode();

app.Run();
