using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Tellurian.Trains.ClockPulseApp.Service.Extensions;
using Tellurian.Trains.ClockPulseApp.Service.Sinks;

namespace Tellurian.Trains.ClockPulseApp.Service.Tests;

[TestClass]
public partial class PulseGeneratorTests
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
        await target.Update(new() { IsRunning = true, Time = time.AsString() });
        while (time < endTime)
        {
            await Task.Delay(75);
            time = time.Add(TimeSpan.FromMinutes(1));
            await target.Update(new() { IsRunning = true, Time = time.AsString() });
        }
        Assert.AreEqual(12, sink.VoltageChanges.Count());
        CollectionAssert.AreEquivalent(
            sink.VoltageChanges.Select(vc => vc.Voltage).ToArray(),
            new[] { 12, 0, -12, 0, 12, 0, -12, 0, 12, 0, -12, 0 });
        Assert.AreEqual("06:06", target.AnalogueTime.AsString());
        Assert.AreEqual("06:06", target.ServerTime.AsString());
    }

    [TestMethod]
    public async Task TimeJumpCausesFastForward()
    {
        var sink = new MonitoringPulseSink();
        var target = CreateTargetWithSink(sink);
        var newTime = "06:10".AsTimeOnly();
        await target.Update(new() { IsRunning = true, Time = newTime.AsString() });
        Assert.AreEqual("06:10", target.AnalogueTime.AsString());
        Assert.AreEqual("06:10", target.ServerTime.AsString());
        Assert.AreEqual(20, sink.VoltageChanges.Count());
    }

    public static PulseGenerator CreateTargetWithSink(IPulseSink sink) =>
        new(Settings, new[] { sink, new LoggingSink(NullLogger.Instance) }, NullLogger.Instance, true, "06:00".AsTimeOnly());

}