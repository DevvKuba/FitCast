import { Client } from "./client";

export interface Trainer {
  id: number;
  email: string;
  firstName: string;
  surname: string;
  passwordHash: string;
  clients: Client[];
}