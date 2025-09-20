import { HttpClient, HttpParams } from '@angular/common/http';
import { inject, Injectable } from '@angular/core';
import { Workout } from '../models/workout';
import { Observable } from 'rxjs';

@Injectable({
  providedIn: 'root'
})
export class WorkoutService {
  http = inject(HttpClient);

  retrievePaginatedWorkouts(first: number, rows: number): Observable<Workout[]> {
    return this.http.get<Workout[]>(`https://clientdashboardapp-dfdja3c4hxffdsg0.uksouth-01.azurewebsites.net/GetPaginatedWorkouts?first=${first}&rows=${rows}`);
  }
  
}
