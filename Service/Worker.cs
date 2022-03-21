using Microsoft.Extensions.Options;
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
        PulseGenerator = new PulseGenerator(options, new[] { new LoggingPulseSink(Logger) }, Logger);
        Timer = new PeriodicTimer(TimeSpan.FromSeconds(PulseGenerator.PollIntervalSeconds));
    }

    private IOptions<PulseGeneratorOptions> GetOptions(IConfiguration configuration)
    {
        var settings = configuration.GetSection(nameof(PulseGeneratorOptions))
             .Get<PulseGeneratorOptions>();
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
        while (await Timer.WaitForNextTickAsync(stoppingToken))
        {
            var response = await client.GetAsync(href, stoppingToken);
            if (response.IsSuccessStatusCode)
            {
                var json = await response.Content.ReadAsStringAsync(stoppingToken);
                var status = JsonSerializer.Deserialize<ClockStatus>(json, jsonOptions);
                if (status is not null) await PulseGenerator.Update(status);
                Logger.LogInformation("Time requested at: {time} {clock}", DateTimeOffset.Now, status?.Time);

            }
            else
            {
                Logger.LogError("Error at: {time}. Responded with code {code}", DateTimeOffset.Now, response.StatusCode);
            }
        }
    }
}
