import { Component, inject } from '@angular/core';
import { FormsModule } from '@angular/forms';
import { ButtonModule } from 'primeng/button';
import { FloatLabelModule } from 'primeng/floatlabel';
import { IftaLabelModule } from 'primeng/iftalabel';
import { InputTextModule } from 'primeng/inputtext';
import { Message } from 'primeng/message';
import { PasswordModule } from 'primeng/password';
import { AccountService } from '../services/account.service';
import { RegisterDto } from '../models/register-dto';
import { Toast } from 'primeng/toast';
import { ApiResponse } from '../models/api-response';
import { ToastService } from '../services/toast.service';
import { Router } from '@angular/router';

@Component({
  selector: 'app-register',
  imports: [InputTextModule, PasswordModule, IftaLabelModule, FormsModule, FloatLabelModule, ButtonModule],
  templateUrl: './register.component.html',
  styleUrl: './register.component.css'
})
export class RegisterComponent {
  accountService = inject(AccountService);
  toastService = inject(ToastService);
  router = inject(Router);

  email: string = "";
  firstName: string = "";
  surname: string = "";
  password: string = "";

  trainerRegister(trainerEmail: string, trainerFirstName: string, trainerSurname: string, trainerPassword: string){
    const registerInfo: RegisterDto = {
      email : trainerEmail, 
      firstName : trainerFirstName,
      surname: trainerSurname,
      password: trainerPassword
    }
    this.accountService.register(registerInfo).subscribe({
      next: (response : ApiResponse<string>) => {
        this.toastService.toastSummary = 'Success Registering';
        this.toastService.toastDetail = response.message;
        this.toastService.showSuccess();
        
        console.log(response);
      },
      error: (response) => {
        this.toastService.toastSummary = 'Error Registering';
        this.toastService.toastDetail = response.error.message;
        ;
        this.toastService.showError();
        console.log(response)
      }
    });
  }
}
