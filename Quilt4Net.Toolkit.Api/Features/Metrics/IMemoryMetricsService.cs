using System.Diagnostics;

namespace Quilt4Net.Toolkit.Api.Features.Metrics;

public interface IMemoryMetricsService
{
    Memory GetMemory(Process process);
}