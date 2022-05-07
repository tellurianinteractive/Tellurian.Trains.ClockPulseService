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
    private readonly bool ResetOnStart;

    public TimeSpan CurrentTime { get; private set; }
    public TimeSpan AnalogueClockTime { get; private set; }
    public string RemoteClockTimeHref => Settings.RemoteClockTimeHref;
    public int PollIntervalSeconds => Settings.PollIntervalSeconds;
    public int ErrorWaitRetryMilliseconds => Settings.ErrorWaitRetrySeconds * 60;

    public IEnumerable<string> InstalledSinksTypes => Sinks.Select(s => s.GetType().Name);
    public PulseGenerator(IOptions<PulseGeneratorSettings> options, IEnumerable<IPulseSink> sinks, ILogger logger, bool resetOnStart = false)
    {
        Settings = options.Value;
        Sinks = sinks;
        Logger = logger;
        CanWriteFiles = true;
        ResetOnStart = resetOnStart;
    }

    private async Task InitializeAsync()
    {
        TryResetAnalogueTime();
        foreach (var sink in Sinks) await sink.InitializeAsync();
        AnalogueClockTime = await InitializeAnalogueTime();
        Logger.LogInformation("Analogue time starting at {time}", AnalogueClockTime.AsTime());
        IsInitialized = true;
    }

    private void TryResetAnalogueTime()
    {
        if (ResetOnStart && File.Exists(LastAnalogueTimeFileName))
        {
            try
            {
                File.Delete(LastAnalogueTimeFileName);
                Logger.LogInformation("Analogue time reset to {time)", Settings.AnalogueClockStartTime);

            }
            catch (IOException ex)
            {
                Logger.LogError(ex, "Cannot reset analogue time because deletion of file '{file}' failed.", LastAnalogueTimeFileName);
            }
        }
    }

    private async Task<TimeSpan> InitializeAnalogueTime()
    {
        var initialTime = Settings.AnalogueClockStartTime;
        if (File.Exists(LastAnalogueTimeFileName))
        {
            var fileTime = await File.ReadAllTextAsync(LastAnalogueTimeFileName);
            if (fileTime is not null && fileTime.Length >= 5) initialTime = fileTime[..5];
            else Logger.LogWarning($"Cannot read {LastAnalogueTimeFileName}.");
        }
        return initialTime.AsTimespan(Settings.Use12HourClock);
    }


    public async Task Update(ClockStatus status)
    {
        if (!IsInitialized) await InitializeAsync();
        if (status.IsUnavailable || status.IsRealtime || status.IsPaused)
        {
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
        //TODO: Inform ClockServer that analogue clock is syncing and stop clock.
        using PeriodicTimer fastTimer = new(TimeSpan.FromMilliseconds(Settings.FastForwardIntervalMilliseconds));
        while (AnalogueClockTime != CurrentTime)
        {
            await fastTimer.WaitForNextTickAsync();
            await FastForwardOneMinute();
            AnalogueClockTime = AnalogueClockTime.AddOneMinute(Settings.Use12HourClock);
        }
        await fastTimer.WaitForNextTickAsync();
        await FastForwardOneMinute();
    }

    private async Task FastForwardOneMinute()
    {
        await MoveOneMinute();
        await SaveAnalogueTime();
        Logger.LogInformation("\x1B[1m\x1B[33mFast forwarding analogue time. Updated analogue time: {time}\x1B[39m\x1B[22m", AnalogueClockTime.AsTime(Settings.Use12HourClock));
    }

    private async Task MoveOneMinute()
    {
        if (AnalogueClockTime.Minutes % 2 == 0)
            await SetNegative();
        else
            await SetPositive();

        await Task.Delay(Settings.PulseDurationMilliseconds);
        await SetZero();
        await Task.Delay(Settings.FastForwardIntervalMilliseconds - Settings.PulseDurationMilliseconds);
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
            Logger.LogError(ex, "Cannot write analogue time to {file}", LastAnalogueTimeFileName);
        }
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
