import { WeeklyActivityPattern } from './weekly-activity-pattern';
import { MonthComparisonDto } from './month-comparison-dto';

export interface CompleteTrainerAnalyticsDto {
  recordedDays: number;
  isPartialMonth: boolean;
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
  collectedRevenue: number;
  outstandingRevenue: number;
  revenuePerWorkingDay: number;
  revenuePerWorkingWeek: number;
  totalWorktimeMinutes: number;
  averageDailyWorktime: number;
  averageWeeklyWorktime: number;
  allWeekdays: WeeklyActivityPattern[];
  busiestDays: WeeklyActivityPattern[];
  lightDays: WeeklyActivityPattern[];
  comparison?: MonthComparisonDto | null;
}