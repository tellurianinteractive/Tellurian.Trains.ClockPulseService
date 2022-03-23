namespace Tellurian.Trains.ClockPulseApp.Service;

public interface IPulseSink
{
    public Task StartAsync();
    public Task PositiveVoltageAsync();
    public Task NegativeVoltageAsync();
    public Task ZeroVoltageAsync();
    public Task StopAsync();
}
