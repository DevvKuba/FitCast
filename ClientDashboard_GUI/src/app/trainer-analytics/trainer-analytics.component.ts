import { Component, inject, OnInit, Signal } from '@angular/core';
import { Button } from "primeng/button";
import { UserBase } from '../models/user-base';
import { AccountService } from '../services/account.service';
import { MlPredictionService } from '../services/ml-prediction.service';
import { TWO_THIRDS_PI } from 'chart.js/helpers';
import { ToastService } from '../services/toast.service';
import { Dialog } from 'primeng/dialog';
import { PredictionResult } from '../models/prediction-result';

@Component({
  selector: 'app-trainer-analytics',
  imports: [Button, Dialog],
  templateUrl: './trainer-analytics.component.html',
  styleUrl: './trainer-analytics.component.css'
})
export class TrainerAnalyticsComponent implements OnInit {
  accountService = inject(AccountService);
  mlPredictionService = inject(MlPredictionService);
  toastService = inject(ToastService);

  currentTrainerId: number = 0;
  predictionDate: Date | null = null;
  predictedRevenue: number = 0;
  predictedLowerBound: number = 0;
  predictedUpperBound: number = 0;
  confidence: string = "";
  predictionMessage: string = "";

  predictionDialogVisible = false;

  ngOnInit(): void {
    this.currentTrainerId = this.accountService.currentUser()?.id ?? 0;
  }

  predictNextMonthsRevenue(){
    this.mlPredictionService.trainModelAndPredictTrainerRevenue(this.accountService.currentUser()?.id ?? 0).subscribe({
      next: (response : PredictionResult) => {
        // use response data object to set all specific dialog display properties

        // pops open a display dialog with prediction information..
        this.predictionDialogVisible = true;

      },
      error: (response) => {
        this.toastService.showError('Error Prediction Revenue', response.error.message);
      }
    })
  }
}
