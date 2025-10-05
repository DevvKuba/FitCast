import { Component } from '@angular/core';
import { FormsModule } from '@angular/forms';
import { ButtonModule } from 'primeng/button';
import { FloatLabelModule } from 'primeng/floatlabel';
import { IftaLabelModule } from 'primeng/iftalabel';
import { InputTextModule } from 'primeng/inputtext';
import { Message } from 'primeng/message';
import { PasswordModule } from 'primeng/password';

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

  trainerRegister(email: string, firstName: string, surname: string, password: string){

  }
}
