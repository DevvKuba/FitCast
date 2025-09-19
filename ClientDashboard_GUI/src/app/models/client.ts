import { Workout } from "./workout";

export interface Client {
  id: number,
  name: string,
  currentBlockSession: number,
  totalBlockSession: number,
  workouts: Workout[],
}