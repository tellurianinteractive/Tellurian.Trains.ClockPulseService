namespace Tellurian.Trains.ClockPulseApp.Service;

/// <summary>
/// Optional interface for sinks that need the actual Fastclock time,
/// for example digital displays or servers, rather than minute pulses.
/// </summary>
/// <remarks>
/// Each method must complete within approx 100 milliseconds.
/// </remarks>
public interface ITimeSink
{
    /// <summary>
    /// Called with the Fastclock time in 24-hour format, the first time it is available
    /// and then each time it changes.
    /// It is not affected by the 12-hour setting used for analogue clocks.
    /// </summary>
    public Task TimeChangedAsync(TimeOnly time);
}
