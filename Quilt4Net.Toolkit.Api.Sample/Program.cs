using Microsoft.ApplicationInsights.AspNetCore.Extensions;
using Quilt4Net.Toolkit;
using Quilt4Net.Toolkit.Api;
using Quilt4Net.Toolkit.Api.Sample.Controllers;
using Quilt4Net.Toolkit.Features.Health;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddControllers();
builder.Services.AddSwaggerGen();

builder.Services.AddHostedService<MyBackgroundService>();
builder.Services.AddHostedService<MyHostedService>();

builder.Services.AddApplicationInsightsTelemetry(new ApplicationInsightsServiceOptions { ConnectionString = builder.Configuration["ApplicationInsights:ConnectionString"] });
builder.Logging.AddApplicationInsights();

builder.AddQuilt4NetApi(o =>
{
    //o.FailReadyWhenDegraded = true;
    o.LogHttpRequest = true;

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
        Name = "A",
        Essential = true,
        Uri = new Uri("https://localhost:7119/api/Health/")
    });
    o.AddDependency(new Dependency
    {
        Name = "B",
        Essential = false,
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
//app.UseQuilt4NetHealthClient();
app.Services.UseQuilt4NetHealthClient();

app.Run();
