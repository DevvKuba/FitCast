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

@Component({
  selector: 'app-client-workouts',
  imports: [TableModule, CommonModule, ButtonModule, SpinnerComponent, Toast, InputTextModule, Dialog, FormsModule, AutoCompleteModule, DatePicker ],
  templateUrl: './client-workouts.component.html',
  styleUrl: './client-workouts.component.css'
})
export class ClientWorkouts {
    workouts: Workout[] | null = null;
    trainerId : number  = 0;
    visible: boolean = false;

    clientName : string = "";
    workoutTitle: string = "";
    date: Date | undefined;
    exerciseCount: number = 0;
    clients: any[] = [];

    private workoutService = inject(WorkoutService);
    private accountService = inject(AccountService);
    private clientService = inject(ClientService);

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

    gatherClientNames(){
    this.trainerId = this.accountService.currentUser()?.id ?? 0;
    this.clientService.getAllTrainerClients(this.trainerId).subscribe({
      next: (response) => {
        this.clients = response.data?.map(x => x.name) ?? [];
      }
    })
  }

    showDialogForAdd() {
        this.visible = true;
    }
}
