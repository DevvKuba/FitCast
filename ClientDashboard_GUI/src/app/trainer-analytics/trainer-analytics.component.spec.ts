import { TestBed } from '@angular/core/testing';
import { of } from 'rxjs';
import { WeekDays } from '../enums/weekdays';
import { CompleteTrainerAnalyticsDto } from '../models/dtos/complete-trainer-analytics-dto';
import { CurrentMonthTrainerAnalyticsDto } from '../models/dtos/current-month-trainer-analytics-dto';
import { AnalyticsMonthDto } from '../models/dtos/analytics-month-dto';
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
    collectedRevenue: 2400,
    outstandingRevenue: 480,
    totalWorktimeMinutes: 4320,
    revenuePerWorkingDay: 144,
    weeklySessionsCounts: [
      { day: WeekDays.Mon, totalSessions: 10 },
      { day: WeekDays.Wed, totalSessions: 14 }
    ]
  };

  const sampleCompleteMonth: CompleteTrainerAnalyticsDto = {
    recordedDays: 31,
    isPartialMonth: false,
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
    collectedRevenue: 2400,
    outstandingRevenue: 480,
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

  // Oldest first, as the API returns them; September is only partly recorded
  const sampleMonths: AnalyticsMonthDto[] = [
    { year: 2026, month: 9, recordedDays: 12, isPartial: true },
    { year: 2026, month: 10, recordedDays: 31, isPartial: false }
  ];

  beforeEach(async () => {
    trainerServiceSpy = jasmine.createSpyObj<TrainerService>('TrainerService', [
      'getCurrentMonthsAnalytics',
      'getSpecificMonthAnalytics',
      'getAnalyticsMonths',
      'retrieveTrainerById'
    ]);
    trainerServiceSpy.getCurrentMonthsAnalytics.and.returnValue(
      of({ success: true, message: 'ok', data: sampleCurrentMonth })
    );
    trainerServiceSpy.getSpecificMonthAnalytics.and.returnValue(
      of({ success: true, message: 'ok', data: sampleCompleteMonth })
    );
    trainerServiceSpy.getAnalyticsMonths.and.returnValue(
      of({ success: true, message: 'ok', data: sampleMonths })
    );
    trainerServiceSpy.retrieveTrainerById.and.returnValue(
      of({ success: true, message: 'ok', data: { defaultCurrency: 'EUR' } })
    );

    accountServiceMock = {
      currentUser: jasmine.createSpy('currentUser').and.returnValue({ id: 22 })
    };

    toastServiceSpy = jasmine.createSpyObj<ToastService>('ToastService', ['showError', 'showNeutral']);

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

    it('loads the recorded months and opens the latest one the first time it switches to past', () => {
      component.setViewMode('past');

      expect(trainerServiceSpy.getAnalyticsMonths).toHaveBeenCalledTimes(1);
      expect(trainerServiceSpy.getSpecificMonthAnalytics).toHaveBeenCalledWith(10, 2026);
      expect(component.completeMonthAnalyticsData).toEqual(sampleCompleteMonth);
    });

    it('does not reload the months on a second switch once already loaded', () => {
      component.setViewMode('past');
      trainerServiceSpy.getAnalyticsMonths.calls.reset();
      trainerServiceSpy.getSpecificMonthAnalytics.calls.reset();

      component.setViewMode('current');
      component.setViewMode('past');

      expect(trainerServiceSpy.getAnalyticsMonths).not.toHaveBeenCalled();
      expect(trainerServiceSpy.getSpecificMonthAnalytics).not.toHaveBeenCalled();
    });

    it('selects no month when nothing has been recorded yet', () => {
      trainerServiceSpy.getAnalyticsMonths.and.returnValue(of({ success: true, message: 'ok', data: [] }));

      component.setViewMode('past');

      expect(component.analyticsMonthsLoaded).toBeTrue();
      expect(component.selectedMonth).toBeNull();
      expect(trainerServiceSpy.getSpecificMonthAnalytics).not.toHaveBeenCalled();
    });
  });

  describe('month navigation', () => {
    beforeEach(() => {
      component.setViewMode('past');
      trainerServiceSpy.getSpecificMonthAnalytics.calls.reset();
    });

    it('disables the next arrow on the latest recorded month', () => {
      expect(component.isNextMonthDisabled).toBeTrue();
      expect(component.isPreviousMonthDisabled).toBeFalse();
    });

    it('goToPreviousMonth moves to the previous recorded month and refetches', () => {
      component.goToPreviousMonth();

      expect(component.selectedMonth).toEqual(sampleMonths[0]);
      expect(trainerServiceSpy.getSpecificMonthAnalytics).toHaveBeenCalledWith(9, 2026);
      expect(component.isPreviousMonthDisabled).toBeTrue();
    });

    it('goToNextMonth is a no-op on the latest recorded month', () => {
      component.goToNextMonth();

      expect(trainerServiceSpy.getSpecificMonthAnalytics).not.toHaveBeenCalled();
    });

    it('picking a month without analytics keeps the current month and says so', () => {
      component.onMonthPickerSelect(new Date(2026, 7, 1));

      expect(toastServiceSpy.showNeutral).toHaveBeenCalled();
      expect(component.selectedMonth).toEqual(sampleMonths[1]);
      expect(trainerServiceSpy.getSpecificMonthAnalytics).not.toHaveBeenCalled();
    });

    it('picking a recorded month loads it', () => {
      component.onMonthPickerSelect(new Date(2026, 8, 15));

      expect(trainerServiceSpy.getSpecificMonthAnalytics).toHaveBeenCalledWith(9, 2026);
    });

    it('reports how many of a partial month\'s days were recorded', () => {
      component.goToPreviousMonth();

      expect(component.selectedMonth?.isPartial).toBeTrue();
      expect(component.selectedMonthTotalDays).toBe(30);
    });
  });

  describe('change indicators', () => {
    it('colours rises green, drops red and no change neutral', () => {
      expect(component.getChangeColourClass(3)).toBe('text-success-emerald');
      expect(component.getChangeColourClass(-2)).toBe('text-error-rose');
      expect(component.getChangeColourClass(0)).toBe('text-on-surface-variant');
    });

    it('reverses the colours for churn, where fewer is better', () => {
      expect(component.getChangeColourClass(-1, false)).toBe('text-success-emerald');
      expect(component.getChangeColourClass(2, false)).toBe('text-error-rose');
    });

    it('formats counts, revenue and worktime with a sign', () => {
      expect(component.formatCountChange(3)).toBe('+3');
      expect(component.formatCountChange(-2)).toBe('-2');
      expect(component.formatRevenueChange(-120)).toBe('-£120');
      expect(component.formatWorktimeChange(135)).toBe('+2h 15m');
    });

    it('uses the trainer\'s currency once it has loaded', () => {
      component.retrieveTrainerCurrency();

      expect(component.currencyCode).toBe('EUR');
      expect(component.formatRevenueChange(50)).toBe('+€50');
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
