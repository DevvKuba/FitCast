import { Component, inject } from '@angular/core';
import { FormsModule } from '@angular/forms';
import { ButtonModule } from 'primeng/button';
import { FloatLabelModule } from 'primeng/floatlabel';
import { IftaLabelModule } from 'primeng/iftalabel';
import { InputTextModule } from 'primeng/inputtext';
import { Message } from 'primeng/message';
import { PasswordModule } from 'primeng/password';
import { LoginDto } from '../models/dtos/login-dto';
import { AccountService } from '../services/account.service';
import { routes } from '../app.routes';
import { Router, RouterLink } from '@angular/router';
import { Toast } from 'primeng/toast';
import { MessageService } from 'primeng/api';
import { ApiResponse } from '../models/api-response';
import { ToastService } from '../services/toast.service';
import { UserDto } from '../models/dtos/user-dto';
import { RadioButton } from 'primeng/radiobutton';
import { ToggleButton } from 'primeng/togglebutton';

@Component({
  selector: 'app-login',
  imports: [InputTextModule, PasswordModule, IftaLabelModule, FormsModule,
     FloatLabelModule, ButtonModule, RouterLink, RadioButton, ToggleButton],
  templateUrl: './login.component.html',
  styleUrl: './login.component.css'
})
export class LoginComponent {
  messageService = inject(MessageService);
  accountService = inject(AccountService);
  toastService = inject(ToastService);
  router = inject(Router);

  email: string = "";
  password: string = "";
  userType: string = "";
  storageItem = "token";

  trainerLogin(trainerEmail: string, trainerPassword: string){
    const loginInfo: LoginDto = {
      email: trainerEmail,
      password: trainerPassword
    }
    this.accountService.login(loginInfo).subscribe({
      next: (response : ApiResponse<UserDto>) => {
        localStorage.setItem(this.storageItem, response.data?.token ?? '' );
        this.accountService.currentUser.set(response.data ?? null);
        
        console.log("User logged in: ", this.accountService.currentUser()?.firstName);
        this.toastService.showSuccess('Logged In','Redirected to client-info page' );
        this.router.navigateByUrl('client-info');
      },
      error: (response) => {
        this.toastService.showError('Unable to log in', response.error.message);
        console.log("Error logging in and fetching jwt token ", response.error.message);
      }
    })
  }

  trainerLogout(storageItem: string){
    this.accountService.logout(storageItem);
    this.accountService.currentUser.set(null);
    console.log("User logged out, current user is now: ", this.accountService.currentUser());
    this.router.navigateByUrl('');
  }
}
