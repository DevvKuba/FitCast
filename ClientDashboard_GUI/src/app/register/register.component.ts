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
import { MessageService } from 'primeng/api';

@Component({
  selector: 'app-register',
  imports: [Message, InputTextModule, PasswordModule, IftaLabelModule, FormsModule, FloatLabelModule, ButtonModule, Toast],
   providers: [MessageService],
  templateUrl: './register.component.html',
  styleUrl: './register.component.css'
})
export class RegisterComponent {
  accountService = inject(AccountService);
  messageService = inject(MessageService);

  email: string = "";
  firstName: string = "";
  surname: string = "";
  password: string = "";
  toastSummary : string = "";
  toastDetail : string = "";

  trainerRegister(trainerEmail: string, trainerFirstName: string, trainerSurname: string, trainerPassword: string){
    const registerInfo: RegisterDto = {
      email : trainerEmail, 
      firstName : trainerFirstName,
      surname: trainerSurname,
      password: trainerPassword
    }
    this.accountService.register(registerInfo).subscribe({
      next: (response) => {
        this.toastSummary = 'Success Registering';
        this.toastDetail = 'Proceed to the login page';
        this.showSuccess();
        console.log(response);
      },
      error: (response) => {
        this.toastSummary = 'Error Registering';
        this.toastDetail = 'Unsuccessfully trying to register';
        this.showError();
        console.log(response)
      }
    });
  }

   showSuccess() {
        this.messageService.add({ severity: 'success', summary: this.toastSummary, detail: this.toastDetail });
  }

  showError() {
        this.messageService.add({ severity: 'error', summary: this.toastSummary, detail: this.toastDetail });
  }
}
