import { WeekDays } from "../../enums/weekdays";

export interface WeeklyActivityPattern {
  day: WeekDays;
  totalSessions: number;
  multiplier: number;
}