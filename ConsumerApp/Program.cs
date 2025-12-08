using Azure.Messaging.ServiceBus;
using Azure.Storage.Blobs;
using ConsumerApp.Application;
using ConsumerApp.Application.Abstractions;
using ConsumerApp.Infrastructure;
using ConsumerApp.Presentation;
using Microsoft.ApplicationInsights.DependencyCollector;
using Microsoft.ApplicationInsights.Extensibility;
using Microsoft.ApplicationInsights.WorkerService;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Diagnostics.HealthChecks;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using StackExchange.Redis;
using HealthChecks.Redis; 

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
        var config = context.Configuration;
        // Optional Application Insights
        // Optional: Application Insights can be added by referencing Microsoft.ApplicationInsights.WorkerService and calling AddApplicationInsightsTelemetryWorkerService

        // Blob container client for checkpoint store
        var blobConn = config["BlobStorage:ConnectionString"];
        var blobContainerName = config["BlobStorage:ContainerName"];
        var blobContainerClient = new BlobContainerClient(blobConn, blobContainerName);
        blobContainerClient.CreateIfNotExists();

        services.AddSingleton(blobContainerClient);

        // ServiceBus client for DLQ
        services.AddSingleton(sp =>
        {
            var sbConn = config["ServiceBus:ConnectionString"];
            return new ServiceBusClient(sbConn);
        });


        // Redis
        services.AddSingleton<IConnectionMultiplexer>(sp =>
        {
            var redisConn = config["Redis:ConnectionString"];
            return ConnectionMultiplexer.Connect(redisConn);
        });


        // Add our services
        services.AddSingleton<IIdempotencyService, IdempotencyService>();
        services.AddSingleton<IDlqService, DlqService>();
        services.AddSingleton<IBusinessProcess, BusinessProcessor>();

        // Hosted Processor Service
        services.AddHostedService<EventHubProcessorService>();

        // Health Checks
        services.AddHealthChecks()
            .AddCheck("self", () => HealthCheckResult.Healthy())
            .AddRedis(config["Redis:ConnectionString"], name: "redis"); // This will now resolve

        // Optional: Application Insights
        // Replace obsolete InstrumentationKey usage with ConnectionString as per Application Insights guidance
        if (!string.IsNullOrEmpty(config["ApplicationInsights:ConnectionString"]))
        {
            services.AddApplicationInsightsTelemetryWorkerService(options =>
            {
                options.ConnectionString = config["ApplicationInsights:ConnectionString"];
            });
        }
    })
    .ConfigureLogging(logging =>
    {
        logging.ClearProviders();
        logging.AddConsole();
    })
    .Build();

await host.RunAsync();