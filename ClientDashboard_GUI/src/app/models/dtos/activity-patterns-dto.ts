import { WeeklyActivityPattern } from './weekly-activity-pattern';

export interface ActivityPatternsDto {
  busiestDays: WeeklyActivityPattern[];
  lightDays: WeeklyActivityPattern[];
}