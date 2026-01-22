import { UserRole } from "../../enums/user-role";

export interface UserDto {
  firstName: string,
  id : number,
  token: string,
  role: UserRole
}