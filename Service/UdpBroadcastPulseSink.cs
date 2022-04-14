using System.Net;
using System.Net.Sockets;

namespace Tellurian.Trains.ClockPulseApp.Service;

public sealed class UdpBroadcastPulseSink : IPulseSink, IDisposable
{
    private readonly ILogger Logger;
    private readonly IPEndPoint BroadcastEndpoint;
    private readonly UdpClient Broadcaster;

    private static readonly byte[] Positive = new[] { (byte)0x2B }; 
    private static readonly byte[] Negative = new[] { (byte)0x2D }; 
    private static readonly byte[] Zero = new[] { (byte)0x5F }; 

    public UdpBroadcastPulseSink(IPEndPoint broadcastEndpoint, ILogger logger)
    {
        Logger = logger;
        BroadcastEndpoint = broadcastEndpoint;
        Broadcaster = new();
        Broadcaster.EnableBroadcast = true;       
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

