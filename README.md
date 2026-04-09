# 🐇 RabbitMQ Demo — .NET + Angular + MassTransit + STOMP

Exemple pédagogique du flux Producer / Consumer complet.

## Architecture

```
[Angular Form]
    │  POST /orders  (HTTP)
    ▼
[OrderProducer]  ──MassTransit 8.x──▶  RabbitMQ  ──exchange──▶  [OrderConsumer]
  .NET 10 API                                                      .NET 10 Worker
                                                                        │
                                                            RabbitMQ.Client publish
                                                                        │
                                                               amq.topic / "orders"
                                                                        │  STOMP WS
                                                                        ▼
                                                              [Angular rx-stomp]
                                                          liste temps réel dans le browser
```

## Prérequis

- Docker + Docker Compose
- .NET 10 SDK
- Node 20+ / npm

## Lancer le projet

### 1. RabbitMQ

```bash
docker compose up -d
```

- Management UI : http://localhost:15672 (guest / guest)
- AMQP : `localhost:5672`
- STOMP WebSocket : `ws://localhost:15674/ws`

### 2. OrderConsumer (Worker .NET)

```bash
cd backend/OrderConsumer
dotnet run
```

### 3. OrderProducer (Web API .NET)

```bash
cd backend/OrderProducer
dotnet run
```

API disponible sur `http://localhost:5000`

### 4. Frontend Angular

```bash
cd frontend/order-app
npm start          # ou : npx @angular/cli@21 serve
```

App sur `http://localhost:4200`

---

## Explication des briques

### Shared — Contrats de messages

```csharp
record OrderSubmitted(Guid OrderId, string CustomerName,
                      string ProductName, int Quantity, DateTime SubmittedAt);
```

Le **contrat est partagé** entre producer et consumer via un projet `Shared`.
En production on utilise un package NuGet interne ou un fichier `.proto`.

---

### OrderProducer — `IBus.Publish`

```csharp
await bus.Publish(new OrderSubmitted(...));
```

- `IBus` est injecté par MassTransit via DI
- `Publish` : MassTransit crée automatiquement un **exchange fanout** nommé d'après le type  
  (ex: `Shared:OrderSubmitted`) et y publie le message sérialisé en JSON
- Tous les consumers abonnés à ce type reçoivent le message (**pub/sub**)

> **Publish vs Send** :  
> `Publish` = fanout vers tous les consumers du type (événement)  
> `Send` = livraison directe à une queue précise (commande)

---

### OrderConsumer — `IConsumer<T>`

```csharp
public class OrderSubmittedConsumer : IConsumer<OrderSubmitted>
{
    public async Task Consume(ConsumeContext<OrderSubmitted> context) { ... }
}
```

MassTransit :
1. Crée une **queue** nommée `order-consumer:OrderSubmitted`
2. La **bind** à l'exchange `Shared:OrderSubmitted`
3. Désérialise le message et appelle `Consume()`
4. **Acknowledge** automatiquement si pas d'exception (ou NACK + retry sinon)

Après traitement, on publie sur `amq.topic` via `RabbitMQ.Client` :

```csharp
await channel.BasicPublishAsync(exchange: "amq.topic", routingKey: "orders", body: json);
```

---

### Frontend — rx-stomp

```typescript
// Connexion WebSocket STOMP (plugin rabbitmq_web_stomp)
const stomp = new RxStomp();
stomp.configure({ brokerURL: 'ws://localhost:15674/ws', ... });
stomp.activate();

// Souscription au topic "orders"
stomp.watch('/topic/orders').subscribe(frame => {
  const order = JSON.parse(frame.body);
});
```

**Mapping STOMP ↔ RabbitMQ :**

| Destination STOMP | Exchange RabbitMQ | Routing key |
|---|---|---|
| `/topic/orders` | `amq.topic` | `orders` |
| `/queue/ma-queue` | `""` (default) | `ma-queue` |
| `/exchange/mon-exchange/cle` | `mon-exchange` | `cle` |

Le plugin `rabbitmq_web_stomp` fait cette traduction automatiquement.

---

## Structure du projet

```
rabbitmq-demo/
├── docker-compose.yml
├── rabbitmq/
│   └── enabled_plugins          ← active stomp + web_stomp
├── backend/
│   ├── Shared/                  ← record OrderSubmitted
│   ├── OrderProducer/           ← POST /orders → MassTransit → RabbitMQ
│   └── OrderConsumer/           ← MassTransit consumer → STOMP publish
└── frontend/
    └── order-app/               ← Angular 21, rx-stomp
```

## Dépendances clés

| Package | Version | Rôle |
|---|---|---|
| `MassTransit.RabbitMQ` | 8.5.x | Transport + consumer (MIT) |
| `RabbitMQ.Client` | 7.x | Publier vers amq.topic pour le STOMP |
| `@stomp/rx-stomp` | 2.x | Client STOMP WebSocket réactif (Angular) |
