namespace Tellurian.Trains.ClockPulseApp.Service;

/// <summary>
/// Interface to implement for any type of hardware control
/// </summary>
public interface IPulseSink
{ 
    /// <summary>
    /// Called when a clock pulse with reversed polarity should be started. 
    /// </summary>
    public Task PositiveVoltageAsync();
    /// <summary>
    /// Called when a clock pulse with straight polarity should be started. 
    /// </summary>
    public Task NegativeVoltageAsync();
    /// <summary>
    /// Called when zero voltage should be set. 
    /// </summary>
    public Task ZeroVoltageAsync();
}
