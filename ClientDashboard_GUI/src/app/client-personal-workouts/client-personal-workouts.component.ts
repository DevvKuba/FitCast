import { Component, inject, OnInit, resolveForwardRef, ViewChild} from '@angular/core';
import { TableModule } from 'primeng/table';
import { CommonModule } from '@angular/common';
import { ButtonModule } from 'primeng/button';
import { Workout } from '../models/workout';
import { WorkoutService } from '../services/workout.service';
import { SpinnerComponent } from "../spinner/spinner.component";
import { SelectModule } from 'primeng/select';
import { AccountService } from '../services/account.service';
import { UserDto } from '../models/dtos/user-dto';
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
import { TagModule } from 'primeng/tag';
import { ToggleButton, ToggleButtonModule } from 'primeng/togglebutton';
import { PasswordModule } from 'primeng/password';
import { TrainerService } from '../services/trainer.service';
import { Popover, PopoverModule } from 'primeng/popover';
import { IconFieldModule } from 'primeng/iconfield';
import { InputIconModule } from 'primeng/inputicon';

@Component({
  selector: 'app-client-personal-workouts',
  imports: [TableModule, CommonModule, ButtonModule, SpinnerComponent, Toast, InputTextModule,
       Dialog, FormsModule, AutoCompleteModule, DatePicker, InputNumberModule, TagModule, SelectModule,
       ToggleButtonModule, ToggleButton, PasswordModule, PopoverModule, IconFieldModule, InputIconModule],
  providers: [MessageService],
  templateUrl: './client-personal-workouts.component.html',
  styleUrl: './client-personal-workouts.component.css'
})
export class ClientPersonalWorkoutsComponent implements OnInit{
  workouts: Workout[] | null = null;
  currentUser: UserDto | null = null;

  accountService = inject(AccountService);
  workoutService = inject(WorkoutService);

  first = 0; // offset
  rows = 10; // pageSize

  ngOnInit(): void {
    this.currentUser = this.accountService.currentUser();
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
        // call retrieve client workouts
        this.workoutService.retrieveClientSpecificWorkouts(this.currentUser?.id ?? 0).subscribe({
            next: (response) => {
                this.workouts = response.data ?? [];
            },
            error: (response) => {
                console.log(response.error.message)
            }
        });
    }
}
