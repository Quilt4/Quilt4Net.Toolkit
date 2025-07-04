using Microsoft.ApplicationInsights.AspNetCore.Extensions;
using Quilt4Net.Toolkit;
using Quilt4Net.Toolkit.Api;
using Quilt4Net.Toolkit.Api.Framework.Endpoints;
using Quilt4Net.Toolkit.Api.Sample.Controllers;
using Quilt4Net.Toolkit.Features.Health;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddControllers();
//builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

builder.Services.AddHostedService<MyBackgroundService>();
builder.Services.AddHostedService<MyHostedService>();

builder.Services.AddApplicationInsightsTelemetry(new ApplicationInsightsServiceOptions { ConnectionString = builder.Configuration["ApplicationInsights:ConnectionString"] });
builder.Logging.AddApplicationInsights();

builder.AddQuilt4NetApi(o =>
{
    var config = new Dictionary<HealthEndpoint, AccessFlags>
    {
        [HealthEndpoint.Default] = new(true, false, true),
        [HealthEndpoint.Live] = new(true, false, true),
        [HealthEndpoint.Ready] = new(true, false, true),
        [HealthEndpoint.Health] = new(true, true, true),
        [HealthEndpoint.Dependencies] = new(true, false, true),
        [HealthEndpoint.Metrics] = new(true, false, true),
        [HealthEndpoint.Version] = new(true, false, true)
    };

    o.Endpoints = config.Encode();

    o.AddComponent(new Component
    {
        Name = "some-service",
        Essential = true,
        CheckAsync = async _ =>
        {
            await Task.Delay(TimeSpan.FromSeconds(1));
            return new CheckResult
            {
                Success = true,
                Message = "Some information"
            };
        }
    });

    o.AddComponentService<MyComponentService>();

    o.AddDependency(new Dependency
    {
        Name = "Self",
        Essential = true,
        Uri = new Uri("https://localhost:7119/api/Health/")
    });
});
builder.Services.AddQuilt4NetHealthClient(o =>
{
    o.HealthAddress = new Uri("https://localhost:7119/api/Health");
});
builder.Services.AddQuilt4NetApplicationInsights();

builder.Services.AddOpenApi();

var app = builder.Build();

if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
}

app.UseSwagger();
app.UseSwaggerUI();

app.UseHttpsRedirection();

app.UseRouting();

app.UseAuthorization();

app.MapControllers();

app.UseQuilt4NetApi();
app.Services.UseQuilt4NetHealthClient();

app.Run();
