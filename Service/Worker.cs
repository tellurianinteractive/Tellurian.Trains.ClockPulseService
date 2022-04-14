using Microsoft.Extensions.Options;
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

    public Worker(string[] args, IConfiguration configuration, ILogger<Worker> logger)
    {
        ResetOnStart = args.Contains("-r", StringComparer.OrdinalIgnoreCase);
        Logger = logger;
        var options = GetOptions(configuration);
        var settings = options.Value;
        var sinks = new List<IPulseSink>() { new LoggingPulseSink(Logger) };

        if (!settings.SerialPulseSink.Disabled && SerialPort.GetPortNames().Contains(settings.SerialPulseSink.PortName))
        {
            sinks.Add(new SerialPortPulseSink(settings.SerialPulseSink.PortName, Logger, settings.SerialPulseSink.DtrOnly));
        }
        if (!settings.UdpBroadcast.Disabled && IPAddress.TryParse(settings.UdpBroadcast.IPAddress, out var iPAddress))
        {
            sinks.Add(new UdpBroadcastPulseSink(new IPEndPoint(iPAddress, settings.UdpBroadcast.PortNumber), logger));
        }
        if (RuntimeInformation.IsOSPlatform(OSPlatform.Linux))
        {
            sinks.Add(new RpiRelayBoardPulseSink(logger, settings.RpiRelayBoardPulseSink.UseRelay1AsClockStatus));
        }
        PulseGenerator = new PulseGenerator(options, sinks,  Logger, ResetOnStart);
        Timer = new PeriodicTimer(TimeSpan.FromSeconds(PulseGenerator.PollIntervalSeconds));
    }

    private IOptions<PulseGeneratorSettings> GetOptions(IConfiguration configuration)
    {
        var settings = configuration.GetSection(nameof(PulseGeneratorSettings))
             .Get<PulseGeneratorSettings>();
        return Options.Create(settings);
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        var jsonOptions = new JsonSerializerOptions
        {
            AllowTrailingCommas = true,
            PropertyNameCaseInsensitive = true
        };
        using var client = new HttpClient();
        var href = PulseGenerator.RemoteClockTimeHref;
        Logger.LogInformation("App version {version} started at {time}", Assembly.GetExecutingAssembly().GetName().Version, DateTimeOffset.Now);
        Logger.LogInformation("Settings: {settings}", PulseGenerator);
        Logger.LogInformation("Installed sinks: {sinks}", string.Join(", ", PulseGenerator.InstalledSinksTypes));

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
                    Logger.LogInformation("Time requested at: {time} {clock}", DateTimeOffset.Now, status?.Time);
                    if (status is not null) await PulseGenerator.Update(status);
                }
                else
                {
                    HasError = true;
                    Logger.LogError("Error at: {time}. Responded with code {code}", DateTimeOffset.Now, response.StatusCode);
                }

            }
            catch (Exception ex)
            {
                HasError= true;
                Logger.LogError("Error at: {time}. Responded with code {message}", DateTimeOffset.Now, ex.Message);
            }
        }
    }

    public override async Task StopAsync(CancellationToken cancellationToken)
    {
        Logger.LogInformation("Stopping service...");
        Timer.Dispose();
        await PulseGenerator.DisposeAsync();
        Logger.LogInformation("Stopped service.");
    }
}
