import { WeeklySessionsCount } from './weekly-sessions-count';
import { MonthComparisonDto } from './month-comparison-dto';

export interface CurrentMonthTrainerAnalyticsDto {
  baseClients: number;
  monthlyClientSessions: number;
  totalRevenue: number;
  collectedRevenue: number;
  outstandingRevenue: number;
  totalWorktimeMinutes: number;
  revenuePerWorkingDay: number;
  weeklySessionsCounts: WeeklySessionsCount[];
  comparison?: MonthComparisonDto | null;
}
