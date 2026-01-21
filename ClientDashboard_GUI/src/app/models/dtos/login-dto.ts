import { UserRole } from "../../enums/user-role";

export interface LoginDto {
  email: string;
  password: string;
  role: UserRole;
}