using Microsoft.Extensions.Hosting;
using System.Linq;
using AzureEventHub.Config;

namespace AzureEventHub.Config.Configuration
{
    public interface IEventHubConfigurator
    {
        IEventHubConfigurator Namespace(string fullyQualifiedNamespace);
        IEventHubConfigurator Storage(string storageAccountNameOrUrl);
        IEventHubConfigurator BlobContainer(string container);
        IEventHubConfigurator ReceiveEndpoint(string eventHubName, Action<IReceiveEndpointConfigurator> configure);
        IEventHubConfigurator ReceiveEndpoint(string eventHubName, string consumerGroup, Action<IReceiveEndpointConfigurator> configure);
    }

    public interface IReceiveEndpointConfigurator
    {
        void ConfigureConsumer<THandler>(HostBuilderContext context);
    }

    public class EventHubConfigurator(EventHubConfiguration configuration, EventHubSubscription subscription) : IEventHubConfigurator
    {
        private readonly EventHubSubscription _subscription = subscription;
        
        public IEventHubConfigurator Namespace(string fullyQualifiedNamespace)
        {
            configuration.Namespace = fullyQualifiedNamespace;
            return this;
        }

        public IEventHubConfigurator Storage(string storageAccountNameOrUrl)
        {
            configuration.StorageAccount = storageAccountNameOrUrl;
            return this;
        }

        public IEventHubConfigurator BlobContainer(string container)
        {
            configuration.BlobContainer = container;
            return this;
        }

        public IEventHubConfigurator ReceiveEndpoint(string eventHubName, Action<IReceiveEndpointConfigurator> configure)
            => ReceiveEndpoint(eventHubName, "$Default", configure);

        public IEventHubConfigurator ReceiveEndpoint(string eventHubName, string consumerGroup, Action<IReceiveEndpointConfigurator> configure)
        {
            var reg = _subscription.EventHubs
                .FirstOrDefault(eh => eh.EventHubName == eventHubName && eh.ConsumerGroup == consumerGroup);
            if (reg == null)
            {
                reg = new EventHub
                {
                    EventHubName = eventHubName,
                    ConsumerGroup = consumerGroup
                };
                _subscription.EventHubs.Add(reg);
            }

            var endpointConfigurator = new ReceiveEndpointConfigurator(reg);
            configure(endpointConfigurator);

            return this;
        }

        private sealed class ReceiveEndpointConfigurator : IReceiveEndpointConfigurator
        {
            private readonly EventHub _eventHub;

            public ReceiveEndpointConfigurator(EventHub reg)
            {
                _eventHub = reg;
            }

            public void ConfigureConsumer<THandler>(HostBuilderContext context)
            {
                // Infer TEvent from IEventHandler<TEvent> implemented by THandler
                var handlerType = typeof(THandler);
                var eventInterface = handlerType
                    .GetInterfaces()
                    .FirstOrDefault(i => i.IsGenericType && i.GetGenericTypeDefinition() == typeof(IEventHandler<>));
                if (eventInterface == null)
                {
                    throw new InvalidOperationException($"Handler type {handlerType.FullName} must implement IEventHandler<TEvent>.");
                }

                var eventType = eventInterface.GetGenericArguments()[0];
                _eventHub.EventType = eventType;
                _eventHub.HandlerType = handlerType;
            }
        }
    }
}
