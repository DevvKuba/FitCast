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
import { UserRole } from '../enums/user-role';

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
  confirmPassword: string = "";
  userType: string = "";

  userRegister(email: string, firstName: string, surname: string,
    phoneNumber: string, userType: string, clientId: number | null,
    clientsTrainerId: number | null,  password: string, confirmPassword: string){
    
    // Frontend validation
    if(!this.validateRegisterFields(email, firstName, surname, phoneNumber, password, confirmPassword, userType)){
      return;
    }
    
    const roleMap: Record<string,UserRole> = {
          'trainer': UserRole.Trainer,
          'client': UserRole.Client
        }
    const userRole = roleMap[userType];
    
    if(!userRole){
      this.toastService.showError('Error Logging In', 'Need to select a role');
      return;
    }

    const registerInfo: RegisterDto = {
      firstName : firstName,
      surname: surname,
      email : email, 
      phoneNumber: phoneNumber,
      role : userRole,
      clientId : clientId || null,
      clientsTrainerId : clientsTrainerId || null,
      password: password,
      confirmPassword: confirmPassword
    }
    this.accountService.register(registerInfo).subscribe({
      next: (response : ApiResponse<string>) => {
        this.toastService.showSuccess('Success Registering', response.message);
        if(userRole == UserRole.Trainer){
          this.verifyEmailDialogVisible = true;
        }
        console.log(response);
      },
      error: (response) => {
        this.toastService.showError('Error Registering', response.error.message);
        console.log(response)
      }
    });
  }

  verifyClientUnderTrainer(trainerPhoneNumber: string, clientFirstName: string){
    if(!trainerPhoneNumber || trainerPhoneNumber.trim() === ''){
      this.toastService.showError('Validation Error', 'Trainer phone number is required');
      return;
    }

    if(!clientFirstName || clientFirstName.trim() === ''){
      this.toastService.showError('Validation Error', 'Client first name is required');
      return;
    }

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

  private validateRegisterFields(email: string, firstName: string, surname: string, 
    phoneNumber: string, password: string, confirmPassword: string, userType: string): boolean {
    
    if(!firstName || firstName.trim() === ''){
      this.toastService.showError('Validation Error', 'First name is required');
      return false;
    }

    if(!surname || surname.trim() === ''){
      this.toastService.showError('Validation Error', 'Surname is required');
      return false;
    }

    if(!email || email.trim() === ''){
      this.toastService.showError('Validation Error', 'Email is required');
      return false;
    }

    if(!phoneNumber || phoneNumber.trim() === ''){
      this.toastService.showError('Validation Error', 'Phone number is required');
      return false;
    }

    if(!password || password.trim() === ''){
      this.toastService.showError('Validation Error', 'Password is required');
      return false;
    }

    if(!confirmPassword || confirmPassword.trim() === ''){
      this.toastService.showError('Validation Error', 'Confirm password is required');
      return false;
    }

    if(password !== confirmPassword){
      this.toastService.showError('Validation Error', 'Passwords do not match');
      return false;
    }

    if(!userType || userType.trim() === ''){
      this.toastService.showError('Validation Error', 'You must select a user type');
      return false;
    }

    return true;
  }
}
