namespace Tellurian.Trains.ClockPulseApp.Service;
internal class AnalogueClockSimulationPulseSink : IPulseSink
{
    private TimeSpan Time;

    public ILogger Logger { get; }

    public AnalogueClockSimulationPulseSink(TimeSpan initialTime, ILogger logger)
    {
        Time = initialTime;
        Logger = logger;
    }

    public Task InitializeAsync() => Task.CompletedTask;
    public Task CleanupAsync() => Task.CompletedTask;

    public Task PositiveVoltageAsync()
    {
        if (Time.Minutes % 2 == 0) Time = Time.AddOneMinute(true);
        Logger.LogInformation("\x1B[1m\x1B[34mAnalogue clock simulator time: {time}\x1B[39m\x1B[22m", Time.ToString(@"hh\:mm"));
        return Task.CompletedTask;
    }

    public Task NegativeVoltageAsync()
    {
        if (Time.Minutes % 2 != 0) Time = Time.AddOneMinute(true);
        Logger.LogInformation("\x1B[1m\x1B[34mAnalogue clock simulator time: {time}\x1B[39m\x1B[22m", Time.ToString(@"hh\:mm"));
        return Task.CompletedTask;
    }

    public Task ZeroVoltageAsync()
    {
        return Task.CompletedTask;
    }

    override public string ToString() => Time.ToString();
}
