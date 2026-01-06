# Quilt4Net Toolkit Api
[![GitHub repo](https://img.shields.io/github/repo-size/Quilt4/Quilt4Net.Toolkit?style=flat&logo=github&logoColor=red&label=Repo)](https://github.com/Quilt4/Quilt4Net.Toolkit)

Add extra api logging with `AddQuilt4NetApiLogging`.

---
# Features moved
Some features have moved to *Quilt4Net Toolkit Health*.

## Get started
After having installed the nuget package.
Register *AddQuilt4NetHealthApi* as a service and use it in the app.
```
var builder = WebApplication.CreateBuilder(args);
...
builder.AddQuilt4NetHealthApi();

var app = builder.Build();
...
app.UseRouting();
...
app.UseQuilt4NetApi();

app.Run();
```
You have to call `AddQuilt4NetHealthApi` in any order on the *builder* (or *builder.Services*).
On the app you have to call `UseRouting` before `UseQuilt4NetApi`.

### Register service check
This is a basic way of adding a service check. This check will be performed when calling *Health*, *Ready* or *Dependencies*.
```
builder.AddQuilt4NetHealthApi(o =>
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

For more complex scenarios, implement *IComponentService* and add the servcice here to separate the setup from the implementation.
```
builder.AddQuilt4NetHealthApi(o =>
{
    o.AddComponentService<MyComponentService>();
});
```

To add dependency information to other services that uses *Quilt4Net API*. This will call the health check on the other service.
```
builder.AddQuilt4NetHealthApi(o =>
{
    o.AddDependency(new Dependency
    {
        Name = "Dependency",
        Essential = true,
        Uri = new Uri("https://localhost:7119/api/Health/")
    });
});
```

### Configuration options
Configuration can be configured by code. This will override any other configuration.
```
builder.AddQuilt4NetHealthApi(o =>
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

## Service Probe
TODO: Revisit

## Troubleshooting
Error at startup with the message:
`Unhandled exception. System.InvalidOperationException: EndpointRoutingMiddleware matches endpoints setup by EndpointMiddleware and so must be added to the request execution pipeline before EndpointMiddleware. Please add EndpointRoutingMiddleware by calling 'IApplicationBuilder.UseRouting' inside the call to 'Configure(...)' in the application startup code.`

The solution is to add `app.UseRouting();` before `app.UseQuilt4NetApi();` in *Program.cs*.
