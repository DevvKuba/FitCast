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

@Component({
  selector: 'app-login',
  imports: [Message, InputTextModule, PasswordModule, IftaLabelModule, FormsModule, FloatLabelModule, ButtonModule],
  templateUrl: './login.component.html',
  styleUrl: './login.component.css'
})
export class LoginComponent {

  email: string = "";
  password: string = "";

  accountService = inject(AccountService);

  trainerLogin(trainerEmail: string, trainerPassword: string){
    const loginInfo: LoginDto = {
      email: trainerEmail,
      password: trainerPassword
    }
    this.accountService.login(loginInfo).subscribe({
      next: (response) => {
        localStorage.setItem('token', response.data );
      },
      error: (response) => {
        console.log("Error logging in and fetching jwt token ", response);
      }
    })

  }
}
