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

@Component({
  selector: 'app-register',
  imports: [Message, InputTextModule, PasswordModule, IftaLabelModule, FormsModule, FloatLabelModule, ButtonModule],
  templateUrl: './register.component.html',
  styleUrl: './register.component.css'
})
export class RegisterComponent {
  email: string = "";
  firstName: string = "";
  surname: string = "";
  password: string = "";

  accountService = inject(AccountService);

  trainerRegister(trainerEmail: string, trainerFirstName: string, trainerSurname: string, trainerPassword: string){
    const registerInfo: RegisterDto = {
      email : trainerEmail, 
      firstName : trainerFirstName,
      surname: trainerSurname,
      password: trainerPassword
    }
    this.accountService.register(registerInfo).subscribe({
      next: (response) => {
        console.log(response);
      },
      error: (response) => {
        console.log(response)
      }
    });

  }
}
