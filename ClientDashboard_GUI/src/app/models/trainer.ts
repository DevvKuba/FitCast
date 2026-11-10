import { Client } from "./client";
import { UserBase } from "./user-base";

export interface Trainer extends UserBase {
  businessName?: string,
  averageSessionPrice?: number,
  workoutRetrievalApiKey?: string,
  emailVerified: boolean,
  autoWorkoutRetrieval: boolean,
  AutoPaymentSetting: boolean,
  defaultCurrency?: string,
  timeZoneId: string,
  excludedNames: string[],
  clients: Client[];
}