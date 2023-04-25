using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Tellurian.Trains.ClockPulseApp.Service.Tests;

[TestClass]
public class PulseGeneratorTests
{
    static PulseGeneratorSettings Settings => new()
    {
        FastForwardIntervalMilliseconds = 200,
        PulseDurationMilliseconds = 100,
        AnalogueClockStartTime = "06:00"
    };

    [TestMethod]
    public async Task StatusesNotAffectingClock()
    {
        var target = CreateTargetWithSink(new FailingPulseSink());

        await target.Update(new() { IsUnavailable = true });
        await target.Update(new() { IsRealtime = true });
        await target.Update(new() { IsPaused = true });
        await target.Update(new() { Time = "06:00", IsRunning = true });
    }

    [TestMethod]
    public async Task RuningClockGeneratesPulses()
    {
        var sink = new MonitoringPulseSink();
        var target = CreateTargetWithSink(sink);
        var time = "06:00".AsTimeOnly();
        var endTime = "06:06".AsTimeOnly();
        await target.Update(new() { IsRunning = true, Time = time.AsTime() });
        while (time < endTime)
        {
            await Task.Delay(75);
            time = time.Add(TimeSpan.FromMinutes(1));
            await target.Update(new() { IsRunning = true, Time = time.AsTime() });
        }
        Assert.AreEqual(12, sink.VoltageChanges.Count());
        CollectionAssert.AreEquivalent(
            sink.VoltageChanges.Select(vc => vc.Voltage).ToArray(),
            new[] { 12, 0, -12, 0, 12, 0, -12, 0, 12, 0, -12, 0 });
        Assert.AreEqual("06:06", target.AnalogueClockTime.AsTime());
        Assert.AreEqual("06:06", target.CurrentTime.AsTime());
    }

    [TestMethod]
    public async Task TimeJumpCausesFastForward()
    {
        var sink = new MonitoringPulseSink();
        var target = CreateTargetWithSink(sink);
        var newTime = "06:10".AsTimeOnly();
        await target.Update(new() { IsRunning = true, Time = newTime.AsTime() });
        Assert.AreEqual("06:10", target.AnalogueClockTime.AsTime());
        Assert.AreEqual("06:10", target.CurrentTime.AsTime());
        Assert.AreEqual(20, sink.VoltageChanges.Count());
    }

    public static PulseGenerator CreateTargetWithSink(IPulseSink sink) =>
        new(Settings, new[] { sink, new LoggingPulseSink(NullLogger.Instance) }, NullLogger.Instance, true, "06:00".AsTimeOnly());

    internal class FailingPulseSink : IPulseSink
    {
        public Task NegativeVoltageAsync() { Assert.Fail("Negative"); return Task.CompletedTask; }
        public Task PositiveVoltageAsync() { Assert.Fail("Positive"); return Task.CompletedTask; }
        public Task ZeroVoltageAsync() { Assert.Fail("Zero"); return Task.CompletedTask; }
        public Task InitializeAsync() => Task.CompletedTask;
        public Task CleanupAsync() => Task.CompletedTask;
    }

    internal class MonitoringPulseSink : IPulseSink
    {
        private readonly List<VoltageChange> voltageChanges = new();
        public IEnumerable<VoltageChange> VoltageChanges => voltageChanges;
        public Task NegativeVoltageAsync()
        {
            voltageChanges.Add(new(DateTimeOffset.Now, -12));
            return Task.CompletedTask;
        }

        public Task PositiveVoltageAsync()
        {
            voltageChanges.Add(new(DateTimeOffset.Now, 12));
            return Task.CompletedTask;
        }

        public Task InitializeAsync() => Task.CompletedTask;
        public Task CleanupAsync() => Task.CompletedTask;

        public Task ZeroVoltageAsync()
        {
            voltageChanges.Add(new(DateTimeOffset.Now, 0));
            return Task.CompletedTask;
        }
    }

    internal sealed record VoltageChange(DateTimeOffset Timestamp, int Voltage);

}