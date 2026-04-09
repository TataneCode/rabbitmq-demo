using RabbitMQ.Client;
using System.Text;
using System.Text.Json;

namespace OrderConsumer;

// ── Interface ─────────────────────────────────────────────────────────────────
// Permet d'injecter IStompPublisher dans le consumer MassTransit.
public interface IStompPublisher
{
    Task PublishAsync<T>(string routingKey, T message);
}

// ── Implémentation ────────────────────────────────────────────────────────────
// Publie un message JSON sur l'exchange "amq.topic" de RabbitMQ.
//
// "amq.topic" est l'exchange topic prédéfini de RabbitMQ.
// Le plugin rabbitmq_web_stomp mappe automatiquement :
//   STOMP destination "/topic/orders"  ←→  amq.topic, routing key "orders"
//
// Ainsi le frontend rx-stomp abonné à "/topic/orders" reçoit nos messages.
public class StompPublisher : IStompPublisher, IAsyncDisposable
{
    private IConnection? _connection;
    private IChannel? _channel;

    public async Task InitAsync(string host, string user, string pass)
    {
        var factory = new ConnectionFactory
        {
            HostName = host,
            UserName = user,
            Password = pass
        };
        _connection = await factory.CreateConnectionAsync();
        _channel    = await _connection.CreateChannelAsync();
    }

    public async Task PublishAsync<T>(string routingKey, T message)
    {
        if (_channel is null) throw new InvalidOperationException("StompPublisher not initialized.");

        var json = JsonSerializer.Serialize(message);
        var body = Encoding.UTF8.GetBytes(json);

        // amq.topic est l'exchange topic prédéfini — pas besoin de le déclarer
        await _channel.BasicPublishAsync(
            exchange:   "amq.topic",
            routingKey: routingKey,
            body:       body
        );
    }

    public async ValueTask DisposeAsync()
    {
        if (_channel    is not null) await _channel.DisposeAsync();
        if (_connection is not null) await _connection.DisposeAsync();
    }
}
