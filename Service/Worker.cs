using System.Device.Gpio;
using System.IO.Ports;
using System.Net;
using System.Reflection;
using System.Runtime.InteropServices;
using System.Text.Json;
using Tellurian.Trains.ClockPulseApp.Service.Extensions;
using Tellurian.Trains.ClockPulseApp.Service.Sinks;
using Tellurian.Trains.MeetingApp.Contracts;

namespace Tellurian.Trains.ClockPulseApp.Service;

public class Worker : BackgroundService
{

    private readonly ILogger<Worker> Logger;
    private readonly PeriodicTimer Timer;
    private readonly PulseGenerator PulseGenerator;

    private readonly bool ResetOnStart;
    private readonly TimeOnly AnalogueClockStartTime;

    public Worker(string[] args, IConfiguration configuration, IHostEnvironment environment, ILogger<Worker> logger)
    {
        Logger = logger;
        var settings = GetSettings(configuration);
        ArgumentNullException.ThrowIfNull(settings);
        var commands = CommandLineArgs(args, settings.AnalogueClockStartTime.AsTimeOnly());

        ResetOnStart = commands.ResetOnStart;
        AnalogueClockStartTime = commands.RestartTime;
        if (ResetOnStart)
        {
            Logger.LogInformation("Using statring analouge time {time}", AnalogueClockStartTime);
        }

        var sinks = new List<IPulseSink>()
        {
            new LoggingSink(Logger)
        };
        if (!settings.SerialPulseSink.Disabled && SerialPort.GetPortNames().Contains(settings.SerialPulseSink.PortName))
        {
            sinks.Add(new SerialPortSink(settings.SerialPulseSink.PortName, Logger));
        }
        if (!settings.UdpBroadcast.Disabled && IPAddress.TryParse(settings.UdpBroadcast.IPAddress, out var iPAddress))
        {
            sinks.Add(new UdpBroadcastSink(new IPEndPoint(iPAddress, settings.UdpBroadcast.PortNumber), logger));
        }
        if (!settings.RpiRelayBoardPulseSink.Disabled && RuntimeInformation.IsOSPlatform(OSPlatform.Linux))
        {
            sinks.Add(new RpiRelayBoardSink(new GpioController(), Enum.Parse<ClockStoppedPinUse>(settings.RpiRelayBoardPulseSink.ClockStoppedPinUse), logger));
        }
        PulseGenerator = new PulseGenerator(settings, sinks, Logger, ResetOnStart, AnalogueClockStartTime);
        Timer = new PeriodicTimer(TimeSpan.FromSeconds(PulseGenerator.PollIntervalSeconds));
    }

    static readonly string[] ResetCommands = ["-r", "-t"];
    private static (bool ResetOnStart, TimeOnly RestartTime) CommandLineArgs(string[] args, TimeOnly defaultStartTime)
    {
        var reset = args.Any(arg => arg.IsAnyOf(ResetCommands));
        var index = Array.IndexOf(args, "-t");
        TimeOnly? startTime = null;
        if (index > -1 && args.Length >= index) { startTime = args[index + 1].AsTimeOnly(); }
        return (reset, startTime ?? defaultStartTime);
    }

    private PulseGeneratorSettings? GetSettings(IConfiguration configuration) =>
        configuration.GetSection(nameof(PulseGeneratorSettings))
             .Get<PulseGeneratorSettings>();

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        Logger.LogInformation("App version {version} started.", Assembly.GetExecutingAssembly().GetName().Version);
        Logger.LogInformation("Settings: {settings}", PulseGenerator);
        Logger.LogInformation("Installed sinks: {sinks}", string.Join(", ", PulseGenerator.InstalledSinksTypes));

        var jsonOptions = new JsonSerializerOptions
        {
            AllowTrailingCommas = true,
            PropertyNameCaseInsensitive = true
        };
        using var client = new HttpClient();
        var href = PulseGenerator.RemoteClockTimeHref;

        var HasError = false;
        while (await Timer.WaitForNextTickAsync(stoppingToken))
        {
            if (stoppingToken.IsCancellationRequested)
            {
                break;
            };
            try
            {
                if (HasError) await Task.Delay(PulseGenerator.ErrorWaitRetryMilliseconds, stoppingToken);
                var response = await client.GetAsync(href, stoppingToken);
                if (response.IsSuccessStatusCode)
                {
                    HasError = false;
                    var json = await response.Content.ReadAsStringAsync(stoppingToken);
                    var status = JsonSerializer.Deserialize<ClockStatus>(json, jsonOptions);
                    Logger.LogInformation("Time requested: server time is {clock}",  status?.Time);
                    if (status is not null) await PulseGenerator.Update(status);
                }
                else
                {
                    HasError = true;
                    Logger.LogError("Responded with code {code}", response.StatusCode.ToString());
                }
            }
            catch (Exception ex)
            {
                HasError = true;
                Logger.LogError("Reason: {message}", ex.Message);
            }
        }
    }

    public override async Task StopAsync(CancellationToken stoppingToken)
    {
        Logger.LogInformation("Stopping service...");
        Timer.Dispose();
        await PulseGenerator.DisposeAsync();
        Logger.LogInformation("Stopped service.");
    }
}
