import { Component, OnInit, OnDestroy, signal } from '@angular/core';
import { CommonModule } from '@angular/common';
import { StompService } from './stomp.service';
import { Subscription } from 'rxjs';

interface OrderSubmitted {
  orderId:      string;
  customerName: string;
  productName:  string;
  quantity:     number;
  submittedAt:  string;
}

// ── Liste des commandes reçues en temps réel ──────────────────────────────────
// Ce composant s'abonne au topic STOMP "/topic/orders".
// Chaque fois que le consumer .NET republié un message, il apparaît ici
// sans rafraîchissement de page — c'est le temps réel via WebSocket STOMP.
@Component({
  selector:    'app-order-list',
  standalone:  true,
  imports:     [CommonModule],
  templateUrl: './order-list.html',
  styleUrl:    './order-list.css',
})
export class OrderListComponent implements OnInit, OnDestroy {
  orders = signal<OrderSubmitted[]>([]);
  private sub?: Subscription;

  constructor(private stomp: StompService) {}

  ngOnInit() {
    // Souscription au topic — rx-stomp gère la reconnexion automatiquement
    this.sub = this.stomp.watch<OrderSubmitted>('/topic/orders').subscribe(order => {
      // Prepend : on affiche les plus récentes en haut
      this.orders.update(prev => [order, ...prev]);
    });
  }

  ngOnDestroy() {
    this.sub?.unsubscribe();
  }
}
