export interface Workout {
  id: number,
  clientId: number,
  clientName: string,
  workoutTitle: string,
  sessionDate: Date,
  currentBlockSession: number,
  totalBlockSession: number,
  exerciseCount: number,
}