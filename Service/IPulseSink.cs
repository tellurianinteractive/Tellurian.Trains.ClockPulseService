namespace Tellurian.Trains.ClockPulseApp.Service;

public interface IPulseSink
{
    public Task PositiveVoltageAsync();
    public Task NegativeVoltageAsync();
    public Task ZeroVoltageAsync();
}
