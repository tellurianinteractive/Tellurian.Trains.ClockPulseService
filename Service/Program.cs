using Tellurian.Trains.ClockPulseApp.Service;

IHost host = Host.CreateDefaultBuilder(args)
    .ConfigureServices((context, services) =>
    {
        var configuration = context.Configuration;
        var loggerFactory = LoggerFactory.Create(configure => {
            configure.ClearProviders();
            configure.AddConsole();
        });
        services.AddHostedService<Worker>(provider => new Worker(args, configuration, loggerFactory.CreateLogger<Worker>()));
        services.Configure<PulseGeneratorSettings>(
            configuration.GetSection(nameof(PulseGeneratorSettings)));

    })            
    .Build();

await host.RunAsync();
