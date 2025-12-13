import { HttpClient } from '@angular/common/http';
import { inject, Injectable } from '@angular/core';
import { Workout } from '../models/workout';
import { Observable } from 'rxjs';
import { ApiResponse } from '../models/api-response';
import { environment } from '../environments/environment';
import { WorkoutAddDto } from '../models/dtos/workout-add-dto';
import { WorkoutUpdateDto } from '../models/dtos/workout-update-dto';

@Injectable({
  providedIn: 'root'
})
export class WorkoutService {
  http = inject(HttpClient);
  baseUrl = environment.apiUrl;

  retrieveTrainerClientWorkouts(trainerId : number): Observable<ApiResponse<Workout[]>> {
    return this.http.get<ApiResponse<Workout[]>>(this.baseUrl + `workout/GetTrainerWorkouts?trainerId=${trainerId}`);
  }

  retrieveClientSpecificWorkouts(clientId: number): Observable<ApiResponse<Workout[]>>{
    return this.http.get<ApiResponse<Workout[]>>(this.baseUrl + `workout/GetClientSpecificWorkouts?clientId=${clientId}`);
  }

  addWorkout(newWorkout : WorkoutAddDto) : Observable<ApiResponse<string>>{
    return this.http.post<ApiResponse<string>>(this.baseUrl + 'workout/Manual/NewWorkout', newWorkout);
  }

  updateWorkout(updatedWorkoutInfo: WorkoutUpdateDto) : Observable<ApiResponse<any>>{
    return this.http.put<any>(this.baseUrl + 'workout/updateWorkout', updatedWorkoutInfo);
  }

  deleteWorkout(workoutId : number) : Observable<ApiResponse<any>>{
    return this.http.delete<any>(this.baseUrl + `workout/DeleteWorkout?workoutId=${workoutId}`);
  }
}
