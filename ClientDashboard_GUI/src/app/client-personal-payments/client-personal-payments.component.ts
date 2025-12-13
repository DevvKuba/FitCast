import { Component, inject, OnInit, ViewChild } from '@angular/core';
import { PaymentService } from '../services/payment.service';
import { TableModule } from 'primeng/table';
import { CommonModule } from '@angular/common';
import { ButtonModule } from 'primeng/button';
import { SpinnerComponent } from '../spinner/spinner.component';
import { Toast } from 'primeng/toast';
import { InputTextModule } from 'primeng/inputtext';
import { Dialog } from 'primeng/dialog';
import { FormsModule } from '@angular/forms';
import { AutoCompleteModule } from 'primeng/autocomplete';
import { DatePicker } from 'primeng/datepicker';
import { InputNumberModule } from 'primeng/inputnumber';
import { TagModule } from 'primeng/tag';
import { SelectModule } from 'primeng/select';
import { ToggleButtonModule } from 'primeng/togglebutton';
import { PasswordModule } from 'primeng/password';
import { Popover, PopoverModule } from 'primeng/popover';
import { Payment } from '../models/payment';
import { Client } from '../models/client';
import { AccountService } from '../services/account.service';
import { ClientWorkouts } from '../client-workouts/client-workouts.component';
import { ClientService } from '../services/client.service';
import { ToastService } from '../services/toast.service';
import { ClientNamePipe } from '../pipes/client-name.pipe';
import { IconFieldModule } from 'primeng/iconfield';
import { InputIconModule } from 'primeng/inputicon';
import { TrainerService } from '../services/trainer.service';
import { UserDto } from '../models/dtos/user-dto';

@Component({
  selector: 'app-client-personal-payments',
  imports: [TableModule, CommonModule, ButtonModule, SpinnerComponent, Toast, InputTextModule,
       Dialog, FormsModule, AutoCompleteModule, DatePicker, InputNumberModule, TagModule, SelectModule,
       ToggleButtonModule, PasswordModule, PopoverModule, IconFieldModule, InputIconModule],
  templateUrl: './client-personal-payments.component.html',
  styleUrl: './client-personal-payments.component.css'
})
export class ClientPersonalPaymentsComponent implements OnInit{

  payments : Payment[] | null = null;
  paymentStatuses: {label: string, value: boolean}[] = [];
  currentUser: UserDto | null = null;

  accountService = inject(AccountService);
  paymentService = inject(PaymentService);

  first = 0; 
  rows = 10;

  ngOnInit(): void {
    this.currentUser = this.accountService.currentUser();
    this.paymentStatuses = [
        {label: 'Confirmed', value: true},
        {label: 'Pending', value: false}
    ];
  }

  next() {
        this.first = this.first + this.rows;
    }

  prev() {
      this.first = this.first - this.rows;
  }

  reset() {
      this.first = 0;
  }

  pageChange(event: { first: number; rows: number; }) {
      this.first = event.first;
      this.rows = event.rows;
  }

  isLastPage(): boolean {
      return this.payments ? this.first + this.rows >= this.payments.length : true;
  }

  isFirstPage(): boolean {
      return this.payments ? this.first === 0 : true;
  }

  getActivities(isConfirmed : boolean) : string {
    return isConfirmed ? 'success' : 'info';
  }

  getActivityLabel(isConfirmed: boolean) : string {
    return isConfirmed ? 'Confirmed' : 'Pending';
  }

}
