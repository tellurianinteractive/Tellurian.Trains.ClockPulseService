using Tellurian.Trains.ClockPulseApp.Service.Extensions;
using Tellurian.Trains.MeetingApp.Contracts;

namespace Tellurian.Trains.ClockPulseApp.Service;

public sealed class PulseGenerator : IAsyncDisposable
{
    private const string LastAnalogueTimeFileName = "AnalogueTime.txt";

    private readonly ILogger Logger;
    private readonly PulseGeneratorSettings Settings;
    private readonly IEnumerable<IPulseSink> Sinks;
    private readonly bool ResetOnStart;
    private readonly TimeOnly RestartTime;
    private bool IsInitialized;
    private bool CanWriteFiles;

    public TimeOnly ServerTime { get; private set; }
    public TimeOnly AnalogueTime { get; private set; }
    public string RemoteClockTimeHref => Settings.RemoteClockTimeHref;
    public int PollIntervalSeconds => Settings.PollIntervalSeconds;
    public int ErrorWaitRetryMilliseconds => Settings.ErrorWaitRetrySeconds * 60;
    private int ZeroDurationMilliseconds => GetZeroDurationMilliseconds(Settings);
    private int SessionEndedIndicationSeconds => Settings.SessionEndedIndicationSeconds;

    private static int GetZeroDurationMilliseconds(PulseGeneratorSettings settings)
    {
        const int MinimumMillisecondsAfterSetZero = 250;
        var result = settings.FastForwardIntervalMilliseconds - settings.PulseDurationMilliseconds;
        if (result < MinimumMillisecondsAfterSetZero) result = MinimumMillisecondsAfterSetZero;
        return result;
    }

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

    /// <summary>
    /// Main logic to control pulses to an analogue clock from the Fast Clock API (https://fastclock.azurewebsites.net/openapi).
    /// </summary>
    /// <param name="status"></param>
    /// <remarks>
    /// This method is a good template for any other implementation on other platforms and languages.
    /// </remarks>
    public async Task Update(ClockStatus status)
    {
        if (!IsInitialized) await InitializeAsync();
        await HandleClockStartAndStop(status);
        if (AnalougueClockShouldNotMove(status)) return;

        ServerTime = status.Time.AsTimeOnly(Settings.Use12HourClock);
        if (AnalogueTime.IsInSyncWith(ServerTime, Settings.Use12HourClock))
        {
        }
        else if (AnalogueTime.IsOneMinuteAfter(ServerTime, Settings.Use12HourClock))
        {
            Logger.LogInformation("\x1B[1m\x1B[33mServer time: {time}\x1B[39m\x1B[22m", ServerTime.AsString(Settings.Use12HourClock));
            AnalogueTime = await PulseOneMinute(AnalogueTime);
        }
        else
        {
            await FastForward();
        }

        static bool AnalougueClockShouldNotMove(ClockStatus status) =>
           status.IsUnavailable || status.IsRealtime || status.IsPaused || status.IsCompleted;
    }

    private async Task InitializeAsync()
    {
        await TryResetAnalogueTime();
        AnalogueTime = await InitializeAnalogueTime();
        await Notify<IControlSink>(InitializeAsync);
        Logger.LogInformation("Analogue time starting at {time}", AnalogueTime.AsString());
        IsInitialized = true;

        async Task InitializeAsync(IControlSink sink)
        {
            try
            {
                await sink.InitializeAsync(AnalogueTime);
            }
            catch (Exception ex)
            {
                Logger.LogError(ex, "Sink {sink} failed initialization.", sink.GetType().Name);
            }
        }
    }

    private async Task HandleClockStartAndStop(ClockStatus status)
    {
        if (status.WasStopped(Previous))
        {
            await Notify<IStatusSink>(NotifyStopped);
            if (status.IsCompleted)
            {
                await Task.Delay(TimeSpan.FromSeconds(SessionEndedIndicationSeconds));
                await Notify<IStatusSink>(NotifySessionCompleted);
            }
        }
        else if (status.WasStarted(Previous))
        {
            await Notify<IStatusSink>(NotifyStarted);
        }
        Previous = status;

        async Task NotifyStopped(IStatusSink sink)
        {
            try
            {
                await sink.ClockIsStoppedAsync();
            }
            catch (Exception ex)
            {
                Logger.LogError(ex, "Sink {sink} faulted when clock stopped.", sink.GetType().Name);
            }
        }
        async Task NotifyStarted(IStatusSink sink)
        {
            try
            {
                await sink.ClockIsStartedAsync();
            }
            catch (Exception ex)
            {
                Logger.LogError(ex, "Sink {sink} faulted when clock started.", sink.GetType().Name);
            }
        }
        async Task NotifySessionCompleted(IStatusSink sink)
        {
            try
            {
                await sink.SessionIsCompletedAsync();
            }
            catch (Exception ex)
            {
                Logger.LogError(ex, "Sink {sink} faulted when session was finished.", sink.GetType().Name);
            }
        }
    }

