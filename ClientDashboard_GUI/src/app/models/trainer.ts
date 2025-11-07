import { Client } from "./client";
import { UserBase } from "./user-base";

export interface Trainer extends UserBase {
  businessName?: string,
  averageSessionPrice?: number,
  workoutRetrievalApiKey?: string,
  defaultCurrency?: string,
  clients: Client[];
}