using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Tellurian.Trains.ClockPulseApp.Service.Tests;

[TestClass]
public class TimeStringExtensionsTests
{
    [TestMethod]
    public void NullThrows()
    {
        Assert.ThrowsException<ArgumentNullException>(() =>
            TimeStringExtensions.AsTimeOnly(null));
    }

    [TestMethod]
    public void NonTimeThrows()
    {
        Assert.ThrowsException<ArgumentOutOfRangeException>(() =>
            "ABC-".AsTimeOnly());
    }

    [TestMethod]
    public void ParsesTime()
    {
        var actual = "12:15".AsTimeOnly();
        Assert.AreEqual(new TimeOnly(12,15,0), actual);
    }

    [TestMethod]
    public void DigitsOnlyThrows()
    {
        Assert.ThrowsException<ArgumentOutOfRangeException>(() =>
            "12345".AsTimeOnly());

    }

    [TestMethod]
    public void AddOneMonuteAt1259()
    {
        var time = new TimeOnly(12, 59, 0);
        var actual = time.AddOneMinute(true);
        Assert.AreEqual(1, actual.Hour);
    }

    [TestMethod]
    public void AddOneMonuteAt2359()
    {
        var time = new TimeOnly(23, 59, 0);
        var actual = time.AddOneMinute();
        Assert.AreEqual(0, actual.Hour);
        Assert.AreEqual(0, actual.Minute);
    }
}
