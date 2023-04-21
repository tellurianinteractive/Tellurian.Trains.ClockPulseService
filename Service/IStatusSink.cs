namespace Tellurian.Trains.ClockPulseApp.Service;
public interface IStatusSink
{
    public Task ClockIsStartedAsync();
    public Task ClockIsStoppedAsync();
}

public interface IControlSink
{
    public Task InitializeAsync();
    public Task CleanupAsync();

}
