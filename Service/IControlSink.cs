namespace Tellurian.Trains.ClockPulseApp.Service;

/// <summary>
/// Interface for initializing a clock for the first time and clean up any resources when the app is closing.
/// </summary>
/// <remarks>
/// Each method must complete within approx 100 milliseconds.
/// </remarks>
public interface IControlSink
{
    /// <summary>
    /// Called before any pulses are generated.
    /// </summary>
    public Task InitializeAsync(TimeOnly analogueStartTime);
    /// <summary>
    /// Called after the app is requested to close.
    /// </summary>
    public Task CleanupAsync();

}
