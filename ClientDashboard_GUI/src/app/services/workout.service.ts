import { HttpClient, HttpParams } from '@angular/common/http';
import { inject, Injectable } from '@angular/core';
import { Workout } from '../models/workout';
import { Observable } from 'rxjs';
import { ApiResponse } from '../models/api-response';
import { environment } from '../environments/environment';

@Injectable({
  providedIn: 'root'
})
export class WorkoutService {
  http = inject(HttpClient);
  baseUrl = environment.apiUrl;

  retrievePaginatedWorkouts(): Observable<ApiResponse<Workout[]>> {
    return this.http.get<ApiResponse<Workout[]>>(this.baseUrl + `workout/GetPaginatedWorkouts`);
  }
  
}
