namespace Tellurian.Trains.ClockPulseApp.Service.Tests;

internal sealed class MonitoringTimeSink : IPulseSink, ITimeSink
{
    private readonly List<TimeOnly> _times = [];

    public IReadOnlyList<TimeOnly> Times => _times;

    public Task PositiveVoltageAsync() => Task.CompletedTask;
    public Task NegativeVoltageAsync() => Task.CompletedTask;
    public Task ZeroVoltageAsync() => Task.CompletedTask;

    public Task TimeChangedAsync(TimeOnly time)
    {
        _times.Add(time);
        return Task.CompletedTask;
    }
}
