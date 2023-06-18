namespace Tellurian.Trains.ClockPulseApp.Service;
/// <summary>
/// Optional interface for indicating when clock is stopped.
/// This can be used to control any audio and/or visual indicator.
/// </summary>
/// <remarks>
/// Each method must complete within approx 100 milliseconds.
/// </remarks>
public interface IStatusSink
{
    /// <summary>
    /// Called when the clock starts to run.
    /// </summary>
    public Task ClockIsStartedAsync();

    /// <summary>
    /// Called when the clock is stopped for som reason, 
    /// including before session starts and after end of game session.
    /// </summary>
    public Task ClockIsStoppedAsync();

    /// <summary>
    /// Called when the session time is ended, and a few seconds after <see cref="ClockIsStoppedAsync"/>.
    /// </summary>
    public Task SessionIsCompletedAsync();
}
