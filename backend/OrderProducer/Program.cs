using MassTransit;
using Shared;

var builder = WebApplication.CreateBuilder(args);

// ── MassTransit ──────────────────────────────────────────────────────────────
// On configure MassTransit pour utiliser RabbitMQ comme transport.
// MassTransit crée automatiquement les exchanges et queues nécessaires.
builder.Services.AddMassTransit(x =>
{
    x.UsingRabbitMq((ctx, cfg) =>
    {
        cfg.Host("localhost", "/", h =>
        {
            h.Username("guest");
            h.Password("guest");
        });
    });
});

// CORS pour laisser Angular (port 4200) appeler l'API
builder.Services.AddCors(opts =>
    opts.AddDefaultPolicy(p => p.WithOrigins("http://localhost:4200")
                                .AllowAnyHeader()
                                .AllowAnyMethod()));

var app = builder.Build();

app.UseCors();

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
