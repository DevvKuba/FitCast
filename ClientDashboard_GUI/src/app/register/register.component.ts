import { Component, inject } from '@angular/core';
import { FormsModule } from '@angular/forms';
import { ButtonModule } from 'primeng/button';
import { FloatLabelModule } from 'primeng/floatlabel';
import { IftaLabelModule } from 'primeng/iftalabel';
import { InputTextModule } from 'primeng/inputtext';
import { Message } from 'primeng/message';
import { PasswordModule } from 'primeng/password';
import { AccountService } from '../services/account.service';
import { RegisterDto } from '../models/dtos/register-dto';
import { Toast } from 'primeng/toast';
import { ApiResponse } from '../models/api-response';
import { ToastService } from '../services/toast.service';
import { Router, RouterLink } from '@angular/router';
import { RadioButton } from 'primeng/radiobutton';
import { InputMask } from 'primeng/inputmask';
import { ToggleButton } from 'primeng/togglebutton';
import { Dialog } from 'primeng/dialog';

@Component({
  selector: 'app-register',
  imports: [InputTextModule, PasswordModule, IftaLabelModule, FormsModule, Dialog,
     FloatLabelModule, ButtonModule, RouterLink, RadioButton, InputMask, ToggleButton],
  templateUrl: './register.component.html',
  styleUrl: './register.component.css'
})
export class RegisterComponent {
  accountService = inject(AccountService);
  toastService = inject(ToastService);
  router = inject(Router);

  verifyEmailDialogVisible = false;
  trainerNumberVerified: boolean = false;
  trainerVerificationPhoneNumber: string = "";
  verifiedClientId: number = 0;
  verifiedTrainerId: number = 0;

  firstName: string = "";
  surname: string = "";
  email: string = "";
  phoneNumber: string = "";
  password: string = "";
  userType: string = "";

  userRegister(email: string, firstName: string, surname: string,
     phoneNumber: string, userType: string, clientId: number | null,
    clientsTrainerId: number | null,  password: string){
    const registerInfo: RegisterDto = {
      firstName : firstName,
      surname: surname,
      email : email, 
      phoneNumber: phoneNumber,
      role : userType,
      clientId : clientId || null,
      clientsTrainerId : clientsTrainerId || null,
      password: password
    }
    this.accountService.register(registerInfo).subscribe({
      next: (response : ApiResponse<string>) => {
        this.toastService.showSuccess('Success Registering', response.message);
        this.verifyEmailDialogVisible = true;
        console.log(response);
      },
      error: (response) => {
        this.toastService.showError('Error Registering', response.error.message);
        console.log(response)
      }
    });
  }

  verifyClientUnderTrainer(trainerPhoneNumber: string, clientFirstName: string){
    this.accountService.clientVerifyUnderTrainer(trainerPhoneNumber, clientFirstName).subscribe({
      next: (response) => {
        this.trainerNumberVerified = true;
        this.verifiedClientId = response.data.clientId;
        this.verifiedTrainerId = response.data.trainerId;
        console.log("Verified client id: " + this.verifiedClientId);
        console.log("Verified trainer id: " + this.verifiedTrainerId)

        this.toastService.showSuccess('Verified Successfully', response.message);
      },
      error: (response) => {
        this.toastService.showError('Unsuccessful Verification', response.error.message);
      }
    })
  }
}
