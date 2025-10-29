import { Component } from '@angular/core';
import { RouterLink } from '@angular/router';
import { LoginComponent } from "../login/login.component";
import { ButtonDirective, ButtonModule } from "primeng/button";
import { RegisterComponent } from "../register/register.component";

@Component({
  selector: 'app-home',
  imports: [RouterLink, ButtonModule],
  templateUrl: './home.html',
  styleUrl: './home.css'
})
export class Home {
}
