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
  }

  isAuthenticated() : boolean {
    if(this.currentUser()?.token != null){
      return true;
    }
    return false;
  }

}
