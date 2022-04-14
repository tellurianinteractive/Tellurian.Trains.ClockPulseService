namespace Tellurian.Trains.ClockPulseApp.Service;

public sealed class LoggingPulseSink : IPulseSink, IStatusSink
{
    public LoggingPulseSink(ILogger logger) => Logger = logger ?? throw new ArgumentNullException(nameof(logger));

    private readonly ILogger Logger;
    public Task NegativeVoltageAsync()
    {
        Logger.LogInformation("{Timestamp:o} \x1B[1m\x1B[31mNegative voltage\x1B[39m\x1B[22m", DateTimeOffset.Now);
        return Task.CompletedTask;
    }

    public Task PositiveVoltageAsync()
    {
        Logger.LogInformation("{Timestamp:o} \x1B[1m\x1B[32mPositive voltage\x1B[39m\x1B[22m", DateTimeOffset.Now);
        return Task.CompletedTask;
    }

    public Task ZeroVoltageAsync()
    {
        Logger.LogInformation("{Timestamp:o} \x1B[1m\x1B[36mZero voltage\x1B[39m\x1B[22m", DateTimeOffset.Now);
        return Task.CompletedTask;
    }

    public Task InitializeAsync()
    {
        Logger.LogInformation("{Timestamp:o} Initialized logging sink.", DateTimeOffset.Now);
        return Task.CompletedTask;
    }

    public Task CleanupAsync()
    {
        Logger.LogInformation("{Timestamp:o} Cleaned up logging sink.", DateTimeOffset.Now);
        return Task.CompletedTask;
    }

    public Task ClockIsStartedAsync()
    {
        Logger.LogInformation("{Timestamp:o} Clock was started.", DateTimeOffset.Now);
        return Task.CompletedTask;
    }
    public Task ClockIsStoppedAsync()
    {
        Logger.LogInformation("{Timestamp:o} Clock was stopped.", DateTimeOffset.Now);
        return Task.CompletedTask;
    }
}
