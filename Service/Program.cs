using Tellurian.Trains.ClockPulseApp.Service;

IHost host = Host.CreateDefaultBuilder(args)
    .ConfigureServices((context, services) =>
    {
        var configuration = context.Configuration;
        var loggerFactory = LoggerFactory.Create(configure =>
        {
            configure.AddSimpleConsole(options =>
            {
                options.IncludeScopes = false;
                options.SingleLine = true;
                options.TimestampFormat = "HH:mm:ss";
            });
        });
        services.AddHostedService<Worker>(provider => new Worker(args, configuration, context.HostingEnvironment, loggerFactory.CreateLogger<Worker>()));
        services.Configure<PulseGeneratorSettings>(
            configuration.GetSection(nameof(PulseGeneratorSettings)));

    })
    .Build();

await host.RunAsync();
