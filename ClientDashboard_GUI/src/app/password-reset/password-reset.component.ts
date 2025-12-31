import { Component, inject, OnInit } from '@angular/core';
import { FormsModule } from '@angular/forms';
import { PasswordModule } from 'primeng/password';
import { IftaLabelModule } from 'primeng/iftalabel';
import { ButtonModule } from 'primeng/button';
import { ActivatedRoute, Router, RouterLink } from "@angular/router";
import { AccountService } from '../services/account.service';
import { ToastService } from '../services/toast.service';

@Component({
  selector: 'app-password-reset',
  imports: [FormsModule, PasswordModule, IftaLabelModule, ButtonModule, RouterLink],
  templateUrl: './password-reset.component.html',
  styleUrl: './password-reset.component.css'
})
export class PasswordResetComponent implements OnInit {
  newPassword: string = '';
  confirmPassword: string = '';
  tokenId: number = 0;

  accountService = inject(AccountService);
  toastService = inject(ToastService);
  route = inject(ActivatedRoute);

  ngOnInit(): void {
    this.tokenId = Number(this.route.snapshot.queryParamMap.get('token'));
    console.log(this.tokenId);
  }

  resetPassword() {
    if (this.newPassword === this.confirmPassword) {
      console.log('Passwords match, proceed with reset');

      const passwordResetDetails = {
        tokenId: this.tokenId,
        newPassword: this.newPassword
      }
      this.accountService.changeUserPassword(passwordResetDetails).subscribe({
        next: (response) => {
          this.toastService.showSuccess("Success", response.message);
        },
        error: (response) => {
          this.toastService.showError("Error", response.error.message);
        }
      })
    } else {
      this.toastService.showError("Error", 'Passwords do not match');
    }
  }
}
