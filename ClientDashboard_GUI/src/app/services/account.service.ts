import { HttpClient } from '@angular/common/http';
import { HttpParams } from '@angular/common/http';
import { inject, Injectable, signal } from '@angular/core';
import { ApiResponse } from '../models/api-response';
import { Observable } from 'rxjs';
import { RegisterDto } from '../models/dtos/register-dto';
import { LoginDto } from '../models/dtos/login-dto';
import { UserDto } from '../models/dtos/user-dto';
import { environment } from '../environments/environment';
import { ClientVerificationInfoDto } from '../models/dtos/client-verification-info-dto';

@Injectable({
  providedIn: 'root'
})
export class AccountService {
  http = inject(HttpClient);
  currentUser = signal<UserDto | null>(null);
  baseUrl = environment.apiUrl;

  register(registerInfo : RegisterDto) : Observable<ApiResponse<any>>{
  return this.http.post<ApiResponse<string>>(this.baseUrl + "account/register", registerInfo);
 }

  login(loginInfo : LoginDto) : Observable<ApiResponse<any>>{
    return this.http.post<ApiResponse<UserDto>>(this.baseUrl + "account/login", loginInfo);
  }

  clientVerifyUnderTrainer(trainerPhoneNumber: string, clientFirstName: string) : Observable<any>{
    const params = new HttpParams()
    .set('trainerPhoneNumber', trainerPhoneNumber)
    .set('clientFirstName', clientFirstName);
    const url = `${this.baseUrl}account/verifyClientUnderTrainer`;

    return this.http.get<ApiResponse<ClientVerificationInfoDto>>(url, {params})
  }

  logout(storageItem: string){
    localStorage.removeItem(storageItem);
    this.currentUser.set(null);
  }

  isAuthenticated() : boolean {
    if(this.currentUser()?.token != null){
      return true;
    }
    return false;
  }

   initializeAuthState(): void {
    const token = localStorage.getItem('token');

    if(token && this.isTokenValid(token)){
      const userInfo = this.extractUserFromToken(token);
      this.currentUser.set(userInfo);
    }
  }

  private extractUserFromToken(token: string) : UserDto{
    try {
      const payload = JSON.parse(atob(token.split('.')[1]));
      return{
        id: payload.sub,
        firstName: payload.given_name,
        token: token
      };
    }
    catch(error){
      throw new Error('Invalid token format');
    }
  }

  private isTokenValid(token : string) : boolean{
    try {
      //decode processes to validate token
      const payload = JSON.parse(atob(token.split('.')[1]));
      const currentTime = Math.floor(Date.now() / 1000);

      // check if token hasn't expired
      return payload.exp > currentTime;
    }
    catch (error) {
      return false;
    }
  }

}
