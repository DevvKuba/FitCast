import { WeeklyMultiplier } from './weekly-multiplier';

export interface ActivityPatternsDto {
  busiestDays: WeeklyMultiplier[];
  lightDays: WeeklyMultiplier[];
}