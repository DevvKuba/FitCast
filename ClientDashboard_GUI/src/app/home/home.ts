import { Component } from '@angular/core';
import { RouterLink } from '@angular/router';
import { LoginComponent } from "../login/login.component";

@Component({
  selector: 'app-home',
  imports: [LoginComponent],
  templateUrl: './home.html',
  styleUrl: './home.css'
})
export class Home {

}
