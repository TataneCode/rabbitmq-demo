import { Component } from '@angular/core';
import { OrderFormComponent } from './order-form';
import { OrderListComponent } from './order-list';

@Component({
  selector:    'app-root',
  standalone:  true,
  imports:     [OrderFormComponent, OrderListComponent],
  templateUrl: './app.html',
  styleUrl:    './app.css',
})
export class App {}
