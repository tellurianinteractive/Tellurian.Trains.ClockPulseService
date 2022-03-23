using Microsoft.Extensions.Options;
using Tellurian.Trains.MeetingApp.Contracts;

namespace Tellurian.Trains.ClockPulseApp.Service;

public sealed class PulseGenerator : IAsyncDisposable
{
    private readonly ILogger Logger;
    private readonly PulseGeneratorSettings Settings;
    private readonly IEnumerable<IPulseSink> Sinks;

    public TimeSpan CurrentTime { get; private set; }
    public TimeSpan AnalogueClockTime { get; private set; }
    public string RemoteClockTimeHref => Settings.RemoteClockTimeHref;
    public int PollIntervalSeconds => Settings.PollIntervalSeconds;
    public PulseGenerator(IOptions<PulseGeneratorSettings> options, IEnumerable<IPulseSink> sinks, ILogger logger)
    {
        Settings = options.Value;
        Sinks = sinks;
        AnalogueClockTime = Settings.AnalogueClockStartTime.AsTimespan(Settings.Use12HourClock);
        Logger = logger;
    }

    public async Task Update(ClockStatus status)
    {
        Logger.LogInformation("Analogue time: {time}", AnalogueClockTime.AsTime(Settings.Use12HourClock));
        if (status.IsUnavailable || status.IsRealtime || status.IsPaused) return;
        CurrentTime = status.Time.AsTimespan(Settings.Use12HourClock);
        if (CurrentTime == AnalogueClockTime) return;
        if (AnalogueClockTime.IsOneMinuteAfter(CurrentTime, Settings.Use12HourClock))
        {
            await MoveOneMinute();
            AnalogueClockTime = CurrentTime;
        }
        else
        {
            await FastForward();
        }
        await Task.CompletedTask;
    }

    private async Task FastForward()
    {
        PeriodicTimer fastTimer = new(TimeSpan.FromMilliseconds(Settings.FastForwardMinuteMilliseconds));
        while (AnalogueClockTime != CurrentTime)
        {
            await fastTimer.WaitForNextTickAsync();
            await MoveOneMinute();
            AnalogueClockTime = AnalogueClockTime.AddOneMinute(Settings.Use12HourClock);
            Logger.LogInformation("Fast forwarding analogue time: {time}", AnalogueClockTime.AsTime(Settings.Use12HourClock));
        }
    }

    private async Task MoveOneMinute()
    {
        if (AnalogueClockTime.Minutes % 2 == 0)
            await SetNegative();
        else
            await SetPositive();

        await Task.Delay(Settings.PulseLengthMilliseconds);
        await SetZero();
    } 
    
    private async Task SetPositive()
    {
        foreach (var sink in Sinks) await sink.PositiveVoltageAsync();
    }
    private async Task SetNegative()
    {
        foreach (var sink in Sinks) await sink.NegativeVoltageAsync();
    }
    private async Task SetZero()
    {
        foreach (var sink in Sinks) await sink.ZeroVoltageAsync();
    }

    public async ValueTask DisposeAsync()
    {
        foreach (var sink in Sinks.OfType<IDisposable>())  sink.Dispose();
        foreach(var sink in Sinks.OfType<IAsyncDisposable>()) await sink.DisposeAsync();
    }

    public override string ToString() => Settings.ToString();
}
