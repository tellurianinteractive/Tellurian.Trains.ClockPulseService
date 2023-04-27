namespace Tellurian.Trains.ClockPulseApp.Service.Tests;

public partial class PulseGeneratorTests
{
    internal sealed record VoltageChange(DateTimeOffset Timestamp, int Voltage);

}