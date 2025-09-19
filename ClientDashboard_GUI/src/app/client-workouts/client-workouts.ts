import { Component, inject, OnInit } from '@angular/core';
import { TableModule } from 'primeng/table';
import { CommonModule } from '@angular/common';
import { ButtonModule } from 'primeng/button';
import { Workout } from '../models/workout';
import { WorkoutService } from '../services/workout.service';

@Component({
  selector: 'app-client-workouts',
  imports: [TableModule, CommonModule, ButtonModule],
  templateUrl: './client-workouts.html',
  styleUrl: './client-workouts.css'
})
export class ClientWorkouts {
  workouts!: Workout[];
  private workoutService = inject(WorkoutService);

    first = 0;
    rows = 10;

    ngOnInit() {
      // calls api call here from the workout service
        // this.customerService.getCustomersLarge().then((workouts: Workout[]) => (this.workouts = workouts));
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
}
