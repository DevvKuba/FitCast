export interface Workout {
  id: number,
  clientId: number,
  clientName: string,
  workoutTitle: string,
  sessionDate: string,
  currentBlockSession?: number,
  totalBlockSessions?: number,
  exerciseCount: number,
}