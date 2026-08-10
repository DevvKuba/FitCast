import { WeekDays } from '../../enums/weekdays';

export interface WeeklySessionsCount {
  day: WeekDays;
  totalSessions: number;
}
