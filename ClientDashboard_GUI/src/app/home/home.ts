import { Component } from '@angular/core';
import { RouterLink } from '@angular/router';
import { LoginComponent } from "../login/login.component";
import { ButtonDirective, ButtonModule } from "primeng/button";
import { RegisterComponent } from "../register/register.component";

@Component({
  selector: 'app-home',
  imports: [LoginComponent, RouterLink, ButtonDirective, ButtonModule, RegisterComponent],
  templateUrl: './home.html',
  styleUrl: './home.css'
})
export class Home {
  isLoggingIn: boolean = true;

  toggleToRegisterPage(){
    this.isLoggingIn  = false;
  }

  toggleToLoginPage(){
    this.isLoggingIn  = true;
  }
}
