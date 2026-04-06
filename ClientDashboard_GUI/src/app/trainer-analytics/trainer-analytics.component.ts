import { Component, inject, OnInit, Signal } from '@angular/core';
import { CommonModule } from '@angular/common';
import { RevenuePredictionComponent } from '../revenue-prediction/revenue-prediction.component';
import { ActivityPatternsDto } from '../models/dtos/activity-patterns-dto';
import { ClientMetricsDto } from '../models/dtos/client-metrics-dto';
import { RevenuePatternsDto } from '../models/dtos/revenue-patterns-dto';
import { WeeklyMultiplier } from '../models/dtos/weekly-multiplier';
import { CompleteTrainerAnalyticsDto } from '../models/dtos/complete-trainer-analytics-dto';
import { TrainerService } from '../services/trainer.service';
import { UserDto } from '../models/dtos/user-dto';
import { AccountService } from '../services/account.service';
import { ToastService } from '../services/toast.service';

@Component({
  selector: 'app-trainer-analytics',
  imports: [CommonModule, RevenuePredictionComponent],
  templateUrl: './trainer-analytics.component.html',
  styleUrl: './trainer-analytics.component.css'
})
export class TrainerAnalyticsComponent implements OnInit{
  trainerService = inject(TrainerService);
  accountService = inject(AccountService);
  toastService = inject(ToastService);

  analyticsData : CompleteTrainerAnalyticsDto | undefined;
  currentUserId: number = 0; 

  ngOnInit(): void {
   this.currentUserId = this.accountService.currentUser()?.id ?? 0;
   this.retrieveAnalytics();
  }

  selectedScope: 'lastMonth' | 'allData' = 'lastMonth';

  setMetricScope(scope: 'lastMonth' | 'allData'): void {
    this.selectedScope = scope;
    this.retrieveAnalytics();
  }

  retrieveAnalytics(){
    if(this.selectedScope == 'lastMonth'){
      this.trainerService.getLastMonthsAnalytics(this.currentUserId).subscribe({
        next: (response) => {
          this.analyticsData = response.data;
        },
        error: (response) => {
          this.toastService.showError('Error', response.error.message);
        }
      })
    }
    else if(this.selectedScope == 'allData'){
      this.trainerService.getFullMonthsAnalytics(this.currentUserId).subscribe({
        next: (response) => {
          this.analyticsData = response.data;
        },
        error: (response) => {
          this.toastService.showError('Error', response.error.message);
        }
      })
    }
  }

  formatWeeklyMultipliers(values: WeeklyMultiplier[]): string {
    return values.map((value) => `${value.weekday} (${value.multiplier}x)`).join(', ');
  }

}
