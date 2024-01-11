using Tellurian.Trains.ClockPulseApp.Service.Extensions;

namespace Tellurian.Trains.ClockPulseApp.Service.Sinks;
internal class AnalogueClockSimulationSink(TimeOnly initialTime, ILogger logger) : IPulseSink
{
    private TimeOnly Time = initialTime;
    private const bool Is12Hour = true;

    public ILogger Logger { get; } = logger;

    public Task PositiveVoltageAsync()
    {
        if (Time.Minute % 2 == 0) Time = Time.AddOneMinute(Is12Hour);
        Logger.LogInformation("\x1B[1m\x1B[34mAnalogue clock simulator time: {time}\x1B[39m\x1B[22m", Time.ToString(@"hh\:mm"));
        return Task.CompletedTask;
    }

    public Task NegativeVoltageAsync()
    {
        if (Time.Minute % 2 != 0) Time = Time.AddOneMinute(Is12Hour);
        Logger.LogInformation("\x1B[1m\x1B[34mAnalogue clock simulator time: {time}\x1B[39m\x1B[22m", Time.ToString(@"hh\:mm"));
        return Task.CompletedTask;
    }

    public Task ZeroVoltageAsync()
    {
        return Task.CompletedTask;
    }

    override public string ToString() => Time.AsString(Is12Hour);
}
