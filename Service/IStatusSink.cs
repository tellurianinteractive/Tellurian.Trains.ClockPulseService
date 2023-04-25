namespace Tellurian.Trains.ClockPulseApp.Service;
/// <summary>
/// Optional interface for indicating when clock is stopped.
/// This can be used to control any audio and/or visual indicator.
/// </summary>
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
    /// <returns></returns>
    public Task ClockIsStoppedAsync();
}
