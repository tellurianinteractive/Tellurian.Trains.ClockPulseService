using Microsoft.Extensions.Options;
using System.IO.Ports;
using System.Net;
using System.Text.Json;
using Tellurian.Trains.MeetingApp.Contracts;

namespace Tellurian.Trains.ClockPulseApp.Service;

public class Worker : BackgroundService
{
    private readonly ILogger<Worker> Logger;
    private readonly PeriodicTimer Timer;
    private readonly PulseGenerator PulseGenerator;

    public Worker(IConfiguration configuration, ILogger<Worker> logger)
    {
        Logger = logger;
        var options = GetOptions(configuration);
        var sinks = new List<IPulseSink>() { new LoggingPulseSink(Logger) };

        if (SerialPort.GetPortNames().Contains(options.Value.SerialPulseSink.PortName))
        {
            sinks.Add(new SerialPortPulseSink(options.Value.SerialPulseSink.PortName, Logger, options.Value.SerialPulseSink.DtrOnly));
        }
        if (IPAddress.TryParse(options.Value.UdpBroadcast.IPAddress, out var iPAddress))
        {
            sinks.Add(new UdpBroadcastPulseSink(new IPEndPoint(iPAddress, options.Value.UdpBroadcast.PortNumber), logger));
        }
        PulseGenerator = new PulseGenerator(options, sinks, Logger);
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
        Logger.LogInformation("Worker started at: {time}", DateTimeOffset.Now);
        Logger.LogInformation("Settings: {settings}", PulseGenerator);
        while (await Timer.WaitForNextTickAsync(stoppingToken))
        {
            if (stoppingToken.IsCancellationRequested) break;

            var response = await client.GetAsync(href, stoppingToken);
            if (response.IsSuccessStatusCode)
            {
                var json = await response.Content.ReadAsStringAsync(stoppingToken);
                var status = JsonSerializer.Deserialize<ClockStatus>(json, jsonOptions);
                Logger.LogInformation("Time requested at: {time} {clock}", DateTimeOffset.Now, status?.Time);
                if (status is not null) await PulseGenerator.Update(status);

            }
            else
            {
                Logger.LogError("Error at: {time}. Responded with code {code}", DateTimeOffset.Now, response.StatusCode);
            }
        }
        Timer.Dispose();
        await PulseGenerator.DisposeAsync();
    }
}
