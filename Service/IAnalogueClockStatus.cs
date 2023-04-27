namespace Tellurian.Trains.ClockPulseApp.Service;
/// <summary>
/// Information about whether analogue clock is fastforwarding or not.
/// </summary>
/// <remarks>
/// Each method must complete within approx 100 milliseconds.
/// </remarks>
public interface IAnalogueClockStatus
{
    /// <summary>
    /// Called when <see cref="PulseGenerator"/> starts fast forwardning.
    /// </summary>
    public Task AnalogueClocksAreFastForwardingAsync();

    /// <summary>
    /// Called when <see cref="PulseGenerator"/> stops fast forwardning.
    /// </summary>
    public Task AnalogueClocksStoppedFastForwardingAsync();

}
