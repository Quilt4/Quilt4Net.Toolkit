using System.Diagnostics;

namespace Quilt4Net.Toolkit.Features.Health.Metrics;

/// <summary>
/// Service for Merics that provides memory information.
/// </summary>
public interface IMemoryMetricsService
{
    /// <summary>
    /// Get memory information for a process.
    /// </summary>
    /// <param name="process"></param>
    /// <returns></returns>
    Memory GetMemory(Process process);
}