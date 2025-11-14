import { Component, OnInit, inject } from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormsModule } from '@angular/forms';
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
    ToastModule
  ],
  providers: [MessageService],
  templateUrl: './trainer-profile-page.component.html',
  styleUrl: './trainer-profile-page.component.css'
})
export class TrainerProfilePageComponent implements OnInit {
  accountService = inject(AccountService);
  messageService = inject(MessageService);
  trainerService = inject(TrainerService);

  firstName: string = "";
  surname: string = "";
  email: string = "";
  phoneNumber: string = "";
  businessName: string = "";
  defaultCurrency: string = "";
  averageSessionPrice: number = 0;
  
  currencies: Currency[] = [
    { name: 'British Pound (GBP)', code: 'GBP' },
    { name: 'US Dollar (USD)', code: 'USD' },
    { name: 'Euro (EUR)', code: 'EUR' },
    { name: 'Canadian Dollar (CAD)', code: 'CAD' },
    { name: 'Australian Dollar (AUD)', code: 'AUD' }
  ];

  ngOnInit(): void {
    this.loadTrainerProfile();
  }
  
  loadTrainerProfile(): void {
    const currentUser = this.accountService.currentUser();
    // get trainer through currentUser.id and then store everything
    if(currentUser){
      this.trainerService.retrieveTrainerById(currentUser.id).subscribe({
        next: (response) => {
          this.firstName = response.data.firstName || '';
          this.surname = response.data.surname || '';
          this.email = response.data.email || '';
          this.phoneNumber = response.data.phoneNumber || '';
          this.businessName = response.data.businessName || '';
          this.defaultCurrency = response.data.defaultCurrency || '';
          this.averageSessionPrice = response.data.averageSessionPrice || 0;
        }
      })
    }
  }

  resetInputFields(): void {
    this.loadTrainerProfile();
  }

  saveInputFields(){
    
  }
  
}