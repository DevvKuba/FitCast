import { HttpClient } from '@angular/common/http';
import { inject, Injectable } from '@angular/core';
import { environment } from '../environments/environment';
import { Observable } from 'rxjs';

@Injectable({
  providedIn: 'root'
})
export class PaymentService {
  http = inject(HttpClient);
  baseUrl = environment.apiUrl;

  getTrainerPayments(trainerId: number) : Observable<any>{
    return this.http.get(this.baseUrl + `payment/getAllTrainerPayments?trainerId=${trainerId}`);
  }
}
