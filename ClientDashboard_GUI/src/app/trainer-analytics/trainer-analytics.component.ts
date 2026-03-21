import { Component, inject, OnInit } from '@angular/core';
import { DatePipe, DecimalPipe } from '@angular/common';
import { Button } from "primeng/button";
import { AccountService } from '../services/account.service';
import { MlPredictionService } from '../services/ml-prediction.service';
import { ToastService } from '../services/toast.service';
import { Dialog } from 'primeng/dialog';
import { PredictionResult } from '../models/prediction-result';

@Component({
  selector: 'app-trainer-analytics',
  imports: [Button, Dialog, DatePipe, DecimalPipe],
  templateUrl: './trainer-analytics.component.html',
  styleUrl: './trainer-analytics.component.css'
})
export class TrainerAnalyticsComponent implements OnInit {
  accountService = inject(AccountService);
  mlPredictionService = inject(MlPredictionService);
  toastService = inject(ToastService);

  currentTrainerId: number = 0;
  predictionDate: Date | null = null;
  monthsOfData: number = 0;
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
        this.predictionDate = response.predictedDate ? new Date(response.predictedDate) : null;
        this.monthsOfData = response.monthsOfData;
        this.predictedRevenue = response.predictedRevenue;
        this.predictedLowerBound = response.lowerBound ?? 0;
        this.predictedUpperBound = response.upperBound ?? 0;
        this.confidence = response.confidence;
        this.predictionMessage = response.message;
        
        this.predictionDialogVisible = true;

      },
      error: (response) => {
        this.toastService.showError('Error Prediction Revenue', response.error.message);
      }
    })
  }
}
