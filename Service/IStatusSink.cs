namespace Tellurian.Trains.ClockPulseApp.Service;
public interface IStatusSink
{
    public Task InitializeAsync();
    public Task CleanupAsync();
    public Task ClockIsStartedAsync();
    public Task ClockIsStoppedAsync();

}
