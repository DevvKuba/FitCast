import { HttpClient } from '@angular/common/http';
import { inject, Injectable } from '@angular/core';
import { environment } from '../environments/environment';
import { Observable, ObservableLike, ObservedValueOf } from 'rxjs';
import { ExcludeNameDto } from '../models/dtos/exclude-name-dto';
import { ApiResponse } from '../models/api-response';
import { CompleteTrainerAnalyticsDto } from '../models/dtos/complete-trainer-analytics-dto';

@Injectable({
  providedIn: 'root'
})
export class TrainerService {
  http = inject(HttpClient);
  baseUrl = environment.apiUrl;

  assignClient(clientId : number) : Observable<any>{
  return this.http.put(this.baseUrl + `trainer/assignClient?clientId=${clientId}`, null);
  }

  retrieveTrainerById() : Observable<any>{
    return this.http.get(this.baseUrl + `trainer/retrieveTrainerById`);
  }

  gatherAndUpdateExternalWorkouts() : Observable<any> {
    return this.http.put(this.baseUrl + `trainer/getDailyHevyWorkouts`, null);
  }

  getWorkoutRetrievalApiKey() : Observable<any>{
    return this.http.get(this.baseUrl +  `trainer/getHevyApiKey`);
  }

  getAutoWorkoutRetrievalStatus() : Observable<any> {
    return this.http.get(this.baseUrl + `trainer/getAutoRetrievalWorkoutStatus`);
  }

  getAutoPaymentSettingStatus() : Observable<any> {
    return this.http.get(this.baseUrl + `trainer/getAutoPaymentSettingStatus`);
  }

  getLastMonthsAnalytics(trainerId: number) : Observable<ApiResponse<CompleteTrainerAnalyticsDto>> {
    return this.http.get<ApiResponse<CompleteTrainerAnalyticsDto>>(this.baseUrl + `trainer/getTrainerLastMonthsAnalytics?trainerId=${trainerId}`);
  }

  getFullMonthsAnalytics(trainerId: number) : Observable<ApiResponse<CompleteTrainerAnalyticsDto>> {
    return this.http.get<ApiResponse<CompleteTrainerAnalyticsDto>>(this.baseUrl + `trainer/getTrainerAllMonthsAnalytics?trainerId=${trainerId}`);
  }

  getAllExcludedNames() : Observable<any> {
    return this.http.get(this.baseUrl + `trainer/getAllExcludedNames`);
  }

  updateTrainerProfile(newProfile: TrainerUpdateDto) : Observable<any>{
    return this.http.put(this.baseUrl + `trainer/updateTrainerProfileDetails`, newProfile);
  }

  updateTrainerRetrievalDetails(providedApiKey: string, enabled: boolean) : Observable<any>{
    return this.http.put(this.baseUrl + `trainer/updateTrainerRetrievalDetails?providedApiKey=${providedApiKey}&enabled=${enabled}`, null);
  }

  updateTrainerPaymentSetting(enabled: boolean) : Observable<any> {
    return this.http.put(this.baseUrl +  `trainer/updateTrainerPaymentSetting?enabled=${enabled}`, null);
  }

  addExcludedName(excludedDetails : ExcludeNameDto) : Observable<any> {
    return this.http.post(this.baseUrl + 'trainer/addExcludedName', excludedDetails);
  }

  deleteExcludedName(excludedDetails : ExcludeNameDto) : Observable<any> {
    return this.http.put(this.baseUrl + 'trainer/deleteExcludedName', excludedDetails)
  }
}
