using System.Diagnostics;

namespace Quilt4Net.Toolkit.Api.Features.Metrics;

public interface IProcessorMetricsService
{
    Processor GetProcessor(Process process);
}