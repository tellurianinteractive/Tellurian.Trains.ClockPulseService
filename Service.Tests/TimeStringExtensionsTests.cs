using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Tellurian.Trains.ClockPulseApp.Service.Tests;

[TestClass]
public class TimeStringExtensionsTests
{
    [TestMethod]
    public void NullThrows()
    {
        Assert.ThrowsException<ArgumentNullException>(() =>
            TimeStringExtensions.AsTimespan(null));
    }

    [TestMethod]
    public void NonTimeThrows()
    {
        Assert.ThrowsException<ArgumentOutOfRangeException>(() =>
            "ABC-".AsTimespan());       
    }

    [TestMethod]
    public void ParsesTime()
    {
       var actual=  "12:15".AsTimespan();
        Assert.AreEqual(TimeSpan.FromHours(12.25), actual);
    }

    [TestMethod]
    public void DigitsOnlyThrows()
    {
        Assert.ThrowsException<ArgumentOutOfRangeException>(() =>
            "12345".AsTimespan());
        
    }

    [TestMethod]
    public void AddOneMonuteAt1259()
    {
        var time = new TimeSpan(12, 59, 0);
        var actual = time.AddOneMinute(true);
        Assert.AreEqual(1.0, actual.TotalHours);
    }

    [TestMethod]
    public void AddOneMonuteAt2359()
    {
        var time = new TimeSpan(23, 59, 0);
        var actual = time.AddOneMinute();
        Assert.AreEqual(TimeSpan.Zero, actual);
    }
}
