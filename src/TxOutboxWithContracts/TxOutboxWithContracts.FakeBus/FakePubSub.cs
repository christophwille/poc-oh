using Microsoft.Extensions.Logging;

namespace TxOutboxWithContracts.FakeBus
{
    public interface IPubSub
    {
        // Similar to https://github.com/EasyNetQ/EasyNetQ/blob/master/Source/EasyNetQ/IPubSub.cs and
        // https://github.com/EasyNetQ/EasyNetQ/blob/master/Source/EasyNetQ/PubSubExtensions.cs respectively
        Task PublishAsync<T>(
            T message,
            CancellationToken cancellationToken = default
        );
    }

    public class FakePubSub : IPubSub
    {
        private readonly ILogger<FakePubSub> _logger;

        public FakePubSub(ILogger<FakePubSub> logger)
        {
            _logger = logger;
        }

        public Task PublishAsync<T>(T message, CancellationToken cancellationToken = default)
        {
            _logger.LogInformation("Publishing message of type {MessageType}: {@Message}", typeof(T).Name, message);
            return Task.CompletedTask;
        }
    }
}
