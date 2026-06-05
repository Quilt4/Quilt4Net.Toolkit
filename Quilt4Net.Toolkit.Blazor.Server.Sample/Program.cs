using Quilt4Net.Toolkit;
using Quilt4Net.Toolkit.Api;
using Quilt4Net.Toolkit.Blazor;
using Quilt4Net.Toolkit.Blazor.Server.Sample.Components;
using Radzen;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddRazorComponents()
    .AddInteractiveServerComponents();

// Registers Radzen's DialogService / NotificationService / ContextMenuService / TooltipService.
// The visual hosts (RadzenDialog / RadzenNotification / ...) live in MainLayout.razor.
builder.Services.AddRadzenComponents();

builder.AddQuilt4NetBlazorContent(o =>
{
    o.AssumeAdmin = true;
});

builder.AddQuilt4NetApplicationInsightsClient();
builder.AddQuilt4NetRemoteConfiguration();

// Register the per-record telemetry-identity processors (env / app / version / host /
// quilt4net.monitor) and the request middleware that mints / honors X-Correlation-ID.
// AddHttpRequestLogging is the opt-in that enables the middleware via UseQuilt4NetLogging.
builder.AddQuilt4NetLogging()
    .AddHttpRequestLogging();

var app = builder.Build();

if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Error", createScopeForErrors: true);
    app.UseHsts();
}

app.UseHttpsRedirection();
app.UseQuilt4NetLogging();
app.UseAntiforgery();

// Demo endpoint showing CorrelationId propagation across multiple log lines from a single
// request. Hit GET /api/correlation-demo (optionally with header X-Correlation-ID: my-id)
// — every ILogger call inside this request handler emits an AppTrace whose
// customDimensions["CorrelationId"] holds the same value, because CorrelationIdMiddleware
// wraps the pipeline in Logger.BeginScope for the duration of the request.
app.MapGet("/api/correlation-demo", (HttpContext ctx, ILogger<Program> logger) =>
{
    logger.LogInformation("Correlation demo: step 1 — request received");
    DoSomeWork(logger);
    logger.LogInformation("Correlation demo: step 3 — finishing");
    return Results.Ok(new
    {
        CorrelationId = ctx.Items["CorrelationId"]?.ToString(),
        Hint = "Send X-Correlation-ID on the request to chain across services. " +
               "Every log line in this handler shares the value; query AI with " +
               "customDimensions['CorrelationId'] == '<id>' to see the chain."
    });

    static void DoSomeWork(ILogger logger)
    {
        logger.LogInformation("Correlation demo: step 2 — doing work");
    }
});

app.MapStaticAssets();
app.MapRazorComponents<App>()
    .AddInteractiveServerRenderMode();

app.Run();
