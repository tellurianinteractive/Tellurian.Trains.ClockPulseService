namespace Tellurian.Trains.ClockPulseApp.Service;

public sealed class LoggingPulseSink : IPulseSink
{
    public LoggingPulseSink(ILogger logger) => Logger = logger ?? throw new ArgumentNullException(nameof(logger));

    private readonly ILogger Logger;
    public Task NegativeVoltageAsync()
    {
        Logger.LogInformation("{Timestamp:o} Negative voltage", DateTimeOffset.Now);
        return Task.CompletedTask;
    }

    public Task PositiveVoltageAsync()
    {
        Logger.LogInformation("{Timestamp:o} Positive voltage", DateTimeOffset.Now);
        return Task.CompletedTask;
    }

    public Task ZeroVoltageAsync()
    {
        Logger.LogInformation("{Timestamp:o} Zero voltage", DateTimeOffset.Now);
        return Task.CompletedTask;
    }

    public Task StartAsync()
    {
        Logger.LogInformation("{Timestamp:o} Started logging sink.", DateTimeOffset.Now);
        return Task.CompletedTask;
    }

    public Task StopAsync()
    {
        Logger.LogInformation("{Timestamp:o} Stopped logging sink.", DateTimeOffset.Now);
        return Task.CompletedTask;
    }
}
