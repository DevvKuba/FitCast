import { HttpClient } from '@angular/common/http';
import { inject, Injectable } from '@angular/core';
import { environment } from '../environments/environment';
import { AccountService } from './account.service';
import { Observable } from 'rxjs';
import { PredictionResult } from '../models/prediction-result';

@Injectable({
  providedIn: 'root'
})
export class MlPredictionService {
  http = inject(HttpClient);
  baseUrl = environment.apiUrl;
  accountService = inject(AccountService);

  trainModelAndPredictTrainerRevenue(trainerId: number) : Observable<PredictionResult> {
    return this.http.get<PredictionResult>(this.baseUrl + `mlprediction/trainAndPredictRevenue?trainerId=${trainerId}`);
  }
}
