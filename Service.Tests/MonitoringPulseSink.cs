namespace Tellurian.Trains.ClockPulseApp.Service.Tests;

public partial class PulseGeneratorTests
{
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

        public Task ZeroVoltageAsync()
        {
            voltageChanges.Add(new(DateTimeOffset.Now, 0));
            return Task.CompletedTask;
        }
    }

}