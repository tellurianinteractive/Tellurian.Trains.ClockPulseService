using System.Net;
using System.Net.Sockets;
using Microsoft.Extensions.Logging;
using Tellurian.Trains.MeetingApp.Contracts;

namespace Tellurian.Trains.ClockPulseApp.Service.Sinks;

public sealed class ZNServerSink : IPulseSink, IControlSink, IStatusSink, IDisposable
{
    private readonly ILogger Logger;
    private readonly UdpClient BroadcastClient;
    private readonly ZNServerSettings Settings;
    private IPEndPoint? ZNServerEndPoint;
    private bool IsDisposed;
    private DateTimeOffset lastMessageTime;
    private const int CONNECTION_TIMEOUT_MINUTES = 5;
    private Timer? connectionMonitorTimer;

    public ZNServerSink(ZNServerSettings settings, ILogger logger)
    {
        Settings = settings ?? throw new ArgumentNullException(nameof(settings));
        Logger = logger ?? throw new ArgumentNullException(nameof(logger));
        BroadcastClient = new UdpClient()
        {
            EnableBroadcast = true
        };
    }

    private async Task SendIdMessage()
    {
        if (IsDisposed || ZNServerEndPoint == null) return;
        var message = System.Text.Encoding.UTF8.GetBytes($"ID:{Settings.StationCode},{Settings.StationName}\r\n");
        await BroadcastClient.SendAsync(message, message.Length, ZNServerEndPoint);
        UpdateLastMessageTime();
        Logger.LogDebug("ZNServer: ID message sent for station {code} ({name})", Settings.StationCode, Settings.StationName);
    }

    private async Task DiscoverZNServer()
    {
        if (Settings.Disabled) return;
        try
        {
            var discoveryMessage = "ZNSERVER?"u8.ToArray();
            var broadcastEndpoint = new IPEndPoint(IPAddress.Parse(Settings.DiscoveryIPAddress), Settings.DiscoveryPort);
            var timeout = DateTime.UtcNow.AddSeconds(5);
            while (DateTime.UtcNow < timeout && !IsDisposed && ZNServerEndPoint == null)
            {
                await BroadcastClient.SendAsync(discoveryMessage, discoveryMessage.Length, broadcastEndpoint);
                var receiveTask = BroadcastClient.ReceiveAsync();
                if (await Task.WhenAny(receiveTask, Task.Delay(500)) == receiveTask)
                {
                    var result = await receiveTask;
                    var ZNresponse = System.Text.Encoding.UTF8.GetString(result.Buffer);
                    if (ZNresponse.StartsWith("ZNSERVERPORT:"))
                    {
                        var portStr = ZNresponse.Substring("ZNSERVERPORT:".Length);
                        if (int.TryParse(portStr, out var port))
                        {
                            ZNServerEndPoint = new IPEndPoint(result.RemoteEndPoint.Address, port);
                            Logger.LogInformation("ZNServer discovered at {address}:{port}", 
                                ZNServerEndPoint.Address, ZNServerEndPoint.Port);
                            await SendIdMessage();
                            break;
                        }
                        else
                        {
                            Logger.LogWarning("Received invalid port in ZNServer response: {response}", ZNresponse);
                            // Immediately retry
                            continue;
                        }
                    }
                }
                else
                {
                    Logger.LogWarning("No ZNServer response received within timeout");
                    break;
                }
            }
        }
        catch (Exception ex)
        {
            Logger.LogError(ex, "Error during ZNServer discovery");
        }
    }

    private void StartConnectionMonitoring()
    {
        lastMessageTime = DateTimeOffset.UtcNow;
        connectionMonitorTimer?.Dispose();
        connectionMonitorTimer = new Timer(CheckConnection, null, TimeSpan.FromMinutes(1), TimeSpan.FromMinutes(1));
    }

    private async void CheckConnection(object? state)
    {
        if (IsDisposed) return;

        var timeSinceLastMessage = DateTimeOffset.UtcNow - lastMessageTime;
        if (timeSinceLastMessage.TotalMinutes >= CONNECTION_TIMEOUT_MINUTES)
        {
            Logger.LogWarning("No messages from ZNServer for {minutes} minutes, attempting rediscovery", CONNECTION_TIMEOUT_MINUTES);
            await ReconnectToServer();
        }
    }

    private async Task ReconnectToServer()
    {
        ZNServerEndPoint = null;
        await DiscoverZNServer();
        if (ZNServerEndPoint != null && lastTime.HasValue)
        {
            await SendTimeUpdate(lastTime.Value.ToString("HH:mm"));
        }
    }

    private void UpdateLastMessageTime()
    {
        lastMessageTime = DateTimeOffset.UtcNow;
    }

    private async Task SendTimeUpdate(string time)
    {
        if (IsDisposed || ZNServerEndPoint == null) return;
        try
        {
            var message = System.Text.Encoding.UTF8.GetBytes($"UHR:{time}");
            await BroadcastClient.SendAsync(message, message.Length, ZNServerEndPoint);
            UpdateLastMessageTime();
            Logger.LogDebug("ZNServer: Time update sent {time}", time);
        }
        catch (Exception ex)
        {
            Logger.LogError(ex, "Failed to send time update to ZNServer");
            await ReconnectToServer();
        }
    }

    public async Task InitializeAsync(TimeOnly analogueStartTime)
    {
        Logger.LogInformation("ZNServer sink initializing at {time}", analogueStartTime);
        await DiscoverZNServer();
        if (ZNServerEndPoint != null)
        {
            await SendTimeUpdate(analogueStartTime.ToString("HH:mm"));
            lastTime = analogueStartTime;
            StartConnectionMonitoring();
        }
    }

    public Task CleanupAsync()
    {
        connectionMonitorTimer?.Dispose();
        Logger.LogInformation("ZNServer sink cleaned up");
        return Task.CompletedTask;
    }

    private TimeOnly? lastTime;

    public async Task NegativeVoltageAsync()
    {
        if (lastTime.HasValue)
        {
            var nextTime = lastTime.Value.AddMinutes(1);
            await SendTimeUpdate(nextTime.ToString("HH:mm"));
            lastTime = nextTime;
        }
    }

    public async Task PositiveVoltageAsync()
    {
        if (lastTime.HasValue)
        {
            var nextTime = lastTime.Value.AddMinutes(1);
            await SendTimeUpdate(nextTime.ToString("HH:mm"));
            lastTime = nextTime;
        }
    }

    public Task ZeroVoltageAsync()
    {
        return Task.CompletedTask;
    }

    public Task ClockIsStartedAsync()
    {
        Logger.LogDebug("ZNServer: Clock started");
        return Task.CompletedTask;
    }

    public Task ClockIsStoppedAsync()
    {
        Logger.LogDebug("ZNServer: Clock stopped");
        return Task.CompletedTask;
    }

    public Task SessionIsCompletedAsync()
    {
        Logger.LogDebug("ZNServer: Session completed");
        return Task.CompletedTask;
    }

    public void Dispose()
    {
        if (IsDisposed) return;
        connectionMonitorTimer?.Dispose();
        BroadcastClient.Dispose();
        IsDisposed = true;
    }
}
