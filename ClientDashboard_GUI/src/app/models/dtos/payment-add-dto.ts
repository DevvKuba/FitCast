export interface PaymentAddDto {
  trainerId: number;
  clientId: number;
  amount: number;
  currency: string;
  numberOfSessions: number;
  paymentDate: string; 
  confirmed: boolean;
}