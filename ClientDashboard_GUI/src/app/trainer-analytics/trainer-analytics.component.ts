import { Component } from '@angular/core';
import { CommonModule } from '@angular/common';
import { RevenuePredictionComponent } from '../revenue-prediction/revenue-prediction.component';
import { ActivityPatternsDto } from '../models/dtos/activity-patterns-dto';
import { ClientMetricsDto } from '../models/dtos/client-metrics-dto';
import { RevenuePatternsDto } from '../models/dtos/revenue-patterns-dto';
import { WeeklyMultiplier } from '../models/dtos/weekly-multiplier';

@Component({
  selector: 'app-trainer-analytics',
  imports: [CommonModule, RevenuePredictionComponent],
  templateUrl: './trainer-analytics.component.html',
  styleUrl: './trainer-analytics.component.css'
})
export class TrainerAnalyticsComponent {
  selectedScope: 'lastMonth' | 'allData' = 'lastMonth';

  clientMetrics: ClientMetricsDto = {
    baseClients: 12,
    acquiredClients: 3,
    acquisitionPercentage: 25,
    churnedClients: 1,
    churnPercentage: -8,
    netGrowth: 2,
    netGrowthPercentage: 17,
    sessionsPerClient: 6,
    monthlyClientSessions: 82
  };

  revenuePatterns: RevenuePatternsDto = {
    sessionsPrice: 65,
    monthlyWorkingDays: 22,
    revenuePerWorkingDay: 191,
    revenuePerWorkingWeek: 852,
    revenuePerWorkingMonth: 3349
  };

  activityPatterns: ActivityPatternsDto = {
    busiestDays: [
      { weekday: 'Mon', multiplier: 1.5 },
      { weekday: 'Thu', multiplier: 1.4 }
    ],
    lightDays: [
      { weekday: 'Sun', multiplier: 0.4 },
      { weekday: 'Sat', multiplier: 0.4 }
    ]
  };
  dashboardData = {
    clientMetrics: this.clientMetrics,
    revenuePatterns: this.revenuePatterns,
    activityPatterns: this.activityPatterns
  };

  setMetricScope(scope: 'lastMonth' | 'allData'): void {
    this.selectedScope = scope;
  }

  formatWeeklyMultipliers(values: WeeklyMultiplier[]): string {
    return values.map((value) => `${value.weekday} (${value.multiplier}x)`).join(', ');
  }

}
