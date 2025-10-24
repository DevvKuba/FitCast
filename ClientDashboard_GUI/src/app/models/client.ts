import { Workout } from "./workout";

export interface Client {
  id?: number,
  name: string,
  isActive: boolean,
  currentBlockSession: number,
  totalBlockSessions: number,
  trainerId? : number,
  workouts: Workout[],
}