import { Component, inject} from '@angular/core';
import { TableModule } from 'primeng/table';
import { CommonModule } from '@angular/common';
import { ButtonModule } from 'primeng/button';
import { Workout } from '../models/workout';
import { WorkoutService } from '../services/workout.service';
import { SpinnerComponent } from "../spinner/spinner.component";
import { SelectModule } from 'primeng/select';

@Component({
  selector: 'app-client-workouts',
  imports: [TableModule, CommonModule, ButtonModule, SpinnerComponent],
  templateUrl: './client-workouts.component.html',
  styleUrl: './client-workouts.component.css'
})
export class ClientWorkouts {
  workouts: Workout[] = [];

  private workoutService = inject(WorkoutService);

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
        this.workoutService.retrievePaginatedWorkouts().subscribe({
            next: (response) => {
                this.workouts = response.data ?? [];
            },
            error: (response) => {
                console.log(`Error when loading workouts: ${response.message}`)
            }
        });
    }
}
