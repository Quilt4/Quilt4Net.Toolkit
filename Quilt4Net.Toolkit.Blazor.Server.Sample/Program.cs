using Quilt4Net.Toolkit;
using Quilt4Net.Toolkit.Blazor;
using Quilt4Net.Toolkit.Blazor.Server.Sample.Components;
using Radzen;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddRazorComponents()
    .AddInteractiveServerComponents();

builder.Services.AddRadzenComponents();

builder.AddQuilt4NetBlazorContent(o =>
{
    o.ApiKey = builder.Configuration["Quilt4Net:ApiKey"];
});

builder.AddQuilt4NetApplicationInsightsClient();
builder.AddQuilt4NetRemoteConfiguration();

var app = builder.Build();

if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Error", createScopeForErrors: true);
    app.UseHsts();
}

app.UseHttpsRedirection();
app.UseAntiforgery();

app.MapStaticAssets();
app.MapRazorComponents<App>()
    .AddInteractiveServerRenderMode();

app.Run();
