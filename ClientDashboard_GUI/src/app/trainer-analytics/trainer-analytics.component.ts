import { Component, inject, OnInit } from '@angular/core';
import { CommonModule } from '@angular/common';
import { RevenuePredictionComponent } from '../revenue-prediction/revenue-prediction.component';
import { WeeklyActivityPattern } from '../models/dtos/weekly-activity-pattern';
import { CompleteTrainerAnalyticsDto } from '../models/dtos/complete-trainer-analytics-dto';
import { TrainerService } from '../services/trainer.service';
import { AccountService } from '../services/account.service';
import { ToastService } from '../services/toast.service';
import { ChartModule } from 'primeng/chart';
import { ChartData, ChartOptions } from 'chart.js';
import { WeekDays } from '../enums/weekdays';
import { SpinnerComponent } from "../spinner/spinner.component";
import { CurrentMonthTrainerAnalyticsDto } from '../models/dtos/current-month-trainer-analytics-dto';

@Component({
  selector: 'app-trainer-analytics',
  imports: [CommonModule, RevenuePredictionComponent, ChartModule, SpinnerComponent],
  templateUrl: './trainer-analytics.component.html',
  styleUrl: './trainer-analytics.component.css'
})
export class TrainerAnalyticsComponent implements OnInit{
  trainerService = inject(TrainerService);
  accountService = inject(AccountService);
  toastService = inject(ToastService);

  currentMonthAnalyticsData: CurrentMonthTrainerAnalyticsDto | undefined;
  completeMonthAnalyticsData : CompleteTrainerAnalyticsDto | undefined;
  currentUserId: number = 0; 

  ngOnInit(): void {
   this.currentUserId = this.accountService.currentUser()?.id ?? 0;
   this.retrieveCurrentMonthAnalytics();
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
    this.retrieveCurrentMonthAnalytics();
  }

  retrieveCurrentMonthAnalytics(){
    this.trainerService.getCurrentMonthsAnalytics().subscribe({
      next: (response) => {
        this.currentMonthAnalyticsData = response.data;
      },
      error: (response) => {
        this.toastService.showError('Error getting current month data', response.error.message);
      }
    })
  }

  retrieveSpecificMonthAnalytics(month: number, year: number){
      this.trainerService.getSpecificMonthAnalytics(month, year).subscribe({
        next: (response) => {
          this.completeMonthAnalyticsData = response.data;
        },
        error: (response) => {
          this.toastService.showError(`Error getting ${month}/${year} data`, response.error.message);
        }
      })
  }

  get clientMetricsChartData(): ChartData<'bar'> {
    if (!this.completeMonthAnalyticsData) {
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
            this.completeMonthAnalyticsData.baseClients,
            this.completeMonthAnalyticsData.averageSessionsPerClient,
            this.completeMonthAnalyticsData.totalClientSessions
          ],
          backgroundColor: ['#1d4ed8', '#14b8a6', '#0f766e'],
          borderRadius: 8,
          borderSkipped: false
        }
      ]
    };
  }

  get revenuePatternsChartData(): ChartData<'bar'> {
    if (!this.completeMonthAnalyticsData) {
      return { labels: [], datasets: [] };
    }

    return {
      labels: ['Revenue / day', 'Revenue / week'],
      datasets: [
        {
          data: [
            this.completeMonthAnalyticsData.revenuePerWorkingDay,
            this.completeMonthAnalyticsData.revenuePerWorkingWeek
          ],
          backgroundColor: ['#14b8a6', '#0ea5e9'],
          borderRadius: 8,
          borderSkipped: false
        }
      ]
    };
  }

  get activityPatternsChartData(): ChartData<'line'> {
    if (!this.completeMonthAnalyticsData) {
      return { labels: [], datasets: [] };
    }

    const days = this.completeMonthAnalyticsData.allWeekdays.map((weekday) => weekday.day);

    return {
      labels: days.map((weekday) => WeekDays[weekday]),
      datasets: [
        {
          label: 'All weekdays',
          data: this.completeMonthAnalyticsData.allWeekdays.map((weekday) => weekday.multiplier),
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

  formatWeeklyMultipliers(values: WeeklyActivityPattern[]): string {
    return values
      .map((value) => `${WeekDays[value.day]} (${value.multiplier}x)`)
      .join(', ');
  }

  formatWeeklyMultiplier(value: WeeklyActivityPattern): string {
    return `${WeekDays[value.day]} - (${value.multiplier}x)`;
  }

  convertMinutesToDecimalHours(totalMinutes: number): number {
    return Math.round((totalMinutes / 60) * 10) / 10;
  }

  convertMinutesToHoursAndMinutes(totalMinutes: number): { hours: number; minutes: number } {
    const hours = Math.floor(totalMinutes / 60);
    const minutes = totalMinutes % 60;
    return { hours, minutes };
  }

}
