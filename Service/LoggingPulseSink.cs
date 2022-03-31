namespace Tellurian.Trains.ClockPulseApp.Service;

public sealed class LoggingPulseSink : IPulseSink
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
        Logger.LogInformation("{Timestamp:o} Started logging sink.", DateTimeOffset.Now);
        return Task.CompletedTask;
    }

    public Task CleanupAsync()
    {
        Logger.LogInformation("{Timestamp:o} Stopped logging sink.", DateTimeOffset.Now);
        return Task.CompletedTask;
    }
}
