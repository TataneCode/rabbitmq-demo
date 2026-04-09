using MassTransit;
using RabbitMQ.Client;
using Shared;
using System.Text;
using System.Text.Json;

namespace OrderConsumer;

// ── IConsumer<T> de MassTransit ───────────────────────────────────────────────
// MassTransit s'occupe de :
//   • créer la queue "order-consumer:OrderSubmitted" dans RabbitMQ
//   • la binder à l'exchange "Shared:OrderSubmitted"
//   • désérialiser le message et appeler Consume()
//
// Après traitement, on republié le message sur amq.topic (routing key: "orders")
// afin que le frontend Angular puisse le recevoir via STOMP WebSocket.
public class OrderSubmittedConsumer(ILogger<OrderSubmittedConsumer> logger, IStompPublisher stomp)
    : IConsumer<OrderSubmitted>
{
    public async Task Consume(ConsumeContext<OrderSubmitted> context)
    {
        var order = context.Message;

        // ① Traitement métier (ici : log)
        logger.LogInformation(
            "[Consumer .NET] Commande reçue — Id:{OrderId} Client:{Customer} Produit:{Product} x{Qty}",
            order.OrderId, order.CustomerName, order.ProductName, order.Quantity);

        // ② Notifier le frontend via STOMP
        await stomp.PublishAsync("orders", order);
    }
}
