import { Component, OnInit, inject } from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormBuilder, FormGroup, FormsModule, ReactiveFormsModule, Validators } from '@angular/forms';
import { InputTextModule } from 'primeng/inputtext';
import { ButtonModule } from 'primeng/button';
import { FieldsetModule } from 'primeng/fieldset';
import { InputNumberModule } from 'primeng/inputnumber';
import { DropdownModule } from 'primeng/dropdown';
import { ToastModule } from 'primeng/toast';
import { MessageService } from 'primeng/api';
import { Trainer } from '../models/trainer';
import { AccountService } from '../services/account.service';
import { TrainerService } from '../services/trainer.service';
import { defaultIfEmpty } from 'rxjs';
import { ToastService } from '../services/toast.service';

interface Currency {
  name: string;
  code: string;
}

@Component({
  selector: 'app-trainer-profile-page',
  standalone: true,
  imports: [
    CommonModule,
    FormsModule,
    InputTextModule,
    ButtonModule,
    FieldsetModule,
    InputNumberModule,
    DropdownModule,
    ToastModule,
    ReactiveFormsModule
  ],
  providers: [MessageService],
  templateUrl: './trainer-profile-page.component.html',
  styleUrl: './trainer-profile-page.component.css'
})
export class TrainerProfilePageComponent implements OnInit {
  profileForm!: FormGroup

  accountService = inject(AccountService);
  // messageService = inject(MessageService);
  toastService = inject(ToastService)
  trainerService = inject(TrainerService);
  formBuilder = inject(FormBuilder);

  // firstName: string = "";
  // surname: string = "";
  // email: string = "";
  // phoneNumber: string = "";
  // businessName: string = "";
  // defaultCurrency: string = "";
  // averageSessionPrice: number = 0;
  
  currencies: Currency[] = [
    { name: 'British Pound (GBP)', code: 'GBP' },
    { name: 'US Dollar (USD)', code: 'USD' },
    { name: 'Euro (EUR)', code: 'EUR' },
    { name: 'Canadian Dollar (CAD)', code: 'CAD' },
    { name: 'Australian Dollar (AUD)', code: 'AUD' }
  ];

  ngOnInit(): void {
    this.profileForm = this.formBuilder.group({
      firstName: ['', Validators.required],
      surname: ['', Validators.required],
      email: ['', Validators.email],
      phoneNumber: [''],
      businessName: [''],
      defaultCurrency: ['GBP'],
      averageSessionPrice: [0]
    });

    this.loadTrainerProfile();
  }
  
  loadTrainerProfile(): void {
    const currentUser = this.accountService.currentUser();
    // get trainer through currentUser.id and then store everything
    if(currentUser){
      this.trainerService.retrieveTrainerById(currentUser.id).subscribe({
        next: (response) => {
          this.profileForm.patchValue(response.data);
        }
      })
    }
  }

  resetProfile(): void {
    this.profileForm.reset();
    this.loadTrainerProfile();
  }

  saveProfile(){
    if(this.profileForm.valid){
      const trainerUpdatedProfile = this.profileForm.value;

      const currentUser = this.accountService.currentUser();

      if(currentUser){
        this.trainerService.updateTrainerProfile(currentUser.id, trainerUpdatedProfile).subscribe({
          next: (response) => {
            this.toastService.showSuccess("Success Updating Profile",  `${response.data}'s profile has been updated`);
          },
          error: (response) => {
            this.toastService.showError("Error Updating Profile",  `${response.data}'s profile has not been updated`);
          }
        })
      }
    }
  }
  
}