using Microsoft.Extensions.Options;
using Tellurian.Trains.MeetingApp.Contracts;

namespace Tellurian.Trains.ClockPulseApp.Service;

public sealed class PulseGenerator : IAsyncDisposable
{
    private readonly ILogger Logger;
    private readonly PulseGeneratorSettings Settings;
    private readonly IEnumerable<IPulseSink> Sinks;
    private bool IsInitialized;
    private bool CanWriteFiles;
    private bool WasStopped;

    public TimeSpan CurrentTime { get; private set; }
    public TimeSpan AnalogueClockTime { get; private set; }
    public string RemoteClockTimeHref => Settings.RemoteClockTimeHref;
    public int PollIntervalSeconds => Settings.PollIntervalSeconds;

    public IEnumerable<string> InstalledSinksTypes => Sinks.Select(s => s.GetType().Name);
    public PulseGenerator(IOptions<PulseGeneratorSettings> options, IEnumerable<IPulseSink> sinks, ILogger logger)
    {
        Settings = options.Value;
        Sinks = sinks;
        Logger = logger;
        CanWriteFiles = true;
    }

    private async Task InitializeAsync()
    {
        foreach (var sink in Sinks) await sink.InitializeAsync();
        AnalogueClockTime = await InitializeAnalogueTime();
        Logger.LogInformation("Analogue time starting at {time}", AnalogueClockTime.AsTime());
        IsInitialized = true;
    }

    public async Task Update(ClockStatus status)
    {
        if (!IsInitialized) await InitializeAsync();
        if (status.IsUnavailable || status.IsRealtime || status.IsPaused) {
            foreach (var sink in Sinks.OfType<IStatusSink>()) await sink.ClockIsStoppedAsync();
            WasStopped = true;
            return;
        }
        if (WasStopped)
        {
            foreach (var sink in Sinks.OfType<IStatusSink>()) await sink.ClockIsStartedAsync();
            WasStopped = false;
        }
        CurrentTime = status.Time.AsTimespan(Settings.Use12HourClock);
        if (CurrentTime == AnalogueClockTime) return;
        if (AnalogueClockTime.IsOneMinuteAfter(CurrentTime, Settings.Use12HourClock))
        {
            await MoveOneMinute();
            AnalogueClockTime = CurrentTime;
            await SaveAnalogueTime();
        }
        else
        {
            await FastForward();
        }
        Logger.LogInformation("\x1B[1m\x1B[33mUpdated analogue time: {time}\x1B[39m\x1B[22m", AnalogueClockTime.AsTime(Settings.Use12HourClock));
        await Task.CompletedTask;
    }

    private async Task FastForward()
    {
        using PeriodicTimer fastTimer = new(TimeSpan.FromMilliseconds(Settings.FastForwardIntervalMilliseconds));
        while (AnalogueClockTime != CurrentTime)
        {
            await fastTimer.WaitForNextTickAsync();
            await MoveOneMinute();
            AnalogueClockTime = AnalogueClockTime.AddOneMinute(Settings.Use12HourClock);
            await SaveAnalogueTime();
            Logger.LogInformation("\x1B[1m\x1B[33mFast forwarding analogue time: {time}\x1B[39m\x1B[22m", AnalogueClockTime.AsTime(Settings.Use12HourClock));
        }
    }

    private async Task MoveOneMinute()
    {
        if (AnalogueClockTime.Minutes % 2 == 0)
            await SetNegative();
        else
            await SetPositive();

        await Task.Delay(Settings.PulseDurationMilliseconds);
        await SetZero();
    }

    private const string LastAnalogueTimeFileName = "AnalogueTime.txt";
    private async Task SaveAnalogueTime()
    {
        if (!CanWriteFiles) return;
        try
        {
            await File.WriteAllTextAsync(LastAnalogueTimeFileName, AnalogueClockTime.AsTime());

        }
        catch (IOException ex)
        {
            CanWriteFiles = false;
            Logger.LogError(ex,"Cannot write analogue time to {file}", LastAnalogueTimeFileName);
        }
    }

    private async Task<TimeSpan> InitializeAnalogueTime()
    {
        var initialTime = Settings.AnalogueClockStartTime;
        if (File.Exists(LastAnalogueTimeFileName))
        {
            var fileTime = await File.ReadAllTextAsync(LastAnalogueTimeFileName);
            if (fileTime is not null && fileTime.Length == 5) initialTime = fileTime;

        }
        return initialTime.AsTimespan(Settings.Use12HourClock);

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
        Logger.LogInformation("Disposing {component}...", nameof(PulseGenerator));
        foreach (var sink in Sinks) await sink.CleanupAsync();
        foreach (var sink in Sinks.OfType<IDisposable>()) sink.Dispose();
        foreach (var sink in Sinks.OfType<IAsyncDisposable>()) await sink.DisposeAsync();
        Logger.LogInformation("Disposed {component}", nameof(PulseGenerator));
    }

    public override string ToString() => Settings.ToString();
}
