import { HttpClient, HttpParams } from '@angular/common/http';
import { inject, Injectable } from '@angular/core';
import { Workout } from '../models/workout';
import { Observable } from 'rxjs';
import { ApiResponse } from '../models/api-response';

@Injectable({
  providedIn: 'root'
})
export class WorkoutService {
  http = inject(HttpClient);

  retrievePaginatedWorkouts(): Observable<ApiResponse<Workout[]>> {
    return this.http.get<ApiResponse<Workout[]>>(`https://clientdashboardapp-dfdja3c4hxffdsg0.uksouth-01.azurewebsites.net/GetPaginatedWorkouts`);
  }
  
}
