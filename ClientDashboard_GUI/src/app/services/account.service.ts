import { HttpClient } from '@angular/common/http';
import { inject, Injectable, signal } from '@angular/core';
import { ApiResponse } from '../models/api-response';
import { Observable } from 'rxjs';
import { RegisterDto } from '../models/register-dto';
import { LoginDto } from '../models/login-dto';
import { UserDto } from '../models/user-dto';
import { environment } from '../environments/environment';

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
