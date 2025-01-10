namespace Quilt4Net.Toolkit;

public record Quilt4NetHealthOptions
{
    /// <summary>
    /// Address to the health API.
    /// </summary>
    public Uri HealthAddress { get; set; }

    ///// <summary>
    ///// Monitor name used to track log-items to selected monitor.
    ///// If set to empty string the value will be omitted.
    ///// Default is Quilt4Net.
    ///// </summary>
    //public string MonitorName { get; set; } = Constants.Monitor;
}