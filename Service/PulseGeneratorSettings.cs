namespace Tellurian.Trains.ClockPulseApp.Service;

public sealed class PulseGeneratorSettings
{
    public string RemoteClockTimeHref { get; set; } = string.Empty;
    public string AnalogueClockStartTime { get; set; } = "06:00";
    public bool Use12HourClock { get; set; } = false;
    public int PollIntervalSeconds { get; set; }
    public int PulseLengthMilliseconds { get; init; }
    public int FastForwardMinuteMilliseconds { get; init; }

    public override string ToString() =>
        $"Remote clock time href: {RemoteClockTimeHref}, " +
        $"Poll interval seconds: {PollIntervalSeconds}, " +
        $"Analogue clock start time: {AnalogueClockStartTime}, " +
        $"Use 12 hour clock: {Use12HourClock}, " +
        $"Pulse length: {PulseLengthMilliseconds} ms, " +
        $"Fast-forward minute: {FastForwardMinuteMilliseconds} ms";
}
