namespace Tellurian.Trains.ClockPulseApp.Service;

public class Worker : BackgroundService
{
    private readonly ILogger<Worker> Logger;
    private readonly IConfiguration Configuration;
    private readonly PeriodicTimer Timer;

    public Worker(IConfiguration configuration, ILogger<Worker> logger)
    {
        Configuration = configuration;
        Logger = logger;
        var interval = Configuration.GetValue<int?>("App:TimeInterval") ?? 2;
        Timer = new PeriodicTimer(TimeSpan.FromSeconds(interval));
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        using var client = new HttpClient();
        var url = Configuration.GetValue<string>("App:ClockTimeApiUrl") ?? "https://fastclock.azurewebsites.net/api/clocks/demo/time";
        Logger.LogInformation("Worker started at: {time}", DateTimeOffset.Now);
        while (await Timer.WaitForNextTickAsync(stoppingToken))
        {
            var response = await client.GetAsync(url, stoppingToken);
            if (response.IsSuccessStatusCode)
            {
                Logger.LogInformation("Time requested at: {time}", DateTimeOffset.Now);

            }
            else
            {
                Logger.LogError("Error at: {time}. Responded with code {code}", DateTimeOffset.Now, response.StatusCode);
            }
        }
    }
}
