namespace Quilt4Net.Toolkit.Api.Features.Startup;

public enum StartupStatusResult
{
    /// <summary>
    /// The application is in the process of initializing but is not yet ready to serve traffic.
    /// </summary>
    Starting,

    /// <summary>
    /// The application has successfully completed startup routines and is ready to handle readiness and health checks.
    /// </summary>
    Started,

    /// <summary>
    /// The application failed to start properly.
    /// </summary>
    Failed
}