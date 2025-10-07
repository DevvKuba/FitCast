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
import { Router } from '@angular/router';
import { Toast } from 'primeng/toast';
import { MessageService } from 'primeng/api';
import { ApiResponse } from '../models/api-response';

@Component({
  selector: 'app-login',
  imports: [Message, InputTextModule, PasswordModule, IftaLabelModule, FormsModule, FloatLabelModule, ButtonModule, Toast],
  providers: [MessageService],
  templateUrl: './login.component.html',
  styleUrl: './login.component.css'
})
export class LoginComponent {
  messageService = inject(MessageService);
  accountService = inject(AccountService);
  router = inject(Router);

  email: string = "";
  password: string = "";
  toastSummary : string = "";
  toastDetail : string = "";

  trainerLogin(trainerEmail: string, trainerPassword: string){
    const loginInfo: LoginDto = {
      email: trainerEmail,
      password: trainerPassword
    }
    this.accountService.login(loginInfo).subscribe({
      next: (response : ApiResponse<string>) => {
        localStorage.setItem('token', response.data ?? '' );
        this.toastSummary = 'Logged In';
        this.toastDetail = 'Redirected to client-info page';
        this.router.navigateByUrl('client-info');
        console.log(response);
      },
      error: (response) => {
        this.toastSummary = 'Unable to log in';
        this.toastDetail = response.error.message;
        this.showError();
        console.log("Error logging in and fetching jwt token ", response.error.message);
      }
    })
  }

  showSuccess() {
        this.messageService.add({ severity: 'success', summary: this.toastSummary, detail: this.toastDetail });
  }

  showError() {
        this.messageService.add({ severity: 'error', summary: this.toastSummary, detail: this.toastDetail });
  }
}
