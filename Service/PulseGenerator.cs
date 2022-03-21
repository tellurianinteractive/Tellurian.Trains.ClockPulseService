using Microsoft.Extensions.Options;
using Tellurian.Trains.MeetingApp.Contracts;

namespace Tellurian.Trains.ClockPulseApp.Service;
public sealed class PulseGenerator
{
    private readonly PulseGeneratorOptions Settings;
    private readonly IEnumerable<IPulseSink> Sinks;
    public TimeSpan CurrentTime { get; private set; }
    public TimeSpan AnalogueClockTime { get; private set; }
    public string RemoteClockTimeHref => Settings.RemoteClockTimeHref;
    public int PollIntervalSeconds => Settings.PollIntervalSeconds;
    private readonly ILogger Logger;
    public PulseGenerator(IOptions<PulseGeneratorOptions> options, IEnumerable<IPulseSink> sinks, ILogger logger)
    {
        Settings = options.Value;
        Sinks = sinks;
        AnalogueClockTime = Settings.AnalogueClockStartTime.AsTimespan();
        Logger = logger;
    }

    public async Task Update(ClockStatus status)
    {
        Logger.LogInformation("Analogue time: {time}", AnalogueClockTime.AsTime());
        if (status.IsUnavailable || status.IsRealtime || status.IsPaused) return;
        CurrentTime = status.Time.AsTimespan();
        if (CurrentTime == AnalogueClockTime) return;
        if ((CurrentTime - AnalogueClockTime).TotalMinutes == 1)
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

    private async Task FastForward()
    {
        PeriodicTimer fastTimer = new(TimeSpan.FromMilliseconds(Settings.FastForwardMinuteMilliseconds));
        while (AnalogueClockTime != CurrentTime)
        {
            await fastTimer.WaitForNextTickAsync();
            await MoveOneMinute();
            AnalogueClockTime += TimeSpan.FromMinutes(1);
            Logger.LogInformation("Fast forwarding analogue time: {time}", AnalogueClockTime.AsTime());
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
}

public sealed class PulseGeneratorOptions
{
    public string RemoteClockTimeHref { get; set; } = string.Empty;
    public string AnalogueClockStartTime { get; set; } = "06:00";
    public int PollIntervalSeconds { get; set; }
    public int PulseLengthMilliseconds { get; init; }
    public int FastForwardMinuteMilliseconds { get; init; }

}

public interface IPulseSink
{
    public Task PositiveVoltageAsync();
    public Task NegativeVoltageAsync();
    public Task ZeroVoltageAsync();
}

public static class TimeStringExtensions
{
    public static TimeSpan AsTimespan(this string? time) =>
        time is null ? throw new ArgumentNullException(nameof(time)) :
        time.Length == 5 && time[2] == ':' ? TimeSpan.Parse(time) :
        throw new ArgumentOutOfRangeException(nameof(time), time);

    public static string AsTime(this TimeSpan time) =>
        $"{time.Hours:00}:{time.Minutes:00}";
}

public class LoggingPulseSink : IPulseSink
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
}
