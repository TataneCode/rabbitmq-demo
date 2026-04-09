namespace Shared;

// Le contrat partagé entre producer et consumer.
// C'est le "message" qui transite dans RabbitMQ via MassTransit.
public record OrderSubmitted(
    Guid   OrderId,
    string CustomerName,
    string ProductName,
    int    Quantity,
    DateTime SubmittedAt
);
