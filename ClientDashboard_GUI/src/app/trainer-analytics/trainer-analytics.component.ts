import { Component, inject, OnInit } from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormsModule } from '@angular/forms';
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
import { DatePickerModule } from 'primeng/datepicker';
import { CardModule } from 'primeng/card';
import { RouterLink } from '@angular/router';

type AnalyticsViewMode = 'current' | 'past';

@Component({
  selector: 'app-trainer-analytics',
  imports: [CommonModule, FormsModule, ChartModule, SpinnerComponent, DatePickerModule, CardModule, RouterLink],
  templateUrl: './trainer-analytics.component.html',
  styleUrl: './trainer-analytics.component.css'
})
export class TrainerAnalyticsComponent implements OnInit{
  trainerService = inject(TrainerService);
  accountService = inject(AccountService);
  toastService = inject(ToastService);

  viewMode: AnalyticsViewMode = 'current';

  currentMonthAnalyticsData: CurrentMonthTrainerAnalyticsDto | undefined;
  completeMonthAnalyticsData : CompleteTrainerAnalyticsDto | undefined;

  // The most recent fully-completed calendar month - the upper bound for past-month selection.
  readonly maxSelectableMonthDate: Date = this.previousMonthDate(new Date());
  selectedMonthDate: Date = this.maxSelectableMonthDate;

  ngOnInit(): void {
   this.retrieveCurrentMonthAnalytics();
  }

  setViewMode(mode: AnalyticsViewMode): void {
    this.viewMode = mode;

    if (mode === 'past' && !this.completeMonthAnalyticsData) {
      this.retrieveSelectedMonthAnalytics();
    }
  }

  private previousMonthDate(from: Date): Date {
    return new Date(from.getFullYear(), from.getMonth() - 1, 1); 
    // - 1 to account for JS Date 0 indexes months
  }

  get isNextMonthDisabled(): boolean {
    return this.selectedMonthDate.getFullYear() === this.maxSelectableMonthDate.getFullYear()
      && this.selectedMonthDate.getMonth() === this.maxSelectableMonthDate.getMonth();
  }

  goToPreviousMonth(): void {
    this.selectedMonthDate = new Date(this.selectedMonthDate.getFullYear(), this.selectedMonthDate.getMonth() - 1, 1);
    this.retrieveSelectedMonthAnalytics();
  }

  goToNextMonth(): void {
    if (this.isNextMonthDisabled) return;
    this.selectedMonthDate = new Date(this.selectedMonthDate.getFullYear(), this.selectedMonthDate.getMonth() + 1, 1);
    this.retrieveSelectedMonthAnalytics();
  }

  onMonthPickerSelect(date: Date): void {
    this.selectedMonthDate = new Date(date.getFullYear(), date.getMonth(), 1);
    this.retrieveSelectedMonthAnalytics();
  }

  retrieveSelectedMonthAnalytics(): void {
    this.retrieveSpecificMonthAnalytics(this.selectedMonthDate.getMonth() + 1, this.selectedMonthDate.getFullYear());
    // the + 1 to send appropriate month int format to API
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
      this.completeMonthAnalyticsData = undefined;
      this.trainerService.getSpecificMonthAnalytics(month, year).subscribe({
        next: (response) => {
          this.completeMonthAnalyticsData = response.data;
        },
        error: (response) => {
          this.toastService.showError(`Error getting ${month}/${year} data`, response.error.message);
        }
      })
  }

  // ---- Current month: weekly activity chart ----

  currentMonthActivityChartOptions: ChartOptions<'bar'> = {
    responsive: true,
    maintainAspectRatio: false,
    plugins: {
      legend: { display: false }
    },
    scales: {
      x: { grid: { display: false } },
      y: { beginAtZero: true, ticks: { precision: 0 } }
    }
  };

  get currentMonthActivityChartData(): ChartData<'bar'> {
    if (!this.currentMonthAnalyticsData) {
      return { labels: [], datasets: [] };
    }

    return {
      labels: this.currentMonthAnalyticsData.weeklySessionsCounts.map((day) => WeekDays[day.day]),
      datasets: [
        {
          data: this.currentMonthAnalyticsData.weeklySessionsCounts.map((day) => day.totalSessions),
          backgroundColor: '#2563eb',
          borderRadius: 4,
          borderSkipped: false
        }
      ]
    };
  }

  // ---- Past month: client metrics chart (Active / Churned / Acquisitions / Total Sessions) ----

  clientMetricsChartOptions: ChartOptions<'bar'> = {
    responsive: true,
    maintainAspectRatio: false,
    plugins: {
      legend: { display: false }
    },
    scales: {
      y: { beginAtZero: true, ticks: { precision: 0 } }
    }
  };

  get clientMetricsChartData(): ChartData<'bar'> {
    if (!this.completeMonthAnalyticsData) {
      return { labels: [], datasets: [] };
    }

    return {
      labels: ['Active Clients', 'Churned', 'Acquisitions', 'Total Sessions'],
      datasets: [
        {
          data: [
            this.completeMonthAnalyticsData.baseClients,
            this.completeMonthAnalyticsData.churnedClients,
            this.completeMonthAnalyticsData.acquiredClients,
            this.completeMonthAnalyticsData.totalClientSessions
          ],
          backgroundColor: ['#0058be', '#6cf8bb', '#4edea3', '#2563eb'],
          borderRadius: 8,
          borderSkipped: false
        }
      ]
    };
  }

  // ---- Past month: activity heatmap rings ----

  private readonly ringCircumference = 2 * Math.PI * 28;

  get maxWeekdayMultiplier(): number {
    if (!this.completeMonthAnalyticsData?.allWeekdays.length) return 1;
    return Math.max(...this.completeMonthAnalyticsData.allWeekdays.map((weekday) => weekday.multiplier));
  }

  getWeekdayRingDashOffset(pattern: WeeklyActivityPattern): number {
    const ratio = this.maxWeekdayMultiplier > 0 ? pattern.multiplier / this.maxWeekdayMultiplier : 0;
    return this.ringCircumference * (1 - ratio);
  }

  formatWeekdayList(patterns: WeeklyActivityPattern[]): string {
    return patterns.map((pattern) => WeekDays[pattern.day]).join(', ');
  }

  weekdayLabel(day: WeekDays): string {
    return WeekDays[day];
  }

  // ---- Worktime formatting ----

  convertMinutesToDecimalHours(totalMinutes: number): number {
    return Math.round((totalMinutes / 60) * 10) / 10;
  }

  convertMinutesToHoursAndMinutes(totalMinutes: number): { hours: number; minutes: number } {
    const hours = Math.floor(totalMinutes / 60);
    const minutes = totalMinutes % 60;
    return { hours, minutes };
  }
}
