import { Workout } from "./workout";

export interface Client {
  id: number,
  name: string,
  currentBlockSession: number,
  totalBlockSessions: number,
  workouts: Workout[],
}