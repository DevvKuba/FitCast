export interface PaymentRequestUpdateDto {
  id: number;
  amount: number;
  currency: string;
  numberOfSessions: number;
  paymentDate: string; // ISO date string format (YYYY-MM-DD)
  confirmed: boolean ;
}