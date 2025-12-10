export interface RegisterDto {
  email: string;
  firstName: string;
  surname: string;
  phoneNumber: string;
  userType: string;
  password: string;
  clientId: number | null;
  clientsTrainerId: number | null;
}