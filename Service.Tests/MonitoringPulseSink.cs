namespace Tellurian.Trains.ClockPulseApp.Service.Tests;

public partial class PulseGeneratorTests
{
    internal class MonitoringPulseSink : IPulseSink, IStatusSink
    {
        private readonly List<VoltageChange> voltageChanges = [];
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

        public bool IsRunning { get; private set; }
        public bool IsCompleted { get; private set; }
        public Task ClockIsStartedAsync() { IsRunning = true; return Task.CompletedTask; }
        public Task ClockIsStoppedAsync() { IsRunning = false; return Task.CompletedTask; }
        public Task SessionIsCompletedAsync() { IsRunning = true; IsCompleted = true; return Task.CompletedTask; }
    }
}

