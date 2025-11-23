export interface PaymentAddDto {
  trainerId: number;
  clientId: number;
  amount: number;
  numberOfSessions: number;
  paymentDate: string; 
  confirmed: boolean;
}