import { Component, inject} from '@angular/core';
import { TableModule } from 'primeng/table';
import { CommonModule } from '@angular/common';
import { ButtonModule } from 'primeng/button';
import { Workout } from '../models/workout';
import { WorkoutService } from '../services/workout.service';
import { SpinnerComponent } from "../spinner/spinner.component";
import { SelectModule } from 'primeng/select';
import { AccountService } from '../services/account.service';
import { UserDto } from '../models/user-dto';
import { Toast } from 'primeng/toast';
import { InputTextModule } from 'primeng/inputtext';
import { Dialog } from 'primeng/dialog';
import { FormsModule } from '@angular/forms';
import { AutoCompleteCompleteEvent, AutoCompleteModule } from 'primeng/autocomplete';
import { Client } from '../models/client';
import { ClientService } from '../services/client.service';
import { DatePicker } from 'primeng/datepicker';
import { InputNumberModule } from 'primeng/inputnumber';
import { MessageService } from 'primeng/api';
import { ToastService } from '../services/toast.service';

@Component({
  selector: 'app-client-workouts',
  imports: [TableModule, CommonModule, ButtonModule, SpinnerComponent, Toast, InputTextModule, Dialog, FormsModule, AutoCompleteModule, DatePicker, InputNumberModule ],
  templateUrl: './client-workouts.component.html',
  providers: [MessageService],
  styleUrl: './client-workouts.component.css'
})
export class ClientWorkouts {
    workouts: Workout[] | null = null;
    trainerId : number  = 0;
    visible: boolean = false;

    selectedClient :{id: number, name: string} = {id: 0, name: ""};
    workoutTitle: string = "";
    sessionDate: Date  = new Date();
    exerciseCount: number = 0;
    clients: {id: number, name: string}[] = [];

    private workoutService = inject(WorkoutService);
    private accountService = inject(AccountService);
    private clientService = inject(ClientService);
    private toastService = inject(ToastService);

    first = 0; // offset
    rows = 10; // pageSize

    ngOnInit() {
        this.displayWorkouts();
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
        return this.workouts ? this.first + this.rows >= this.workouts.length : true;
    }

    isFirstPage(): boolean {
        return this.workouts ? this.first === 0 : true;
    }

    displayWorkouts(){
        this.trainerId = this.accountService.currentUser()?.id ?? 0;
        console.log(this.trainerId);
        this.workoutService.retrieveTrainerClientWorkouts(this.trainerId).subscribe({
            next: (response) => {
                this.workouts = response.data ?? [];
            },
            error: (response) => {
                console.log(response.message)
            }
        });
    }

    addNewWorkout(selectedClient : {id: number, name: string}, workoutTitle: string, sessionDate : Date | undefined, exerciseCount: number){
        var newWorkout = {
            workoutTitle: workoutTitle,
            clientName: selectedClient.name,
            clientId: selectedClient.id,
            sessionDate: this.formatDateForApi(sessionDate),
            exerciseCount: exerciseCount
        }

        this.workoutService.addWorkout(newWorkout).subscribe({
            next: (response) => {
                this.visible = false;
                this.toastService.showSuccess('Successfully added workout', response.message);
                this.displayWorkouts();
            },
            error: (response) => {
                this.toastService.showError('Workout not added', response.message);
            }
        })
    }

    gatherClientNames(){
    this.trainerId = this.accountService.currentUser()?.id ?? 0;
    this.clientService.getAllTrainerClients(this.trainerId).subscribe({
      next: (response) => {
        this.clients = response.data?.map(x => ({id: x.id , name: x.name})) ?? [];
      },
      error: (response) => {
        console.log('Failed to display client for which you may add a workout for');
        this.clients = [];
      }
    })
  }

    showDialogForAdd() {
        this.visible = true;
    }

  formatDateForApi(date: Date | undefined): string {
  if (!date) return '';
  
  const day = date.getDate().toString().padStart(2, '0');
  const month = (date.getMonth() + 1).toString().padStart(2, '0');
  const year = date.getFullYear();
  
  return `${day}/${month}/${year}`;
    }
}
