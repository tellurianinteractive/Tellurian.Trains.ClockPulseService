using System.IO.Ports;
using System.Net;
using System.Reflection;
using System.Runtime.InteropServices;
using System.Text.Json;
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

        var sinks = new List<IPulseSink>()
        {
            new LoggingPulseSink(Logger)
        };
        if (!settings.SerialPulseSink.Disabled && SerialPort.GetPortNames().Contains(settings.SerialPulseSink.PortName))
        {
            sinks.Add(new SerialPortPulseSink(settings.SerialPulseSink.PortName, Logger, settings.SerialPulseSink.DtrOnly));
        }
        if (!settings.UdpBroadcast.Disabled && IPAddress.TryParse(settings.UdpBroadcast.IPAddress, out var iPAddress))
        {
            sinks.Add(new UdpBroadcastPulseSink(new IPEndPoint(iPAddress, settings.UdpBroadcast.PortNumber), logger));
        }
        if (!settings.RpiRelayBoardPulseSink.Disabled && RuntimeInformation.IsOSPlatform(OSPlatform.Linux))
        {
            sinks.Add(new RpiRelayBoardPulseSink(logger));
        }
        if (environment.IsDevelopment())
        {
            sinks.Add(new AnalogueClockSimulationPulseSink(AnalogueClockStartTime, logger));
        }
        PulseGenerator = new PulseGenerator(settings, sinks, Logger, ResetOnStart, AnalogueClockStartTime);
        Timer = new PeriodicTimer(TimeSpan.FromSeconds(PulseGenerator.PollIntervalSeconds));
    }

    private static (bool ResetOnStart, TimeOnly RestartTime) CommandLineArgs(string[] args, TimeOnly defaultStartTime)
    {
        var reset = args.Contains("-r", StringComparer.OrdinalIgnoreCase);
        var index = Array.IndexOf(args, "-t");
        TimeOnly? startTime = null;
        if (index > -1) { startTime = args[index + 1].AsTimeOnly(); }
        return (reset, startTime ?? defaultStartTime);
    }

    private PulseGeneratorSettings? GetSettings(IConfiguration configuration) =>
        configuration.GetSection(nameof(PulseGeneratorSettings))
             .Get<PulseGeneratorSettings>();

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        Logger.LogInformation("App version {version} started at {time}", Assembly.GetExecutingAssembly().GetName().Version, DateTimeOffset.Now);
        Logger.LogInformation("Settings: {settings}", PulseGenerator);
        Logger.LogInformation("Installed sinks: {sinks}", string.Join(", ", PulseGenerator.InstalledSinksTypes));
        //return Task.CompletedTask;
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
            if (stoppingToken.IsCancellationRequested) break;
            try
            {
                if (HasError) await Task.Delay(PulseGenerator.ErrorWaitRetryMilliseconds, stoppingToken);
                var response = await client.GetAsync(href, stoppingToken);
                if (response.IsSuccessStatusCode)
                {
                    HasError = false;
                    var json = await response.Content.ReadAsStringAsync(stoppingToken);
                    var status = JsonSerializer.Deserialize<ClockStatus>(json, jsonOptions);
                    Logger.LogInformation("Time requested at: {time} with clock time {clock}", DateTimeOffset.Now, status?.Time);
                    if (status is not null) await PulseGenerator.Update(status);
                }
                else
                {
                    HasError = true;
                    Logger.LogError("Error at: {time}. Responded with code {code}", DateTimeOffset.Now, response.StatusCode.ToString());
                }
            }
            catch (Exception ex)
            {
                HasError = true;
                Logger.LogError("Error at: {time}. Reason: {message}", DateTimeOffset.Now, ex.Message);
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
