import { HttpClient } from '@angular/common/http';
import { inject, Injectable } from '@angular/core';
import { environment } from '../environments/environment';
import { AccountService } from './account.service';
import { Observable } from 'rxjs';
import { PredictionResult } from '../models/prediction-result';
import { ApiResponse } from '../models/api-response';

@Injectable({
  providedIn: 'root'
})
export class MlPredictionService {
  http = inject(HttpClient);
  baseUrl = environment.apiUrl;
  accountService = inject(AccountService);

  trainModelAndPredictTrainerRevenue(trainerId: number) : Observable<ApiResponse<PredictionResult>> {
    return this.http.get<ApiResponse<PredictionResult>>(this.baseUrl + `mlprediction/trainAndPredictRevenue?trainerId=${trainerId}`);
  }
}
