﻿using Quilt4Net.Toolkit.Features.Health;

namespace Quilt4Net.Toolkit.Api.Features.Probe;

public interface IHostedServiceProbe<TCategory> : IHostedServiceProbe
{
    IHostedServiceProbe Register(TimeSpan? plannedInterval = default, bool autoMaxInterval = true);
}

public interface IHostedServiceProbe
{
    string Name { get; }
    void Pulse();
    IHostedServiceProbe Register(string name, TimeSpan? plannedInterval = default, bool autoMaxInterval = true);
    void EndService(bool success);
    void EndService(Exception exception);
    HealthComponent GetHealth();
}