import { UserRole } from "../../enums/user-role";

export interface RegisterDto {
  email: string;
  firstName: string;
  surname: string;
  phoneNumber: string;
  role: UserRole;
  password: string;
  confirmPassword: string;
  clientId: number | null;
  clientsTrainerId: number | null;
}