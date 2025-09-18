import { Component, OnInit } from '@angular/core';
// import { Customer } from '@/domain/customer';
// import { CustomerService } from '@/service/customerservice';
import { TableModule } from 'primeng/table';
import { CommonModule } from '@angular/common';
import { ButtonModule } from 'primeng/button';

@Component({
  selector: 'app-client-workouts',
  imports: [TableModule, CommonModule, ButtonModule],
  templateUrl: './client-workouts.html',
  styleUrl: './client-workouts.css'
})
export class ClientWorkouts {
  // customers!: Customer[];

  //   first = 0;

  //   rows = 10;

  //   constructor(private customerService: CustomerService) {}

  //   ngOnInit() {
  //       this.customerService.getCustomersLarge().then((customers) => (this.customers = customers));
  //   }

  //   next() {
  //       this.first = this.first + this.rows;
  //   }

  //   prev() {
  //       this.first = this.first - this.rows;
  //   }

  //   reset() {
  //       this.first = 0;
  //   }

  //   pageChange(event: { first: number; rows: number; }) {
  //       this.first = event.first;
  //       this.rows = event.rows;
  //   }

  //   isLastPage(): boolean {
  //       return this.customers ? this.first + this.rows >= this.customers.length : true;
  //   }

  //   isFirstPage(): boolean {
  //       return this.customers ? this.first === 0 : true;
  //   }
}
