# Quilt4Net Toolkit Api
[![NuGet](https://img.shields.io/nuget/v/Quilt4Net.Toolkit.Api)](https://www.nuget.org/packages/Quilt4Net.Toolkit.Api)
![Nuget](https://img.shields.io/nuget/dt/Quilt4Net/Quilt4Net.Toolkit.Api)
[![License](https://img.shields.io/badge/license-MIT-blue.svg)](LICENSE)

[![GitHub repo Issues](https://img.shields.io/github/issues/Quilt4/Quilt4Net.Toolkit?style=flat&logo=github&logoColor=red&label=Issues)](https://github.com/Quilt4/Quilt4Net.Toolkit/issues?q=is%3Aopen)

## Get started
After having installed the nuget package.
Register *AddQuilt4Net* as a service and use it in the app.
```
var builder = WebApplication.CreateBuilder(args);

...

builder.Services.AddQuilt4Net();

var app = builder.Build();

...

app.UseQuilt4Net();

app.Run();
```

### Register service check
This is a basic way of adding a service check. This check will be performed when calling *Health*, *Ready* or *Dependencies*.
```
builder.AddQuilt4Net(o =>
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
builder.AddQuilt4Net(o =>
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

## Planned
- IP-Address lookup
- Authentication for endpoints (Use project auth or API-Key for different methods.)
- Feature to check if background services are running or if they have crashed.
- Monitor service that can be implemented so that components does not have to be added with 'AddComponent' in 'AddQuilt4Net'.
- Possible to create custom implementation of services

# Quilt4Net Toolkit
[![NuGet](https://img.shields.io/nuget/v/Quilt4Net.Toolkit)](https://www.nuget.org/packages/Quilt4Net.Toolkit)
![Nuget](https://img.shields.io/nuget/dt/Quilt4Net/Quilt4Net.Toolkit)
[![License](https://img.shields.io/badge/license-MIT-blue.svg)](LICENSE)

This package can be used on the client side to consume the result of *Quilt4Net Toolkit Api*