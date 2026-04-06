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
    return this.http.get(this.baseUrl + `trainer/getAutoPaymentSettingStatus?trainerId=${trainerId}`);
  }

  getLastMonthsAnalytics(trainerId: number) : Observable<ApiResponse<CompleteTrainerAnalyticsDto>> {
    return this.http.get<ApiResponse<CompleteTrainerAnalyticsDto>>(this.baseUrl + `trainer/getTrainerLastMonthsAnalytics?trainerId=${trainerId}`);
  }

  getFullMonthsAnalytics(trainerId: number) : Observable<ApiResponse<CompleteTrainerAnalyticsDto>> {
    return this.http.get<ApiResponse<CompleteTrainerAnalyticsDto>>(this.baseUrl + `trainer/getTrainerAllMonthsAnalytics?trainerId=${trainerId}`);
  }

  getAllExcludedNames(trainerId: number) : Observable<any> {
    return this.http.get(this.baseUrl + `trainer/getAllExcludedNames?trainerId=${trainerId}`);
  }

  updateTrainerProfile(trainerId: number, newProfile: TrainerUpdateDto) : Observable<any>{
    return this.http.put(this.baseUrl + `trainer/updateTrainerProfileDetails?trainerId=${trainerId}`, newProfile);
  }

  updateTrainerRetrievalDetails(trainerId: number, providedApiKey: string, enabled: boolean) : Observable<any>{
    return this.http.put(this.baseUrl + `trainer/updateTrainerRetrievalDetails?trainerId=${trainerId}&providedApiKey=${providedApiKey}&enabled=${enabled}`, null);
  }

  updateTrainerPaymentSetting(trainerId: number, enabled: boolean) : Observable<any> {
    return this.http.put(this.baseUrl +  `trainer/updateTrainerPaymentSetting?trainerId=${trainerId}&enabled=${enabled}`, null);
  }

  addExcludedName(excludedDetails : ExcludeNameDto) : Observable<any> {
    return this.http.post(this.baseUrl + 'trainer/addExcludedName', excludedDetails);
  }

  deleteExcludedName(excludedDetails : ExcludeNameDto) : Observable<any> {
    return this.http.put(this.baseUrl + 'trainer/deleteExcludedName', excludedDetails)
  }
}
