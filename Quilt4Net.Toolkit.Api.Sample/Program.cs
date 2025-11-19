using Microsoft.ApplicationInsights.AspNetCore.Extensions;
using Microsoft.AspNetCore.Authentication;
using Microsoft.OpenApi.Models;
using Quilt4Net.Toolkit.Api;
using Quilt4Net.Toolkit.Api.Framework.Endpoints;
using Quilt4Net.Toolkit.Api.Sample;
using Quilt4Net.Toolkit.Api.Sample.Controllers;
using Quilt4Net.Toolkit.Features.Health;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddAuthentication("Dummy")
    .AddScheme<AuthenticationSchemeOptions, DummyAuthHandler>("Dummy", _ => { });
builder.Services.AddAuthorization();

builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(c =>
{
    c.SwaggerDoc("v1", new OpenApiInfo { Title = "Quilt4Net Sample API V1", Version = "v1" });
    c.AddSecurityDefinition("ApiKey", new OpenApiSecurityScheme
    {
        In = ParameterLocation.Header,
        Name = DummyAuthHandler.ApiKeyHeaderName,
        Type = SecuritySchemeType.ApiKey,
        Description = "API Key needed to access the endpoints"
    });
    c.AddSecurityRequirement(new OpenApiSecurityRequirement
    {
        {
            new OpenApiSecurityScheme
            {
                Reference = new OpenApiReference
                {
                    Type = ReferenceType.SecurityScheme,
                    Id = "ApiKey"
                }
            },
            new List<string>()
        }
    });
    c.IncludeXmlComments(Path.Combine(AppContext.BaseDirectory, "Quilt4Net.Toolkit.Api.Sample.xml"));
});

builder.Services.AddHostedService<MyBackgroundService>();
builder.Services.AddHostedService<MyHostedService>();

builder.Services.AddApplicationInsightsTelemetry(new ApplicationInsightsServiceOptions { ConnectionString = builder.Configuration["ApplicationInsights:ConnectionString"] });
builder.Logging.AddApplicationInsights();

builder.AddQuilt4NetApplicationInsightsClient();
builder.AddQuilt4NetHealthClient();
builder.AddQuilt4NetContent();
builder.AddQuilt4NetRemoteConfiguration();
builder.AddQuilt4NetApi(o =>
{
    o.Certificate.SelfCheckEnabled = false;
    o.Certificate.CertExpiryUnhealthyLimitDays = 33;

    var config = new Dictionary<HealthEndpoint, AccessFlags>
    {
        [HealthEndpoint.Default] = new(true, true, true),
        [HealthEndpoint.Live] = new(true, false, true),
        [HealthEndpoint.Ready] = new(true, false, true),
        [HealthEndpoint.Health] = new(true, false, true),
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

var app = builder.Build();

app.UseSwagger();
app.UseSwaggerUI();

app.UseHttpsRedirection();

app.UseRouting();

app.UseAuthentication();
app.UseAuthorization();

app.MapControllers();

app.UseQuilt4NetApi();

app.Run();