    private async Task TryResetAnalogueTime()
    {
        if (ResetOnStart)
        {
            await SaveAnalogueClockTime(RestartTime);
        }
    }

    private async Task<TimeOnly> InitializeAnalogueTime()
    {
        var initialTime = RestartTime;
        if (File.Exists(LastAnalogueTimeFileName))
        {
            var fileTime = await File.ReadAllTextAsync(LastAnalogueTimeFileName);
            if (fileTime?.Length >= 5) initialTime = fileTime[..5].AsTimeOnly(Settings.Use12HourClock);
            else Logger.LogWarning("Cannot interpret time in file {file}", LastAnalogueTimeFileName);
        }
        return initialTime;
    }

    private async Task FastForward()
    {
        using PeriodicTimer fastTimer = new(TimeSpan.FromMilliseconds(Settings.FastForwardIntervalMilliseconds));
        Logger.LogInformation("\x1B[1m\x1B[33mStart fast forwarding to time: {time}\x1B[39m\x1B[22m", ServerTime.AsString(Settings.Use12HourClock));
        foreach (var sink in Sinks.OfType<IAnalogueClockStatus>()) await sink.AnalogueClocksAreFastForwardingAsync();
        while (!AnalogueTime.IsInSyncWith(ServerTime, Settings.Use12HourClock))
        {
            await fastTimer.WaitForNextTickAsync();
            AnalogueTime = await PulseOneMinute(AnalogueTime);
        }
        foreach (var sink in Sinks.OfType<IAnalogueClockStatus>()) await sink.AnalogueClocksStoppedFastForwardingAsync();
    }

    private async Task<TimeOnly> PulseOneMinute(TimeOnly analogueTime)
    {
        if (analogueTime.Minute % 2 == 0)
            if (Settings.FlipPolarity) await SetPositive(); else await SetNegative();
        else
            if (Settings.FlipPolarity) await SetNegative(); else await SetPositive();

        var updatedTime = await GetUpdatedTime(analogueTime);

        await Task.Delay(Settings.PulseDurationMilliseconds);
        await SetZero();
        await Task.Delay(ZeroDurationMilliseconds);

        return updatedTime;
    }

    private async Task<TimeOnly> GetUpdatedTime(TimeOnly analogueTime)
    {
        var updatedTime = analogueTime.AddOneMinute(Settings.Use12HourClock);
        await SaveAnalogueClockTime(updatedTime);
        Logger.LogInformation("\x1B[1m\x1B[33mUpdated analogue time: {time}\x1B[39m\x1B[22m", updatedTime.AsString(Settings.Use12HourClock));
        return updatedTime;
    }

    private async Task SaveAnalogueClockTime(TimeOnly time)
    {
        if (!CanWriteFiles) return;
        try
        {
            await File.WriteAllTextAsync(LastAnalogueTimeFileName, time.AsString());

        }
        catch (IOException ex)
        {
            CanWriteFiles = false;
            Logger.LogError(ex, "Cannot write analogue time to {file}", LastAnalogueTimeFileName);
        }
    }

    private async Task SetPositive()
    {
        await Notify<IPulseSink>(SetPositive);
        async Task SetPositive(IPulseSink sink)
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
        await Notify<IPulseSink>(SetNegative);
        async Task SetNegative(IPulseSink sink)
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
        await Notify<IPulseSink>(SetZero);
        async Task SetZero(IPulseSink sink)
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

    private async ValueTask Notify<TSink>(Func<TSink, Task> action) where TSink : class
    {
        var tasks = Sinks.OfType<TSink>().Select(sink => action(sink)).ToArray();
        await Task.WhenAll(tasks);
    }

    public async ValueTask DisposeAsync()
    {
        Logger.LogInformation("Disposing {component}...", nameof(PulseGenerator));
        await Notify<IControlSink>(Cleanup);
        await Notify<IDisposable>(Dispose);
        await Notify<IAsyncDisposable>(DisposeAsync);
        Logger.LogInformation("Disposed {component}", nameof(PulseGenerator));

        async Task Cleanup(IControlSink sink) => await sink.CleanupAsync();
        Task Dispose(IDisposable sink) { sink.Dispose(); return Task.CompletedTask; }
        async Task DisposeAsync(IAsyncDisposable sink) => await sink.DisposeAsync();
    }

    public override string ToString() => Settings.ToString();
}
