using Microsoft.Extensions.DependencyInjection;
using AzureEventHub.Config.Client;
using Azure.Core;
using Azure.Identity;
using AzureEventHub.Config.Hosting;
using AzureEventHub.Config.Configuration;

namespace AzureEventHub.Config
{
    public static class ServiceCollectionExtension
    {
        public static IServiceCollection AddEventHub(this IServiceCollection services, Action<IEventHubConfigurator> configure)
        {
            var eventHubConfiguration = new EventHubConfiguration();
            var subscription = new EventHubSubscription();
            var builder = new EventHubConfigurator(eventHubConfiguration, subscription);
            configure(builder);

            services.AddSingleton(eventHubConfiguration);
            services.AddSingleton(subscription);
            services.AddSingleton<TokenCredential>(_ => new DefaultAzureCredential());
            services.AddSingleton<EventHubClientFactory>();
            services.AddHostedService<EventHubHostedService>();

            return services;
        }
    }
}
