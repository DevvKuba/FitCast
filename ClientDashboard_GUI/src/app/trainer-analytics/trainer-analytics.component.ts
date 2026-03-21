import { Component, inject, OnInit } from '@angular/core';
import { CommonModule } from '@angular/common';
import { Button } from "primeng/button";
import { AccountService } from '../services/account.service';
import { MlPredictionService } from '../services/ml-prediction.service';
import { ToastService } from '../services/toast.service';
import { Dialog } from 'primeng/dialog';
import { PredictionResult } from '../models/prediction-result';

@Component({
  selector: 'app-trainer-analytics',
  imports: [CommonModule, Button, Dialog],
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
  currency: string = "";
  confidence: string = "";
  predictionMessage: string = "";

  predictionDialogVisible = false;

  ngOnInit(): void {
    this.currentTrainerId = this.accountService.currentUser()?.id ?? 0;
  }

  predictNextMonthsRevenue(){
    this.mlPredictionService.trainModelAndPredictTrainerRevenue(this.accountService.currentUser()?.id ?? 0).subscribe({
      next: (response) => {
        this.predictionDate = response.data?.predictedDate ? new Date(response.data?.predictedDate) : null;
        this.monthsOfData = response.data?.monthsOfData ?? 0;
        this.predictedRevenue = response.data?.predictedRevenue ?? 0;
        this.predictedLowerBound = response.data?.lowerBound ?? 0;
        this.predictedUpperBound = response.data?.upperBound ?? 0;
        this.currency = response.data?.currency ?? "";
        this.confidence = response.data?.confidence ?? 'uncertain';
        this.predictionMessage = response.message;
        
        this.predictionDialogVisible = true;

      },
      error: (response) => {
        this.toastService.showError('Error Prediction Revenue', response.error.message);
      },
    })
  }
}
