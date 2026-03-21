export interface PredictionResult {
  trainerId: number;
  predictedRevenue: number;
  lowerBound: number | null;
  upperBound: number | null;
  currency: string | null;
  predictedDate: Date;
  confidence: string;
  rSquared: number;
  monthsOfData: number;
  message: string;
}
