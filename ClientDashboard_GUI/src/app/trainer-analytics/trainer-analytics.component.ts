import { Component, inject, LOCALE_ID, OnInit } from '@angular/core';
import { CommonModule, formatCurrency, getCurrencySymbol } from '@angular/common';
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
import { AnalyticsMonthDto } from '../models/dtos/analytics-month-dto';

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
  locale = inject(LOCALE_ID);

  viewMode: AnalyticsViewMode = 'current';

  currentMonthAnalyticsData: CurrentMonthTrainerAnalyticsDto | undefined;
  completeMonthAnalyticsData : CompleteTrainerAnalyticsDto | undefined;

  // Past months with recorded analytics, oldest first - the picker and arrows only move between these
  analyticsMonths: AnalyticsMonthDto[] = [];
  analyticsMonthsLoaded = false;
  selectedMonthIndex = -1;
  selectedMonthDate: Date | null = null;

  currencyCode = 'GBP';

  ngOnInit(): void {
   this.retrieveCurrentMonthAnalytics();
   this.retrieveTrainerCurrency();
  }

  setViewMode(mode: AnalyticsViewMode): void {
    this.viewMode = mode;

    if (mode === 'past' && !this.analyticsMonthsLoaded) {
      this.retrieveAnalyticsMonths();
    }
  }

  get selectedMonth(): AnalyticsMonthDto | null {
    return this.selectedMonthIndex >= 0 ? this.analyticsMonths[this.selectedMonthIndex] : null;
  }

  get selectedMonthTotalDays(): number {
    if (!this.selectedMonth) return 0;
    return new Date(this.selectedMonth.year, this.selectedMonth.month, 0).getDate();
  }

  get firstSelectableMonthDate(): Date | null {
    return this.analyticsMonths.length > 0 ? this.toMonthDate(this.analyticsMonths[0]) : null;
  }

  get lastSelectableMonthDate(): Date | null {
    return this.analyticsMonths.length > 0 ? this.toMonthDate(this.analyticsMonths[this.analyticsMonths.length - 1]) : null;
  }

  get isPreviousMonthDisabled(): boolean {
    return this.selectedMonthIndex <= 0;
  }

  get isNextMonthDisabled(): boolean {
    return this.selectedMonthIndex >= this.analyticsMonths.length - 1;
  }

  goToPreviousMonth(): void {
    if (this.isPreviousMonthDisabled) return;
    this.selectMonth(this.selectedMonthIndex - 1);
  }

  goToNextMonth(): void {
    if (this.isNextMonthDisabled) return;
    this.selectMonth(this.selectedMonthIndex + 1);
  }

  onMonthPickerSelect(date: Date): void {
    const index = this.analyticsMonths.findIndex((month) => month.year === date.getFullYear() && month.month === date.getMonth() + 1);

    if (index === -1) {
      this.toastService.showNeutral('No analytics recorded', 'No sessions were recorded that month');
      // Resets the picker back to the month still being shown
      this.selectedMonthDate = this.selectedMonth ? this.toMonthDate(this.selectedMonth) : null;
      return;
    }

    this.selectMonth(index);
  }

  retrieveAnalyticsMonths(): void {
    this.trainerService.getAnalyticsMonths().subscribe({
      next: (response) => {
        this.analyticsMonths = response.data ?? [];
        this.analyticsMonthsLoaded = true;

        if (this.analyticsMonths.length > 0) {
          this.selectMonth(this.analyticsMonths.length - 1);
        }
      },
      error: (response) => {
        this.toastService.showError('Error getting past months', response.error?.message);
      }
    })
  }

  retrieveTrainerCurrency(): void {
    this.trainerService.retrieveTrainerById().subscribe({
      next: (response) => {
        this.currencyCode = response.data?.defaultCurrency || 'GBP';
      }
    })
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

  // ---- Month-on-month change indicators ----

  get hasCurrentMonthData(): boolean {
    return (this.currentMonthAnalyticsData?.recordedDays ?? 0) > 0;
  }

  // Fewer churned clients is the improvement, so churn passes higherIsBetter = false
  getChangeColourClass(change: number, higherIsBetter: boolean = true): string {
    if (change === 0) return 'text-on-surface-variant';

    const isImprovement = higherIsBetter ? change > 0 : change < 0;
    return isImprovement ? 'text-success-emerald' : 'text-error-rose';
  }

  getChangeIcon(change: number): string {
    if (change === 0) return 'pi pi-minus';
    return change > 0 ? 'pi pi-arrow-up' : 'pi pi-arrow-down';
  }

  formatCountChange(change: number): string {
    return this.changeSign(change) + Math.abs(change);
  }

  formatRevenueChange(change: number): string {
    const symbol = getCurrencySymbol(this.currencyCode, 'narrow');
    return this.changeSign(change) + formatCurrency(Math.abs(change), this.locale, symbol, this.currencyCode, '1.0-2');
  }

  formatWorktimeChange(changeMinutes: number): string {
    const time = this.convertMinutesToHoursAndMinutes(Math.abs(changeMinutes));
    return `${this.changeSign(changeMinutes)}${time.hours}h ${time.minutes}m`;
  }

  private selectMonth(index: number): void {
    this.selectedMonthIndex = index;
    const month = this.analyticsMonths[index];
    this.selectedMonthDate = this.toMonthDate(month);
    this.retrieveSpecificMonthAnalytics(month.month, month.year);
  }

  private changeSign(change: number): string {
    if (change > 0) return '+';
    if (change < 0) return '-';
    return '';
  }

  // The API's months are 1-12, JS Date months are 0-11
  private toMonthDate(month: AnalyticsMonthDto): Date {
    return new Date(month.year, month.month - 1, 1);
  }
}
