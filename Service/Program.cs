using Tellurian.Trains.ClockPulseApp.Service;

IHost host = Host.CreateDefaultBuilder(args)
    .ConfigureServices((context, services) =>
    {
        services.AddHostedService<Worker>();
        var configurationRoot = context.Configuration;
        services.Configure<PulseGeneratorOptions>(
            configurationRoot.GetSection(nameof(PulseGeneratorOptions)));

    })            
    .Build();

await host.RunAsync();
