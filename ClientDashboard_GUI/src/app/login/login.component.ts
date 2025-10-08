import { Component, inject } from '@angular/core';
import { FormsModule } from '@angular/forms';
import { ButtonModule } from 'primeng/button';
import { FloatLabelModule } from 'primeng/floatlabel';
import { IftaLabelModule } from 'primeng/iftalabel';
import { InputTextModule } from 'primeng/inputtext';
import { Message } from 'primeng/message';
import { PasswordModule } from 'primeng/password';
import { LoginDto } from '../models/login-dto';
import { AccountService } from '../services/account.service';
import { routes } from '../app.routes';
import { Router, RouterLink } from '@angular/router';
import { Toast } from 'primeng/toast';
import { MessageService } from 'primeng/api';
import { ApiResponse } from '../models/api-response';
import { ToastService } from '../services/toast.service';
import { User } from '../models/user';

@Component({
  selector: 'app-login',
  imports: [InputTextModule, PasswordModule, IftaLabelModule, FormsModule, FloatLabelModule, ButtonModule, RouterLink],
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

  trainerLogin(trainerEmail: string, trainerPassword: string){
    const loginInfo: LoginDto = {
      email: trainerEmail,
      password: trainerPassword
    }
    this.accountService.login(loginInfo).subscribe({
      next: (response : ApiResponse<User>) => {
        localStorage.setItem('token', response.data?.token ?? '' );
        this.toastService.toastSummary = 'Logged In';
        this.toastService.toastDetail = 'Redirected to client-info page';
        this.toastService.showSuccess();
        this.router.navigateByUrl('client-info');
        console.log(response);
      },
      error: (response) => {
        this.toastService.toastSummary = 'Unable to log in';
        this.toastService.toastDetail = response.error.message;
        this.toastService.showError();
        console.log("Error logging in and fetching jwt token ", response.error.message);
      }
    })
  }
}
