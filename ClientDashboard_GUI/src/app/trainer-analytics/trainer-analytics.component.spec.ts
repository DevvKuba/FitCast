import { TestBed } from '@angular/core/testing';
import { of } from 'rxjs';
import { WeekDays } from '../enums/weekdays';
import { CompleteTrainerAnalyticsDto } from '../models/dtos/complete-trainer-analytics-dto';
import { CurrentMonthTrainerAnalyticsDto } from '../models/dtos/current-month-trainer-analytics-dto';
import { AccountService } from '../services/account.service';
import { ToastService } from '../services/toast.service';
import { TrainerService } from '../services/trainer.service';

import { TrainerAnalyticsComponent } from './trainer-analytics.component';

describe('TrainerAnalyticsComponent', () => {
  let component: TrainerAnalyticsComponent;
  let trainerServiceSpy: jasmine.SpyObj<TrainerService>;
  let accountServiceMock: { currentUser: jasmine.Spy };
  let toastServiceSpy: jasmine.SpyObj<ToastService>;

  const sampleCurrentMonth: CurrentMonthTrainerAnalyticsDto = {
    baseClients: 12,
    monthlyClientSessions: 72,
    totalRevenue: 2880,
    totalWorktimeMinutes: 4320,
    revenuePerWorkingDay: 144,
    weeklySessionsCounts: [
      { day: WeekDays.Mon, totalSessions: 10 },
      { day: WeekDays.Wed, totalSessions: 14 }
    ]
  };

  const sampleCompleteMonth: CompleteTrainerAnalyticsDto = {
    baseClients: 12,
    acquiredClients: 4,
    acquisitionPercentage: 33,
    churnedClients: 2,
    churnPercentage: 16,
    netGrowth: 2,
    netGrowthPercentage: 17,
    averageSessionsPerClient: 6,
    totalClientSessions: 72,
    sessionsPrice: 40,
    monthlyWorkingDays: 20,
    totalRevenue: 2880,
    revenuePerWorkingDay: 144,
    revenuePerWorkingWeek: 720,
    totalWorktimeMinutes: 4320,
    averageDailyWorktime: 60,
    averageWeeklyWorktime: 300,
    allWeekdays: [
      { day: WeekDays.Mon, totalSessions: 10, multiplier: 1.2 },
      { day: WeekDays.Wed, totalSessions: 14, multiplier: 1.5 },
      { day: WeekDays.Sun, totalSessions: 5, multiplier: 0.9 }
    ],
    busiestDays: [{ day: WeekDays.Wed, totalSessions: 14, multiplier: 1.5 }],
    lightDays: [{ day: WeekDays.Sun, totalSessions: 5, multiplier: 0.9 }]
  };

  beforeEach(async () => {
    trainerServiceSpy = jasmine.createSpyObj<TrainerService>('TrainerService', [
      'getCurrentMonthsAnalytics',
      'getSpecificMonthAnalytics'
    ]);
    trainerServiceSpy.getCurrentMonthsAnalytics.and.returnValue(
      of({ success: true, message: 'ok', data: sampleCurrentMonth })
    );
    trainerServiceSpy.getSpecificMonthAnalytics.and.returnValue(
      of({ success: true, message: 'ok', data: sampleCompleteMonth })
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

    component = TestBed.runInInjectionContext(() => new TrainerAnalyticsComponent());
  });

  it('should create', () => {
    expect(component).toBeTruthy();
  });

  describe('view mode', () => {
    it('defaults to current month', () => {
      expect(component.viewMode).toBe('current');
    });

    it('fetches specific-month data the first time it switches to past', () => {
      component.setViewMode('past');

      expect(trainerServiceSpy.getSpecificMonthAnalytics).toHaveBeenCalledWith(
        component.selectedMonthDate.getMonth() + 1,
        component.selectedMonthDate.getFullYear()
      );
      expect(component.completeMonthAnalyticsData).toEqual(sampleCompleteMonth);
    });

    it('does not re-fetch past-month data on a second switch once already loaded', () => {
      component.setViewMode('past');
      trainerServiceSpy.getSpecificMonthAnalytics.calls.reset();

      component.setViewMode('current');
      component.setViewMode('past');

      expect(trainerServiceSpy.getSpecificMonthAnalytics).not.toHaveBeenCalled();
    });
  });

  describe('month navigation', () => {
    it('selectedMonthDate defaults to the most recent fully-completed month', () => {
      const now = new Date();
      const expected = new Date(now.getFullYear(), now.getMonth() - 1, 1);

      expect(component.selectedMonthDate.getFullYear()).toBe(expected.getFullYear());
      expect(component.selectedMonthDate.getMonth()).toBe(expected.getMonth());
    });

    it('disables moving to the next month once at the max selectable month', () => {
      expect(component.isNextMonthDisabled).toBeTrue();
    });

    it('goToPreviousMonth steps back a month and refetches', () => {
      const before = component.selectedMonthDate;

      component.goToPreviousMonth();

      expect(component.selectedMonthDate.getTime()).toBeLessThan(before.getTime());
      expect(trainerServiceSpy.getSpecificMonthAnalytics).toHaveBeenCalled();
    });

    it('goToNextMonth is a no-op when already at the max selectable month', () => {
      component.goToNextMonth();

      expect(trainerServiceSpy.getSpecificMonthAnalytics).not.toHaveBeenCalled();
    });
  });

  describe('computed chart data', () => {
    it('returns empty chart structures when the underlying data is undefined', () => {
      component.currentMonthAnalyticsData = undefined;
      component.completeMonthAnalyticsData = undefined;

      expect(component.currentMonthActivityChartData).toEqual({ labels: [], datasets: [] });
      expect(component.clientMetricsChartData).toEqual({ labels: [], datasets: [] });
    });

    it('current month activity chart maps weekday enum values to readable labels', () => {
      component.currentMonthAnalyticsData = sampleCurrentMonth;

      const chartData = component.currentMonthActivityChartData;

      expect(chartData.labels).toEqual(['Mon', 'Wed']);
      expect(chartData.datasets[0].data).toEqual([10, 14]);
    });

    it('client metrics chart maps active/churned/acquired/total sessions', () => {
      component.completeMonthAnalyticsData = sampleCompleteMonth;

      const chartData = component.clientMetricsChartData;

      expect(chartData.labels).toEqual(['Active Clients', 'Churned', 'Acquisitions', 'Total Sessions']);
      expect(chartData.datasets[0].data).toEqual([12, 2, 4, 72]);
    });
  });

  describe('activity heatmap ring helper', () => {
    it('gives the busiest day (max multiplier) a dash offset of 0', () => {
      component.completeMonthAnalyticsData = sampleCompleteMonth;

      const busiest = sampleCompleteMonth.allWeekdays.find((weekday) => weekday.multiplier === 1.5)!;

      expect(component.getWeekdayRingDashOffset(busiest)).toBeCloseTo(0, 5);
    });

    it('scales lighter days proportionally to the max multiplier', () => {
      component.completeMonthAnalyticsData = sampleCompleteMonth;

      const lightest = sampleCompleteMonth.allWeekdays.find((weekday) => weekday.multiplier === 0.9)!;
      const circumference = 2 * Math.PI * 28;
      const expectedOffset = circumference * (1 - 0.9 / 1.5);

      expect(component.getWeekdayRingDashOffset(lightest)).toBeCloseTo(expectedOffset, 5);
    });
  });

  describe('formatting helpers', () => {
    it('formatWeekdayList joins weekday names without multiplier suffixes', () => {
      const formatted = component.formatWeekdayList([
        { day: WeekDays.Mon, totalSessions: 10, multiplier: 1.2 },
        { day: WeekDays.Wed, totalSessions: 14, multiplier: 1.5 }
      ]);

      expect(formatted).toBe('Mon, Wed');
    });

    it('weekdayLabel resolves a single enum value to its short name', () => {
      expect(component.weekdayLabel(WeekDays.Fri)).toBe('Fri');
    });

    it('convertMinutesToDecimalHours rounds to one decimal place', () => {
      expect(component.convertMinutesToDecimalHours(95)).toBe(1.6);
    });

    it('convertMinutesToHoursAndMinutes splits into whole hours and remainder minutes', () => {
      expect(component.convertMinutesToHoursAndMinutes(125)).toEqual({ hours: 2, minutes: 5 });
    });
  });
});
