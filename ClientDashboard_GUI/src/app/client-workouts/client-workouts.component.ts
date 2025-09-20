import { Component, inject, OnInit } from '@angular/core';
import { TableModule } from 'primeng/table';
import { CommonModule } from '@angular/common';
import { ButtonModule } from 'primeng/button';
import { Workout } from '../models/workout';
import { WorkoutService } from '../services/workout.service';

@Component({
  selector: 'app-client-workouts',
  imports: [TableModule, CommonModule, ButtonModule],
  templateUrl: './client-workouts.component.html',
  styleUrl: './client-workouts.component.css'
})
export class ClientWorkouts {
  workouts: Workout[] = [];
  private workoutService = inject(WorkoutService);

    first = 0; // offset
    rows = 10; // pageSize

    ngOnInit() {
        this.displayWorkouts(this.first, this.rows);
    }

    // apart from the initial initialisation of paginated workouts
    // add further method that can filter the results accordingly *potentially

    next() {
        this.first = this.first + this.rows;
        this.displayWorkouts(this.first, this.rows);
    }

    prev() {
        this.first = this.first - this.rows;
        this.displayWorkouts(this.first, this.rows);
    }

    reset() {
        this.first = 0;
        this.displayWorkouts(this.first, this.rows);
    }

    pageChange(event: { first: number; rows: number; }) {
        this.first = event.first;
        this.rows = event.rows;
        this.displayWorkouts(this.first, this.rows);
    }

    isLastPage(): boolean {
        return this.workouts ? this.first + this.rows >= this.workouts.length : true;
    }

    isFirstPage(): boolean {
        return this.workouts ? this.first === 0 : true;
    }

    displayWorkouts(first: number, rows: number){
        this.workoutService.retrievePaginatedWorkouts(this.first, this.rows).subscribe({
            next: (data) => {
                this.workouts = data;
            }
        });
    }
}
