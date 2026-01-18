using AzureEventHub.Config;
using ConsumerApp.Handlers;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

internal class Program
{
    private static async Task Main(string[] args)
    {
        var host = Host.CreateDefaultBuilder(args)
            .ConfigureAppConfiguration((context, config) =>
            {
                // DO NOT clear config.Sources
                config.AddJsonFile("appsettings.json", optional: false, reloadOnChange: true);
                config.AddJsonFile(
                    $"appsettings.{context.HostingEnvironment.EnvironmentName}.json",
                    optional: true);

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
        
        services.AddEventHub(k =>
        {
            k.Namespace(config["EventHubRider:Namespace"]);
            k.Storage(config["EventHubRider:StorageAccount"]);
            k.BlobContainer(config["EventHubRider:BlobContainer"]);
            k.ReceiveEndpoint("customer.created", "sampleapp", c =>
            {
                c.ConfigureConsumer<CustomerEventHandler>(context);
            });
            k.ReceiveEndpoint("invoice.created", "sampleapp", c =>
            {
                c.ConfigureConsumer<InvoiceEventHandler>(context);
            });
        });        
    })
    .ConfigureLogging(logging =>
    {
        logging.ClearProviders();
        logging.AddConsole();
    })
    .Build();

        await host.RunAsync();
    }
}