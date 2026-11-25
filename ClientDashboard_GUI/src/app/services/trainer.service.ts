import { HttpClient } from '@angular/common/http';
import { inject, Injectable } from '@angular/core';
import { environment } from '../environments/environment';
import { Observable, ObservableLike, ObservedValueOf } from 'rxjs';
import { ApiResponse } from '../models/api-response';
import { CompleteTrainerAnalyticsDto } from '../models/dtos/complete-trainer-analytics-dto';
import { CurrentMonthTrainerAnalyticsDto } from '../models/dtos/current-month-trainer-analytics-dto';
import { AnalyticsMonthDto } from '../models/dtos/analytics-month-dto';

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

  getAutoPaymentSettingStatus() : Observable<any> {
    return this.http.get(this.baseUrl + `trainer/getAutoPaymentSettingStatus`);
  }

  getSpecificMonthAnalytics(month: number, year: number): Observable<ApiResponse<CompleteTrainerAnalyticsDto>> {
    return this.http.get<ApiResponse<CompleteTrainerAnalyticsDto>>(this.baseUrl + `trainer/getTrainerSpecificMonthAnalytics?month=${month}&year=${year}`);
  }

  getCurrentMonthsAnalytics() : Observable<ApiResponse<CurrentMonthTrainerAnalyticsDto>> {
    return this.http.get<ApiResponse<CurrentMonthTrainerAnalyticsDto>>(this.baseUrl + `trainer/getTrainerCurrentMonthsAnalytics`);
  }

  getAnalyticsMonths(): Observable<ApiResponse<AnalyticsMonthDto[]>> {
    return this.http.get<ApiResponse<AnalyticsMonthDto[]>>(this.baseUrl + `trainer/getTrainerAnalyticsMonths`);
  }

  updateTrainerProfile(newProfile: TrainerUpdateDto) : Observable<any>{
    return this.http.put(this.baseUrl + `trainer/updateTrainerProfileDetails`, newProfile);
  }

  updateTrainerPaymentSetting(enabled: boolean) : Observable<any> {
    return this.http.put(this.baseUrl +  `trainer/updateTrainerPaymentSetting?enabled=${enabled}`, null);
  }
}
