import { Component, OnInit } from '@angular/core';
import { FormsModule } from '@angular/forms';
import { PasswordModule } from 'primeng/password';
import { IftaLabelModule } from 'primeng/iftalabel';
import { ButtonModule } from 'primeng/button';
import { RouterLink } from "@angular/router";

@Component({
  selector: 'app-password-reset',
  imports: [FormsModule, PasswordModule, IftaLabelModule, ButtonModule, RouterLink],
  templateUrl: './password-reset.component.html',
  styleUrl: './password-reset.component.css'
})
export class PasswordResetComponent implements OnInit {
  newPassword: string = '';
  confirmPassword: string = '';
  userId: number = 0;

  ngOnInit(): void {
    throw new Error('Method not implemented.');
    // upon directing to this page, should let the userId
    // to the user looking to change their password
  }

  resetPassword() {
    //  password reset logic
    if (this.newPassword === this.confirmPassword) {
      console.log('Passwords match, proceed with reset');
      // Call your service 
    } else {
      console.log('Passwords do not match');
    }
  }
}
