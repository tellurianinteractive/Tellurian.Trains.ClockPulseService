using Tellurian.Trains.MeetingApp.Contracts;

namespace Tellurian.Trains.ClockPulseApp.Service;

public sealed class PulseGenerator : IAsyncDisposable
{
    private readonly ILogger Logger;
    private readonly PulseGeneratorSettings Settings;
    private readonly IEnumerable<IPulseSink> Sinks;
    private readonly bool ResetOnStart;
    private readonly TimeOnly RestartTime;
    private bool IsInitialized;
    private bool CanWriteFiles;

    public TimeOnly CurrentTime { get; private set; }
    public TimeOnly AnalogueClockTime { get; private set; }
    public string RemoteClockTimeHref => Settings.RemoteClockTimeHref;
    public int PollIntervalSeconds => Settings.PollIntervalSeconds;
    public int ErrorWaitRetryMilliseconds => Settings.ErrorWaitRetrySeconds * 60;
    private int ZeroMilliseconds => Settings.FastForwardIntervalMilliseconds - Settings.PulseDurationMilliseconds;

    private ClockStatus? Previous;

    public IEnumerable<string> InstalledSinksTypes => Sinks.Select(s => s.GetType().Name);
    public PulseGenerator(PulseGeneratorSettings settings, IEnumerable<IPulseSink> sinks, ILogger logger, bool resetOnStart, TimeOnly restartTime)
    {
        Settings = settings;
        Sinks = sinks;
        Logger = logger;
        CanWriteFiles = true;
        ResetOnStart = resetOnStart;
        RestartTime = restartTime;
    }

    private async Task InitializeAsync()
    {
        TryResetAnalogueTime();
        foreach (var sink in Sinks.OfType<IControlSink>()) await sink.InitializeAsync();
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
                Logger.LogInformation("Analogue time reset to {time)", RestartTime);

            }
            catch (IOException ex)
            {
                Logger.LogError(ex, "Cannot reset analogue time because deletion of file '{file}' failed.", LastAnalogueTimeFileName);
            }
        }
    }

    private async Task<TimeOnly> InitializeAnalogueTime()
    {
        var initialTime = RestartTime;
        if (File.Exists(LastAnalogueTimeFileName))
        {
            var fileTime = await File.ReadAllTextAsync(LastAnalogueTimeFileName);
            if (fileTime is not null && fileTime.Length >= 5) initialTime = fileTime[..5].AsTimeOnly(Settings.Use12HourClock);
            else Logger.LogWarning("Cannot interpret time in file {file}", LastAnalogueTimeFileName);
        }
        return initialTime;
    }


    public async Task Update(ClockStatus status)
    {
        if (!IsInitialized) await InitializeAsync();
        if (status.WasStopped(Previous))
        {
            Previous = status;
            foreach (var sink in Sinks.OfType<IStatusSink>())
            {
                try
                {
                    await sink.ClockIsStoppedAsync();
                }
                catch (Exception ex)
                {
                    Logger.LogError(ex, "Sink {sink} faulted when stopping.", sink.GetType().Name);
                }
            }
            return;
        }
        else if (status.WasStarted(Previous))
        {
            Previous = status;
            foreach (var sink in Sinks.OfType<IStatusSink>())
                try
                {
                    await sink.ClockIsStartedAsync();
                }
                catch (Exception ex)
                {
                    Logger.LogError(ex, "Sink {sink} faulted when starting.", sink.GetType().Name);
                }
        }
        CurrentTime = status.Time.AsTimeOnly(Settings.Use12HourClock);
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
        while (!AnalogueClockTime.IsEqualTo(CurrentTime, Settings.Use12HourClock))
        {
            await fastTimer.WaitForNextTickAsync();
            await FastForwardOneMinute();
        }
    }

    private async Task FastForwardOneMinute()
    {
        await MoveOneMinute();
        await SaveAnalogueTime();
        AnalogueClockTime = AnalogueClockTime.AddOneMinute(Settings.Use12HourClock);
        Logger.LogInformation("\x1B[1m\x1B[33mFast forwarding analogue time. Updated analogue time: {time}\x1B[39m\x1B[22m", AnalogueClockTime.AsTime(Settings.Use12HourClock));
    }

    private async Task MoveOneMinute()
    {
        if (AnalogueClockTime.Minute % 2 == 0)
            if (Settings.FlipPolarity) await SetPositive(); else await SetNegative();
        else
            if (Settings.FlipPolarity) await SetNegative(); else await SetPositive();

        await Task.Delay(Settings.PulseDurationMilliseconds);
        await SetZero();
        await Task.Delay(ZeroMilliseconds);
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
        foreach (var sink in Sinks)
        {
            try
            {
                await sink.PositiveVoltageAsync();
            }
            catch (Exception ex)
            {
                Logger.LogError(ex, "Sink {sink} faulted when set positive.", sink.GetType().Name);
            }
        }
    }
    private async Task SetNegative()
    {
        foreach (var sink in Sinks)
        {
            try
            {
                await sink.NegativeVoltageAsync();
            }
            catch (Exception ex)
            {
                Logger.LogError(ex, "Sink {sink} faulted when set negative.", sink.GetType().Name);
            }
        }
    }
    private async Task SetZero()
    {
        foreach (var sink in Sinks)
        {
            try
            {
                await sink.ZeroVoltageAsync();

            }
            catch (Exception ex)
            {
                Logger.LogError(ex, "Sink {sink} faulted when set zero.", sink.GetType().Name);
            }
        }
    }

    public async ValueTask DisposeAsync()
    {
        Logger.LogInformation("Disposing {component}...", nameof(PulseGenerator));
        foreach (var sink in Sinks.OfType<IControlSink>()) await sink.CleanupAsync();
        foreach (var sink in Sinks.OfType<IDisposable>()) sink.Dispose();
        foreach (var sink in Sinks.OfType<IAsyncDisposable>()) await sink.DisposeAsync();
        Logger.LogInformation("Disposed {component}", nameof(PulseGenerator));
    }

    public override string ToString() => Settings.ToString();
}
