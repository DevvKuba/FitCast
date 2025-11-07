export interface UserBase {
  id: number,
  firstName: string, 
  email?: string,
  surname?: string,
  photoUrl?: string,
  phoneNumber?: string,
  passwordHash?: string;
}