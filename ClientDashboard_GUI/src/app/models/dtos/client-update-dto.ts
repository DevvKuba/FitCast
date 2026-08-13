export interface ClientUpdateDto {
  id: number,
  firstName: string,
  isActive: boolean,
  currentBlockSession: number,
  totalBlockSessions: number,
  phoneNumber?: string
}
