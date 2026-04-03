import { WeeklyMultiplier } from './weekly-multiplier';

export interface CompleteTrainerAnalyticsDto {
  baseClients: number;
  acquiredClients: number;
  acquisitionPercentage: number;
  churnedClients: number;
  churnPercentage: number;
  netGrowth: number;
  netGrowthPercentage: number;
  sessionsPerClient: number;
  monthlyClientSessions: number;
  sessionsPrice: number;
  monthlyWorkingDays: number;
  revenuePerWorkingDay: number;
  revenuePerWorkingWeek: number;
  revenuePerWorkingMonth: number;
  busiestDays: WeeklyMultiplier[];
  lightDays: WeeklyMultiplier[];
}