import { UserBase } from "./user-base";
import { Workout } from "./workout";

export interface Client extends UserBase {
  isActive: boolean,
  currentBlockSession: number,
  totalBlockSessions: number,
  dailySteps?: number,
  weight?: number,
  trainerId? : number,
  workouts: Workout[],
}