import { Component } from '@angular/core';
import { CommonModule } from '@angular/common';
import { RevenuePredictionComponent } from '../revenue-prediction/revenue-prediction.component';

interface TrainerDashboardData {
  clientMetrics: {
    currentClients: number;
    acquiredLastMonth: number;
    acquiredChange: string;
    churnedLastMonth: number;
    churnedChange: string;
    netGrowth: string;
    sessionsPerClient: number;
  };
  revenuePatterns: {
    averageSessionPrice: string;
    priceTrend: string;
    monthlyWorkingDays: number;
    revenuePerWorkingDay: string;
    revenuePerWorkingWeek: string;
    revenuePerWorkingMonth: string;
  };
  activityPatterns: {
    busiestDays: string;
    lightDays: string;
    endOfMonthSurge: string;
  };
}

@Component({
  selector: 'app-trainer-analytics',
  imports: [CommonModule, RevenuePredictionComponent],
  templateUrl: './trainer-analytics.component.html',
  styleUrl: './trainer-analytics.component.css'
})
export class TrainerAnalyticsComponent {
  dashboardData: TrainerDashboardData = {
    clientMetrics: {
      currentClients: 12,
      acquiredLastMonth: 3,
      acquiredChange: '+25%',
      churnedLastMonth: 1,
      churnedChange: '-8%',
      netGrowth: '+2 (+17%)',
      sessionsPerClient: 6.2
    },
    revenuePatterns: {
      averageSessionPrice: '$65',
      priceTrend: 'Stable (0)',
      monthlyWorkingDays: 22,
      revenuePerWorkingDay: '$191',
      revenuePerWorkingWeek: '$852',
      revenuePerWorkingMonth: '$3349'
    },
    activityPatterns: {
      busiestDays: 'Mon (1.5x), Thu (1.4x)',
      lightDays: 'Sun (0.4x), Sat (0.4x)',
      endOfMonthSurge: '+30% last week'
    }
  };

}
