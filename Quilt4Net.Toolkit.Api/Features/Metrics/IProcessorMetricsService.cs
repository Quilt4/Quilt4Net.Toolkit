using System.Diagnostics;

namespace Quilt4Net.Toolkit.Api.Features.Metrics;

/// <summary>
/// Service for Merics that provides processor information.
/// </summary>
public interface IProcessorMetricsService
{
    /// <summary>
    /// Get processor information for a process.
    /// </summary>
    /// <param name="process"></param>
    /// <returns></returns>
    Processor GetProcessor(Process process);
}