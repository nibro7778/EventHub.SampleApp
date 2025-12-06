using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using ConsumerApp.Presentation;
using ConsumerApp.Application;
using ConsumerApp.Infrastructure;

var host = Host.CreateDefaultBuilder(args)
    .ConfigureAppConfiguration((context, config) =>
    {
        config.AddJsonFile("appsettings.json", optional: true, reloadOnChange: true);
        config.AddEnvironmentVariables();
    })
    .ConfigureLogging(logging =>
    {
        logging.ClearProviders();
        logging.AddConsole();
    })
    .ConfigureServices((context, services) =>
    {
        // Application services
        services.AddSingleton<IMessageProcessor, MessageProcessor>();
        services.AddSingleton<IPoisonMessageSink, PoisonMessageSink>();
        services.AddSingleton<IMetricsExporter, MetricsExporter>();

        // Presentation worker
        services.AddHostedService<KafkaConsumerService>();
    })
    .UseConsoleLifetime()
    .Build();

await host.RunAsync();
// Moved implementation to Clean Architecture folders.
