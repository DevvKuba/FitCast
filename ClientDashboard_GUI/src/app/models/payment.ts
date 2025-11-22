export interface Payment {
  id: number;
  trainerId: number;
  clientId: number | null;
  amount: number;
  currency: string;
  numberOfSessions: number;
  paymentDate: string; 
  confirmed: boolean;
}