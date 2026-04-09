import { Component, signal } from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormsModule } from '@angular/forms';
import { HttpClient } from '@angular/common/http';

interface CreateOrderRequest {
  customerName: string;
  productName:  string;
  quantity:     number;
}

// ── Formulaire de création de commande ────────────────────────────────────────
// Ce composant appelle POST /orders sur l'API .NET OrderProducer.
// L'API publie ensuite le message dans RabbitMQ via MassTransit.
@Component({
  selector:    'app-order-form',
  standalone:  true,
  imports:     [CommonModule, FormsModule],
  templateUrl: './order-form.html',
  styleUrl:    './order-form.css',
})
export class OrderFormComponent {
  form: CreateOrderRequest = { customerName: '', productName: '', quantity: 1 };
  sending  = signal(false);
  feedback = signal('');

  constructor(private http: HttpClient) {}

  submit() {
    if (!this.form.customerName || !this.form.productName) return;

    this.sending.set(true);
    this.feedback.set('');

    // Appel HTTP → OrderProducer → MassTransit → RabbitMQ
    this.http.post('http://localhost:5000/orders', this.form).subscribe({
      next: () => {
        this.feedback.set('✅ Commande envoyée !');
        this.form = { customerName: '', productName: '', quantity: 1 };
        this.sending.set(false);
      },
      error: (err) => {
        this.feedback.set(`❌ Erreur : ${err.message}`);
        this.sending.set(false);
      },
    });
  }
}
