import { Component, inject, OnInit } from '@angular/core';
import { FormsModule } from '@angular/forms';
import { PasswordModule } from 'primeng/password';
import { IftaLabelModule } from 'primeng/iftalabel';
import { ButtonModule } from 'primeng/button';
import { ActivatedRoute, Router, RouterLink } from "@angular/router";

@Component({
  selector: 'app-password-reset',
  imports: [FormsModule, PasswordModule, IftaLabelModule, ButtonModule, RouterLink],
  templateUrl: './password-reset.component.html',
  styleUrl: './password-reset.component.css'
})
export class PasswordResetComponent implements OnInit {
  newPassword: string = '';
  confirmPassword: string = '';

  route = inject(ActivatedRoute);
  tokenId: number = 0;

  ngOnInit(): void {
    this.tokenId = Number(this.route.snapshot.queryParamMap.get('token'));
    console.log(this.tokenId);
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
