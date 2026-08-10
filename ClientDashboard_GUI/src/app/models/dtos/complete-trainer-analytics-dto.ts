import { WeeklyActivityPattern } from './weekly-activity-pattern';

export interface CompleteTrainerAnalyticsDto {
  baseClients: number;
  acquiredClients: number;
  acquisitionPercentage: number;
  churnedClients: number;
  churnPercentage: number;
  netGrowth: number;
  netGrowthPercentage: number;
  averageSessionsPerClient: number;
  totalClientSessions: number;
  sessionsPrice: number;
  monthlyWorkingDays: number;
  totalRevenue: number;
  revenuePerWorkingDay: number;
  revenuePerWorkingWeek: number;
  totalWorktimeMinutes: number;
  averageDailyWorktime: number;
  averageWeeklyWorktime: number;
  allWeekdays: WeeklyActivityPattern[];
  busiestDays: WeeklyActivityPattern[];
  lightDays: WeeklyActivityPattern[];
}