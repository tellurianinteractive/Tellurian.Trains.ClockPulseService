using System.Text;
using Tellurian.Trains.ClockPulseApp.Service.Sinks;

namespace Tellurian.Trains.ClockPulseApp.Service;

public sealed class PulseGeneratorSettings
{
    public string RemoteClockTimeHref { get; set; } = string.Empty;
    public string AnalogueClockStartTime { get; set; } = "06:00";
    public bool Use12HourClock { get; set; } = false;
    public bool FlipPolarity { get; set; } = false;
    public int PollIntervalSeconds { get; set; }
    public int PulseDurationMilliseconds { get; init; }
    public int FastForwardIntervalMilliseconds { get; init; }
    public int ErrorWaitRetrySeconds { get; init; } = 60;
    public int SessionEndedIndicationSeconds { get; init; } = 5;
    public UdpBroadcastSettings UdpBroadcast { get; set; } = new();
    public SerialPulseSinkSettings SerialPulseSink { get; init; } = new();
    public RpiRelayBoardPulseSinkSettings RpiRelayBoardPulseSink { get; init; } = new();
    public override string ToString()
    {
        var text = new StringBuilder(200);
        text.AppendLine($"Remote clock time href: {RemoteClockTimeHref}");
        text.AppendLine($"Poll interval seconds: {PollIntervalSeconds}");
        text.AppendLine($"Analogue clock start time: {AnalogueClockStartTime}");
        text.AppendLine($"Use 12 hour clock: {Use12HourClock}");
        text.AppendLine($"Pulse duration: {PulseDurationMilliseconds} ms");
        text.AppendLine($"Fast-forward interval: {FastForwardIntervalMilliseconds} ms");
        text.AppendLine(SerialPulseSink.ToString());
        return text.ToString();
    }
}

public sealed class UdpBroadcastSettings
{
    public bool Disabled { get; set; } = true;
    public string IPAddress { get; set; } = string.Empty;
    public int PortNumber { get; set; }
}

public sealed class SerialPulseSinkSettings
{
    public bool Disabled { get; set; } = true;
    public string PortName { get; set; } = string.Empty;

    public override string ToString() =>
        PortName.IsValidSerialPortName() ? $"Serial pulse sink: {PortName}" : string.Empty;
}

public sealed class RpiRelayBoardPulseSinkSettings
{
    public bool Disabled { get; set; } = true;
    public string ClockStoppedPinUse { get; set; } = Sinks.ClockStoppedPinUse.RedGreen.ToString();
    public override string ToString() => "RPI Relay Board: no specific setting";
}


