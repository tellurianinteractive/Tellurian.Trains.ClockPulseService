namespace Tellurian.Trains.ClockPulseApp.Service;

/// <summary>
/// Interface for initializing a clock for the first time and clean up any resources when the app is closing.
/// </summary>
public interface IControlSink
{
    /// <summary>
    /// Called before any pulses are generated.
    /// </summary>
    public Task InitializeAsync();
    /// <summary>
    /// Called after the app is requested to close.
    /// </summary>
    public Task CleanupAsync();

}
