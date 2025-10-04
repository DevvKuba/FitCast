import { HttpClient } from '@angular/common/http';
import { inject, Injectable } from '@angular/core';
import { ApiResponse } from '../models/api-response';
import { Observable } from 'rxjs';
import { RegisterDto } from '../models/register-dto';
import { LoginDto } from '../models/login-dto';

@Injectable({
  providedIn: 'root'
})
export class AccountService {
  http = inject(HttpClient);

  register(registerInfo : RegisterDto) : Observable<ApiResponse<any>>{
    return this.http.post<ApiResponse<string>>("https://clientdashboardapp-dfdja3c4hxffdsg0.uksouth-01.azurewebsites.net/register", registerInfo);
  }

  login(loginInfo : LoginDto) : Observable<ApiResponse<any>>{
    return this.http.post<ApiResponse<string>>("https://clientdashboardapp-dfdja3c4hxffdsg0.uksouth-01.azurewebsites.net/login", loginInfo);
  }
}
