using Tellurian.Trains.ClockPulseApp.Service;

IHost host = Host.CreateDefaultBuilder(args)
    .ConfigureServices((context, services) =>
    {
        services.AddHostedService<Worker>();
        var configurationRoot = context.Configuration;
        services.Configure<PulseGeneratorSettings>(
            configurationRoot.GetSection(nameof(PulseGeneratorSettings)));

    })            
    .Build();

await host.RunAsync();
