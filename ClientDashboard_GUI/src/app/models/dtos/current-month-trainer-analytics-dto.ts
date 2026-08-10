import { WeeklySessionsCount } from './weekly-sessions-count';

export interface CurrentMonthTrainerAnalyticsDto {
  baseClients: number;
  monthlyClientSessions: number;
  totalRevenue: number;
  totalWorktimeMinutes: number;
  revenuePerWorkingDay: number;
  weeklySessionsCounts: WeeklySessionsCount[];
}
