using System.Net;
using System.Net.Sockets;

namespace Tellurian.Trains.ClockPulseApp.Service.Sinks;

public sealed class UdpBroadcastSink : IControlSink, IStatusSink, IPulseSink, IDisposable
{
    private readonly ILogger Logger;
    private readonly IPEndPoint BroadcastEndpoint;
    private readonly UdpClient Broadcaster;

    private static readonly byte[] Positive = "+"u8.ToArray();
    private static readonly byte[] Negative = "-"u8.ToArray();
    private static readonly byte[] Zero = "_"u8.ToArray();
    private static readonly byte[] Stopped = "-"u8.ToArray();
    private static readonly byte[] Started = "|"u8.ToArray();
    private static readonly byte[] Completed = "X"u8.ToArray();

    public UdpBroadcastSink(IPEndPoint broadcastEndpoint, ILogger logger)
    {
        Logger = logger;
        BroadcastEndpoint = broadcastEndpoint;
        Broadcaster = new()
        {
            EnableBroadcast = true
        };
    }

    public async Task NegativeVoltageAsync() => await Broadcaster.SendAsync(Negative, 1, BroadcastEndpoint);
    public async Task PositiveVoltageAsync() => await Broadcaster.SendAsync(Positive, 1, BroadcastEndpoint);
    public async Task ZeroVoltageAsync() => await Broadcaster.SendAsync(Zero, 1, BroadcastEndpoint);
    public Task InitializeAsync(TimeOnly analogueStartTime)
    {
        Logger.LogInformation("UDP broadcast started on {endpoint}.", BroadcastEndpoint);
        return Task.CompletedTask;
    }

    public Task CleanupAsync()
    {
        Logger.LogInformation("UDP broadcast stopped on {endpoint}.", BroadcastEndpoint);
        return Task.CompletedTask;
    }
    public async Task ClockIsStartedAsync() => await Broadcaster.SendAsync(Started, 1, BroadcastEndpoint);
    public async Task ClockIsStoppedAsync() => await Broadcaster.SendAsync(Stopped, 1, BroadcastEndpoint);
    public async Task SessionIsCompletedAsync() => await Broadcaster.SendAsync(Completed, 1, BroadcastEndpoint);
    public void Dispose() => Broadcaster.Dispose();

}

