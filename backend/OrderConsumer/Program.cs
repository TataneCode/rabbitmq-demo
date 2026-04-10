using MassTransit;
using OrderConsumer;

var builder = Host.CreateApplicationBuilder(args);

var rabbitHost = builder.Configuration["RabbitMq:Host"] ?? "localhost";

// ── StompPublisher (Singleton) ────────────────────────────────────────────────
// On l'enregistre comme singleton car la connexion AMQP est coûteuse.
// L'initialisation async se fait dans un IHostedService dédié.
var stompPublisher = new StompPublisher();
builder.Services.AddSingleton<IStompPublisher>(stompPublisher);

// Service d'initialisation : attend que RabbitMQ soit prêt avant de connecter
builder.Services.AddHostedService(sp =>
    new StompInitService(stompPublisher, rabbitHost, sp.GetRequiredService<ILogger<StompInitService>>()));

// ── MassTransit ───────────────────────────────────────────────────────────────
// AddConsumer<T> enregistre notre consumer.
// MassTransit crée la queue et la lie à l'exchange du type de message.
builder.Services.AddMassTransit(x =>
{
    x.AddConsumer<OrderSubmittedConsumer>();

    x.UsingRabbitMq((ctx, cfg) =>
    {
        cfg.Host(builder.Configuration["RabbitMq:Host"] ?? "localhost", "/", h =>
        {
            h.Username("guest");
            h.Password("guest");
        });

        // Configure les endpoints pour tous les consumers enregistrés
        cfg.ConfigureEndpoints(ctx);
    });
});

var host = builder.Build();
host.Run();

// ── Initialisation différée du StompPublisher ─────────────────────────────────
// On attend que le host soit démarré avant de se connecter à RabbitMQ
// (RabbitMQ doit déjà être disponible à ce stade).
internal class StompInitService(StompPublisher publisher, string rabbitHost, ILogger<StompInitService> logger)
    : IHostedService
{
    public async Task StartAsync(CancellationToken cancellationToken)
    {
        logger.LogInformation("Initialisation de la connexion STOMP publisher...");
        await publisher.InitAsync(rabbitHost, "guest", "guest");
        logger.LogInformation("STOMP publisher connecté.");
    }

    public Task StopAsync(CancellationToken cancellationToken) => Task.CompletedTask;
}
