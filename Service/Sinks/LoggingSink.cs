namespace Tellurian.Trains.ClockPulseApp.Service.Sinks;

public sealed class LoggingSink(ILogger logger) : IPulseSink, IStatusSink, IControlSink, IAnalogueClockStatus
{
    private readonly ILogger Logger = logger ?? throw new ArgumentNullException(nameof(logger));
    public Task NegativeVoltageAsync()
    {
        Logger.LogInformation("\x1B[1m\x1B[31mNegative voltage\x1B[39m\x1B[22m");
        return Task.CompletedTask;
    }

    public Task PositiveVoltageAsync()
    {
        Logger.LogInformation("\x1B[1m\x1B[32mPositive voltage\x1B[39m\x1B[22m");
        return Task.CompletedTask;
    }

    public Task ZeroVoltageAsync()
    {
        Logger.LogInformation("\x1B[1m\x1B[36mZero voltage\x1B[39m\x1B[22m");
        return Task.CompletedTask;
    }

    public Task InitializeAsync(TimeOnly analogueTime)
    {
        Logger.LogInformation("Initialized logging sink with analogue time {time:T}.", analogueTime);
        return Task.CompletedTask;
    }

    public Task CleanupAsync()
    {
        Logger.LogInformation("Cleaned up logging sink.");
        return Task.CompletedTask;
    }

    public Task ClockIsStartedAsync()
    {
        Logger.LogInformation("Clock was \x1B[1m\x1B[32mstarted\x1B[39m\x1B[22m.");
        return Task.CompletedTask;
    }
    public Task ClockIsStoppedAsync()
    {
        Logger.LogInformation("Clock was \x1B[1m\x1B[31mstopped\x1B[39m\x1B[22m.");
        return Task.CompletedTask;
    }
    public Task SessionIsCompletedAsync()
    {
        Logger.LogInformation("Session is \x1B[1m\x1B[31mcompleted\x1B[39m\x1B[22m.");
        return Task.CompletedTask;
    }

    public Task AnalogueClocksAreFastForwardingAsync()
    {
        Logger.LogInformation("Clock was starting fast forwarding.");
        return Task.CompletedTask;
    }

    public Task AnalogueClocksStoppedFastForwardingAsync()
    {
        Logger.LogInformation("Clock was stopping fast forwarding.");
        return Task.CompletedTask;
    }
}
