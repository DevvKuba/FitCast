import { Client } from "./client";
import { UserBase } from "./user-base";

export interface Trainer extends UserBase {
  businessName?: string,
  averageSessionPrice?: number,
  emailVerified: boolean,
  calendarSyncEnabled: boolean,
  AutoPaymentSetting: boolean,
  defaultCurrency?: string,
  timeZoneId: string,
  clients: Client[];
}