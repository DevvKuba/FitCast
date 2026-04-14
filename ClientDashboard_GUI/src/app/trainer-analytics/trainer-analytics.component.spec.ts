import { TestBed } from '@angular/core/testing';
import { of } from 'rxjs';
import { WeekDays } from '../enums/weekdays';
import { CompleteTrainerAnalyticsDto } from '../models/dtos/complete-trainer-analytics-dto';
import { AccountService } from '../services/account.service';
import { ToastService } from '../services/toast.service';
import { TrainerService } from '../services/trainer.service';

import { TrainerAnalyticsComponent } from './trainer-analytics.component';

describe('TrainerAnalyticsComponent', () => {
  let component: TrainerAnalyticsComponent;
  let trainerServiceSpy: jasmine.SpyObj<TrainerService>;
  let accountServiceMock: { currentUser: jasmine.Spy };
  let toastServiceSpy: jasmine.SpyObj<ToastService>;

  const sampleAnalytics: CompleteTrainerAnalyticsDto = {
    baseClients: 12,
    acquiredClients: 4,
    acquisitionPercentage: 33,
    churnedClients: 2,
    churnPercentage: 16,
    netGrowth: 2,
    netGrowthPercentage: 17,
    sessionsPerClient: 6,
    monthlyClientSessions: 72,
    sessionsPrice: 40,
    monthlyWorkingDays: 20,
    revenuePerWorkingDay: 144,
    revenuePerWorkingWeek: 720,
    revenuePerWorkingMonth: 2880,
    allWeekdays: [
      { day: WeekDays.Mon, multiplier: 1.2 },
      { day: WeekDays.Wed, multiplier: 1.5 },
      { day: WeekDays.Sun, multiplier: 0.9 }
    ],
    busiestDays: [{ day: WeekDays.Wed, multiplier: 1.5 }],
    lightDays: [{ day: WeekDays.Sun, multiplier: 0.9 }]
  };

  beforeEach(async () => {
    trainerServiceSpy = jasmine.createSpyObj<TrainerService>('TrainerService', [
      'getLastMonthsAnalytics',
      'getFullMonthsAnalytics'
    ]);
    trainerServiceSpy.getLastMonthsAnalytics.and.returnValue(
      of({ success: true, message: 'ok', data: sampleAnalytics })
    );
    trainerServiceSpy.getFullMonthsAnalytics.and.returnValue(
      of({ success: true, message: 'ok', data: sampleAnalytics })
    );

    accountServiceMock = {
      currentUser: jasmine.createSpy('currentUser').and.returnValue({ id: 22 })
    };

    toastServiceSpy = jasmine.createSpyObj<ToastService>('ToastService', ['showError']);

    TestBed.configureTestingModule({
      providers: [
        { provide: TrainerService, useValue: trainerServiceSpy },
        { provide: AccountService, useValue: accountServiceMock },
        { provide: ToastService, useValue: toastServiceSpy }
      ]
    });

    // This keeps tests focused on computation logic and avoids template dependency noise.
    component = TestBed.runInInjectionContext(() => new TrainerAnalyticsComponent());
  });

  it('should create', () => {
    expect(component).toBeTruthy();
  });

  describe('computed chart data', () => {
    it('returns empty chart structures when analyticsData is undefined', () => {
      component.analyticsData = undefined;

      expect(component.clientMetricsChartData).toEqual({ labels: [], datasets: [] });
      expect(component.revenuePatternsChartData).toEqual({ labels: [], datasets: [] });
      expect(component.activityPatternsChartData).toEqual({ labels: [], datasets: [] });
    });

    it('client metrics labels use "Monthly sessions" when scope is lastMonth', () => {
      component.selectedScope = 'lastMonth';
      component.analyticsData = sampleAnalytics;

      const chartData = component.clientMetricsChartData;

      expect(chartData.labels).toEqual([
        'Base clients',
        'Sessions per client',
        'Monthly sessions'
      ]);
      expect(chartData.datasets[0].data).toEqual([12, 6, 72]);
    });

    it('client metrics labels use "Average monthly sessions" when scope is allData', () => {
      component.selectedScope = 'allData';
      component.analyticsData = sampleAnalytics;

      const chartData = component.clientMetricsChartData;

      expect(chartData.labels).toEqual([
        'Base clients',
        'Sessions per client',
        'Average monthly sessions'
      ]);
    });

    it('revenue chart maps day/week/month values correctly', () => {
      component.analyticsData = sampleAnalytics;

      const chartData = component.revenuePatternsChartData;

      expect(chartData.labels).toEqual(['Revenue / day', 'Revenue / week', 'Revenue / month']);
      expect(chartData.datasets[0].data).toEqual([144, 720, 2880]);
    });

    it('activity chart maps weekday enum values to readable labels', () => {
      component.analyticsData = sampleAnalytics;

      const chartData = component.activityPatternsChartData;

      expect(chartData.labels).toEqual(['Mon', 'Wed', 'Sun']);
      expect(chartData.datasets[0].data).toEqual([1.2, 1.5, 0.9]);
    });
  });

  describe('formatting helpers', () => {
    it('formatWeeklyMultipliers creates a comma-separated summary string', () => {
      const formatted = component.formatWeeklyMultipliers([
        { day: WeekDays.Mon, multiplier: 1.2 },
        { day: WeekDays.Wed, multiplier: 1.5 }
      ]);

      expect(formatted).toBe('Mon (1.2x), Wed (1.5x)');
    });

    it('formatWeeklyMultiplier formats one weekday multiplier item', () => {
      const formatted = component.formatWeeklyMultiplier({
        day: WeekDays.Fri,
        multiplier: 1.1
      });

      expect(formatted).toBe('Fri - (1.1x)');
    });
  });

  describe('scope behavior', () => {
    it('setMetricScope updates scope and triggers data refresh', () => {
      const retrieveSpy = spyOn(component, 'retrieveAnalytics');

      component.setMetricScope('allData');

      expect(component.selectedScope).toBe('allData');
      expect(retrieveSpy).toHaveBeenCalled();
    });
  });

  describe('documentation: how these tests operate', () => {
    it('validates chart getter outputs by assigning analyticsData directly (pure computation tests)', () => {
      component.analyticsData = sampleAnalytics;

      const labels = component.activityPatternsChartData.labels;

      expect(labels).toEqual(['Mon', 'Wed', 'Sun']);
      expect(component.activityPatternsChartData.datasets.length).toBe(1);
    });
  });
});
