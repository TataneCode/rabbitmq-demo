using MassTransit;
using Shared;

var builder = WebApplication.CreateBuilder(args);

var rabbitHost = builder.Configuration["RabbitMq:Host"] ?? "localhost";

// ── MassTransit ──────────────────────────────────────────────────────────────
// On configure MassTransit pour utiliser RabbitMQ comme transport.
// MassTransit crée automatiquement les exchanges et queues nécessaires.
builder.Services.AddMassTransit(x =>
{
    x.UsingRabbitMq((ctx, cfg) =>
    {
        cfg.Host(rabbitHost, "/", h =>
        {
            h.Username("guest");
            h.Password("guest");
        });
    });
});

var app = builder.Build();

// ── Endpoint POST /orders ────────────────────────────────────────────────────
// Reçoit une commande depuis Angular et la publie dans RabbitMQ via MassTransit.
// IBus.Publish → MassTransit choisit l'exchange en fonction du type du message.
app.MapPost("/orders", async (CreateOrderRequest req, IBus bus) =>
{
    var message = new OrderSubmitted(
        OrderId:      Guid.NewGuid(),
        CustomerName: req.CustomerName,
        ProductName:  req.ProductName,
        Quantity:     req.Quantity,
        SubmittedAt:  DateTime.UtcNow
    );

    // Publish envoie le message à tous les consumers abonnés à OrderSubmitted
    await bus.Publish(message);

    return Results.Accepted($"/orders/{message.OrderId}", new { message.OrderId });
});

app.Run();

// ── DTO de la requête HTTP ────────────────────────────────────────────────────
record CreateOrderRequest(string CustomerName, string ProductName, int Quantity);
