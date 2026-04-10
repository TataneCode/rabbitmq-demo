import { Injectable } from '@angular/core';
import { RxStomp, RxStompConfig } from '@stomp/rx-stomp';
import { Observable } from 'rxjs';
import { map } from 'rxjs/operators';

// ── Configuration de la connexion STOMP ───────────────────────────────────────
// RabbitMQ expose un WebSocket STOMP sur le port 15674 (plugin rabbitmq_web_stomp).
// Le chemin /ws est le point d'entrée du WebSocket STOMP.
const stompConfig: RxStompConfig = {
  brokerURL: `ws://${window.location.host}/ws`,
  connectHeaders: {
    login:    'guest',
    passcode: 'guest',
  },
  heartbeatIncoming: 4000,
  heartbeatOutgoing: 4000,
  reconnectDelay: 5000,
};

// ── Service STOMP ─────────────────────────────────────────────────────────────
// Ce service encapsule rx-stomp et expose une méthode watch() typée.
// Il se connecte au démarrage et gère la reconnexion automatiquement.
@Injectable({ providedIn: 'root' })
export class StompService {
  private readonly stomp = new RxStomp();

  constructor() {
    this.stomp.configure(stompConfig);
    this.stomp.activate(); // Démarre la connexion WebSocket STOMP
  }

  // S'abonner à un topic RabbitMQ.
  // "/topic/orders" → amq.topic exchange, routing key "orders"
  watch<T>(destination: string): Observable<T> {
    return this.stomp.watch(destination).pipe(
      map(frame => JSON.parse(frame.body) as T)
    );
  }
}
