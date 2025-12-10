import { Component } from '@angular/core';
import { RouterLink } from '@angular/router';
import { LoginComponent } from "../login/login.component";
import { ButtonDirective, ButtonModule } from "primeng/button";
import { RegisterComponent } from "../register/register.component";
import { HomeNavbarComponent } from "../home-navbar/home-navbar.component";

@Component({
  selector: 'app-home',
  imports: [ButtonModule, HomeNavbarComponent],
  templateUrl: './home.html',
  styleUrl: './home.css'
})
export class Home {
}
