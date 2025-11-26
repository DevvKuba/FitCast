import { HttpClient } from '@angular/common/http';
import { inject, Injectable } from '@angular/core';
import { environment } from '../environments/environment';
import { Observable, ObservableLike } from 'rxjs';

@Injectable({
  providedIn: 'root'
})
export class TrainerService {
  http = inject(HttpClient);
  baseUrl = environment.apiUrl;

  assignClient(clientId : number, trainerId : number) : Observable<any>{
  return this.http.put(this.baseUrl + `trainer/assignClient?clientId=${clientId}&trainerId=${trainerId}`, null);
  }

  retrieveTrainerById(trainerId: number) : Observable<any>{
    return this.http.get(this.baseUrl + `trainer/retrieveTrainerById?trainerId=${trainerId}`);
  }

  gatherAndUpdateExternalWorkouts(trainerId: number) : Observable<any> {
    return this.http.put(this.baseUrl + `trainer/getDailyHevyWorkouts?trainerId=${trainerId}`, null);
  }

  getWorkoutRetrievalApiKey(trainerId : number) : Observable<any>{
    return this.http.get(this.baseUrl +  `trainer/getHevyApiKey?trainerId=${trainerId}`);
  }

  getAutoWorkoutRetrievalStatus(trainerId : number) : Observable<any> {
    return this.http.get(this.baseUrl + `trainer/getAutoRetrievalWorkoutStatus?trainerId=${trainerId}`);
  }

  getAutoPaymentSettingStatus(trainerId: number) : Observable<any> {
    return this.http.get(this.baseUrl + `getAutoPaymentSettingStatus?trainerId=${trainerId}`);
  }

  updateTrainerProfile(trainerId: number, newProfile: TrainerUpdateDto) : Observable<any>{
    return this.http.put(this.baseUrl + `trainer/updateTrainerProfileDetails?trainerId=${trainerId}`, newProfile);
  }

  updateTrainerRetrievalDetails(trainerId: number, providedApiKey: string, retrievalStatus: boolean) : Observable<any>{
    return this.http.put(this.baseUrl + `trainer/updateTrainerRetrievalDetails?trainerId=${trainerId}&providedApiKey=${providedApiKey}&enabled=${retrievalStatus}`, null);
  }
}
