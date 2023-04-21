namespace Tellurian.Trains.ClockPulseApp.Service;

public interface IPulseSink: IControlSink
{
    public Task PositiveVoltageAsync();
    public Task NegativeVoltageAsync();
    public Task ZeroVoltageAsync();



}
