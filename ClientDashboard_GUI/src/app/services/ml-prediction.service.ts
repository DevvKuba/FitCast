import { HttpClient } from '@angular/common/http';
import { inject, Injectable } from '@angular/core';
import { environment } from '../environments/environment';
import { AccountService } from './account.service';
import { Observable } from 'rxjs';

@Injectable({
  providedIn: 'root'
})
export class MlPredictionService {
  http = inject(HttpClient);
  baseUrl = environment.apiUrl;
  accountService = inject(AccountService);

  trainModelAndPredictTrainerRevenue(trainerId: number) : Observable<any> {
    return this.http.get(this.baseUrl + `mlprediction/trainAndPredictRevenue?trainerId=${trainerId}`);
  }
}
