namespace Tellurian.Trains.ClockPulseApp.Service.Tests;

public partial class PulseGeneratorTests
{
    internal class FailingPulseSink : IPulseSink
    {
        public Task NegativeVoltageAsync() { Assert.Fail("Negative"); return Task.CompletedTask; }
        public Task PositiveVoltageAsync() { Assert.Fail("Positive"); return Task.CompletedTask; }
        public Task ZeroVoltageAsync() { Assert.Fail("Zero"); return Task.CompletedTask; }
    }

}