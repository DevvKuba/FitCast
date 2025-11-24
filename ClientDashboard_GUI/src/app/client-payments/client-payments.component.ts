import { Component, inject, OnInit } from '@angular/core';
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
import { PopoverModule } from 'primeng/popover';
import { Payment } from '../models/payment';
import { Client } from '../models/client';
import { AccountService } from '../services/account.service';
import { ClientWorkouts } from '../client-workouts/client-workouts.component';
import { ClientService } from '../services/client.service';
import { ToastService } from '../services/toast.service';

@Component({
  selector: 'app-client-payments',
  imports: [TableModule, CommonModule, ButtonModule, SpinnerComponent, Toast, InputTextModule,
     Dialog, FormsModule, AutoCompleteModule, DatePicker, InputNumberModule, TagModule, SelectModule,
     ToggleButtonModule, PasswordModule, PopoverModule],
  templateUrl: './client-payments.component.html',
  styleUrl: './client-payments.component.css'
})
export class ClientPaymentsComponent implements OnInit {

  paymentService = inject(PaymentService);
  accountService = inject(AccountService);
  clientService = inject(ClientService);
  toastService = inject(ToastService);

  addDialogVisible: boolean = false;
  deleteDialogVisible: boolean = false;
  clients: {id: number, name: string}[] = [];
  paymentStatuses: {name: string, value: boolean}[] = [];
  currentUserId: number = 0;
  selectedClient: {id: number, name: string} = {id: 0, name: ""};
  selectedStatus: {name: string, value: boolean} | null = null;
  amount: number = 0;
  currency: string = 'GBP';
  numberOfSessions: number = 1;
  paymentDate: Date | null = null;
  deletePaymentId: number = 0;

  payments : Payment[] | null = null;
  clonedPayments: { [s: string]: Payment } = {};

  first = 0; 
  rows = 10;

  ngOnInit(): void {
    this.currentUserId = this.accountService.currentUser()?.id ?? 0;
    this.paymentStatuses = [
        {name: 'Confirmed', value: true},
        {name: 'Pending', value: false}
    ];
    this.gatherAllTrainerPayments();
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

    onRowEditInit(payment: Payment) {
            this.clonedPayments[payment.id as number] = { ...payment };
    }

    onRowEditSave(newPayment: Payment) {

    }

    onRowEditCancel(payment: Payment, index: number) {
        if(this.payments && this.clonedPayments[payment.id as number]){
            this.payments[index] = this.clonedPayments[payment.id as number];
            delete this.clonedPayments[payment.id as number];
        }
    }

    onRowDelete(paymentId: number){
      // method to remove payment including closing the delete dialog
       
    } 

    addNewPayment(selectedClientId: number, paymentAmount: number, numberOfSessions: number, paymentDate : Date, selectedStatus : {name: string, value: boolean} | null){
      // potentially null check the values
      console.log('Selected Status:', selectedStatus);
      console.log('Selected Status Value:', selectedStatus?.value);
      
      var paymentInformation = {
        trainerId: this.currentUserId,
        clientId: selectedClientId,
        amount: paymentAmount,
        numberOfSessions: numberOfSessions,
        paymentDate: this.formatDateForApi(paymentDate),
        confirmed: selectedStatus?.value ?? false
      }
      console.log('Payment Information:', paymentInformation);
      console.log('Confirmed value:', paymentInformation.confirmed);
      
      // take in values then pass then into a payment-add-dto 
      this.paymentService.addTrainerPayment(paymentInformation).subscribe({
        next: (response) => {
          this.toastService.showSuccess('Success Adding Payment', response.message)
          this.addDialogVisible = false;
          this.gatherAllTrainerPayments();
          this.resetForm();
        },
        error: (response) => {
          this.toastService.showError('Error Adding Payment', response.message)
        }
      })
      // including closing dialog if everything is successful
    }

    resetForm() {
      this.selectedClient = {id: 0, name: ""};
      this.selectedStatus = null;
      this.amount = 0;
      this.numberOfSessions = 1;
      this.paymentDate = null;
    }

    showDialogForAdd() {
      this.addDialogVisible = true;
    }

    showDialogForDelete(paymentId: number){
      // can use payment id to set a payment id variable then use 
      this.deleteDialogVisible = true;
      this.deletePaymentId = paymentId;
      // within on RowDelete to delete payment with that id
    }

    gatherAllTrainerPayments(){
      this.paymentService.getTrainerPayments(this.currentUserId).subscribe({
        next: (response) => {
          this.payments = response.data;
        },
        error: (response) => {
          console.log(response.message)
        }
      })
    }

    gatherClientNames(){
    this.clientService.gatherClientNames(this.currentUserId).subscribe({
        next: (response) => {
            this.clients = response
        }
    });
    }

    gatherStatuses(){
    this.paymentStatuses = [
        {name: 'Confirmed', value: true},
        {name: 'Pending', value: false}
    ];
  }

   getActivities(isConfirmed : boolean) : string {
    return isConfirmed ? 'success' : 'warning';
  }

  getActivityLabel(isConfirmed: boolean) : string {
    return isConfirmed ? 'Confirmed' : 'Pending';
  }

  formatDateForApi(date: Date | undefined): string {
  if (!date) return '';
  
  const day = date.getDate().toString().padStart(2, '0');
  const month = (date.getMonth() + 1).toString().padStart(2, '0');
  const year = date.getFullYear();
  
  return `${day}/${month}/${year}`;
    }

}
