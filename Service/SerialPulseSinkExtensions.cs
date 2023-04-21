namespace Tellurian.Trains.ClockPulseApp.Service;

public static class SerialPulseSinkExtensions
{
    public static bool IsValidSerialPortName(this string? name) =>
        !string.IsNullOrWhiteSpace(name) && name.ToUpperInvariant().StartsWith("COM");
}


