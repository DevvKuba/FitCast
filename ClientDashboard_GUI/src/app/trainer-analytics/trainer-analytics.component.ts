import { Component, inject, OnInit } from '@angular/core';
import { CommonModule } from '@angular/common';
import { Button } from "primeng/button";
import { AccountService } from '../services/account.service';
import { MlPredictionService } from '../services/ml-prediction.service';
import { ToastService } from '../services/toast.service';
import { Dialog } from 'primeng/dialog';
import { PredictionResult } from '../models/prediction-result';
import { RevenuePredictionComponent } from '../revenue-prediction/revenue-prediction.component';

@Component({
  selector: 'app-trainer-analytics',
  imports: [CommonModule, RevenuePredictionComponent],
  templateUrl: './trainer-analytics.component.html',
  styleUrl: './trainer-analytics.component.css'
})
export class TrainerAnalyticsComponent implements OnInit {
  accountService = inject(AccountService);
  mlPredictionService = inject(MlPredictionService);
  toastService = inject(ToastService);

  currentTrainerId: number = 0;

  ngOnInit(): void {
    this.currentTrainerId = this.accountService.currentUser()?.id ?? 0;
  }

}
