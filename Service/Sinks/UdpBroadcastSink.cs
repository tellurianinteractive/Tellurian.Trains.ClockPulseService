using System.Net;
using System.Net.Sockets;

namespace Tellurian.Trains.ClockPulseApp.Service.Sinks;

public sealed class UdpBroadcastSink : IPulseSink, IDisposable
{
    private readonly ILogger Logger;
    private readonly IPEndPoint BroadcastEndpoint;
    private readonly UdpClient Broadcaster;

    private static readonly byte[] Positive = "+"u8.ToArray();
    private static readonly byte[] Negative = "-"u8.ToArray();
    private static readonly byte[] Zero = "_"u8.ToArray();

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
    public Task InitializeAsync()
    {
        Logger.LogInformation("UDP broadcast started on {endpoint}.", BroadcastEndpoint);
        return Task.CompletedTask;
    }

    public Task CleanupAsync()
    {
        Logger.LogInformation("UDP broadcast stopped on {endpoint}.", BroadcastEndpoint);
        return Task.CompletedTask;
    }
    public void Dispose() => Broadcaster.Dispose();
}

