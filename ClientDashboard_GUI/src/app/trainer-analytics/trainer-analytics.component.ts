import { Component, inject, OnInit } from '@angular/core';
import { CommonModule } from '@angular/common';
import { RevenuePredictionComponent } from '../revenue-prediction/revenue-prediction.component';
import { WeeklyMultiplier } from '../models/dtos/weekly-multiplier';
import { CompleteTrainerAnalyticsDto } from '../models/dtos/complete-trainer-analytics-dto';
import { TrainerService } from '../services/trainer.service';
import { AccountService } from '../services/account.service';
import { ToastService } from '../services/toast.service';
import { ChartModule } from 'primeng/chart';
import { ChartData, ChartOptions } from 'chart.js';
import { WeekDays } from '../enums/weekdays';

@Component({
  selector: 'app-trainer-analytics',
  imports: [CommonModule, RevenuePredictionComponent, ChartModule],
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

  clientMetricsChartOptions: ChartOptions<'bar'> = {
    responsive: true,
    maintainAspectRatio: false,
    plugins: {
      legend: {
        display: false
      }
    },
    scales: {
      y: {
        beginAtZero: true,
        ticks: {
          precision: 0
        }
      }
    }
  };

  revenuePatternsChartOptions: ChartOptions<'bar'> = {
    responsive: true,
    maintainAspectRatio: false,
    plugins: {
      legend: {
        display: false
      }
    },
    scales: {
      y: {
        beginAtZero: true,
        ticks: {
          precision: 0
        }
      }
    }
  };

  activityPatternsChartOptions: ChartOptions<'line'> = {
    responsive: true,
    maintainAspectRatio: false,
    plugins: {
      legend: {
        display: false
      }
    },
    scales: {
      x: {
        ticks: {
          autoSkip: false
        }
      },
      y: {
        beginAtZero: true,
        ticks: {
          precision: 0
        }
      }
    }
  };

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

  get clientMetricsChartData(): ChartData<'bar'> {
    if (!this.analyticsData) {
      return { labels: [], datasets: [] };
    }

    return {
      labels: [
        'Base clients',
        'Sessions per client',
        this.selectedScope === 'lastMonth' ? 'Monthly sessions' : 'Average monthly sessions'
      ],
      datasets: [
        {
          data: [
            this.analyticsData.baseClients,
            this.analyticsData.sessionsPerClient,
            this.analyticsData.monthlyClientSessions
          ],
          backgroundColor: ['#1d4ed8', '#14b8a6', '#0f766e'],
          borderRadius: 8,
          borderSkipped: false
        }
      ]
    };
  }

  get revenuePatternsChartData(): ChartData<'bar'> {
    if (!this.analyticsData) {
      return { labels: [], datasets: [] };
    }

    return {
      labels: ['Revenue / day', 'Revenue / week', 'Revenue / month'],
      datasets: [
        {
          data: [
            this.analyticsData.revenuePerWorkingDay,
            this.analyticsData.revenuePerWorkingWeek,
            this.analyticsData.revenuePerWorkingMonth
          ],
          backgroundColor: ['#14b8a6', '#0ea5e9', '#2563eb'],
          borderRadius: 8,
          borderSkipped: false
        }
      ]
    };
  }

  get activityPatternsChartData(): ChartData<'line'> {
    if (!this.analyticsData) {
      return { labels: [], datasets: [] };
    }

    const days = this.analyticsData.allWeekdays.map((weekday) => weekday.day);

    return {
      labels: days.map((weekday) => WeekDays[weekday]),
      datasets: [
        {
          label: 'All weekdays',
          data: this.analyticsData.allWeekdays.map((weekday) => weekday.multiplier),
          borderColor: '#2563eb',
          backgroundColor: '#2563eb',
          pointBackgroundColor: '#2563eb',
          pointBorderColor: '#2563eb',
          pointRadius: 5,
          pointHoverRadius: 7,
          tension: 0.3,
          fill: false
        }
      ]
    };
  }

  formatWeeklyMultipliers(values: WeeklyMultiplier[]): string {
    return values
      .map((value) => `${WeekDays[value.day]} (${value.multiplier}x)`)
      .join(', ');
  }

}
