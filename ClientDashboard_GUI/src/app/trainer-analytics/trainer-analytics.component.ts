import { Component, inject, OnInit, Signal } from '@angular/core';
import { Button } from "primeng/button";
import { UserBase } from '../models/user-base';
import { AccountService } from '../services/account.service';
import { MlPredictionService } from '../services/ml-prediction.service';
import { TWO_THIRDS_PI } from 'chart.js/helpers';

@Component({
  selector: 'app-trainer-analytics',
  imports: [Button],
  templateUrl: './trainer-analytics.component.html',
  styleUrl: './trainer-analytics.component.css'
})
export class TrainerAnalyticsComponent implements OnInit {
  accountService = inject(AccountService);
  mlPredictionService = inject(MlPredictionService);
  currentTrainerId: number = 0;

  ngOnInit(): void {
    this.currentTrainerId = this.accountService.currentUser()?.id ?? 0;
  }

  predictNextMonthsRevenue(){
    this.mlPredictionService.trainModelAndPredictTrainerRevenue(this.accountService.currentUser()?.id ?? 0).subscribe({
      next: (response) => {

      },
      error: (response) => {

      }
    })
  }
}
