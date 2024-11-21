# Quilt4Net Toolkit Api
[![NuGet](https://img.shields.io/nuget/v/Quilt4Net.Toolkit.Api)](https://www.nuget.org/packages/Quilt4Net.Toolkit.Api)
![Nuget](https://img.shields.io/nuget/dt/Quilt4Net.Toolkit.Api)
[![License](https://img.shields.io/badge/license-MIT-blue.svg)](LICENSE)

[![GitHub repo Issues](https://img.shields.io/github/issues/Quilt4/Quilt4Net.Toolkit?style=flat&logo=github&logoColor=red&label=Issues)](https://github.com/Quilt4/Quilt4Net.Toolkit/issues?q=is%3Aopen)

## Get started
After having installed the nuget package.
Register *AddQuilt4NetApi* as a service and use it in the app.
```
var builder = WebApplication.CreateBuilder(args);
...
builder.AddQuilt4NetApi();

var app = builder.Build();
...
app.UseRouting();
...
app.UseQuilt4NetApi();

app.Run();
```
You have to call `AddQuilt4NetApi` in any order on the *builder* (or *builder.Services*).
On the app you have to call `UseRouting` before `UseQuilt4NetApi`.

### Register service check
This is a basic way of adding a service check. This check will be performed when calling *Health*, *Ready* or *Dependencies*.
```
builder.AddQuilt4NetApi(o =>
{
    o.AddComponent(new Component
    {
        Name = "some-service",
        Essential = true,
        CheckAsync = async _ =>
        {
            await Task.Delay(TimeSpan.FromSeconds(1));
            return new CheckResult { Success = true };
        }
    });
});
```

### Configuration options
Configuration can be configured by code. This will override any other configuration.
```
builder.AddQuilt4NetApi(o =>
{
    o.ShowInSwagger = false;
    o.FailReadyWhenDegraded = true;
});
```
Configuration in *appsettings.json*.
```
{
  "Quilt4Net": {
    "ShowInSwagger": false,
    "FailReadyWhenDegraded" : true,
  }
}
```
For values without configuration default values are used.

- ShowInSwagger: Turns on visibility in swagger.
- FailReadyWhenDegraded: When calling *Ready* and the service is *Degraded* it sill by default return *200*. If this is set to *true*, the response will be *503* for degraded components.

### Endpoints
Use the endpoint in different scenarios.

#### Health
`~/api/Health/health`

Use this by ping-services to check that everything works as intended. It can also be used for smoke tests after release to assure that the service is working.

#### Liveness
`~/api/Health/live`

Use this endpoint to check if a new instance sould be started. Commonly used in *kubernetes* or *Azure* to make sure the correct number of pods or machines are active.

#### Readyness
`~/api/Health/ready`

Use this endpoint to check if the instance is ready to perform work.

## Troubleshooting
Error at startup with the message:
`Unhandled exception. System.InvalidOperationException: EndpointRoutingMiddleware matches endpoints setup by EndpointMiddleware and so must be added to the request execution pipeline before EndpointMiddleware. Please add EndpointRoutingMiddleware by calling 'IApplicationBuilder.UseRouting' inside the call to 'Configure(...)' in the application startup code.`

The solution is to add `app.UseRouting();` before `app.UseQuilt4NetApi();` in *Program.cs*.

## Planned
- IP-Address lookup
- Authentication for endpoints (Use project auth or API-Key for different methods.)
- Feature to check if background services are running or if they have crashed.
- Monitor service that can be implemented so that components does not have to be added with 'AddComponent' in 'AddQuilt4NetApi'.
- Possible to create custom implementation of services

# Quilt4Net Toolkit Client
[![NuGet](https://img.shields.io/nuget/v/Quilt4Net.Toolkit.Client)](https://www.nuget.org/packages/Quilt4Net.Toolkit.Client)
![Nuget](https://img.shields.io/nuget/dt/Quilt4Net.Toolkit.Client)
[![License](https://img.shields.io/badge/license-MIT-blue.svg)](LICENSE)

Client toolkit for Quilt4Net that can access *Application Insights*
and consume the result of *Quilt4Net Toolkit Api*