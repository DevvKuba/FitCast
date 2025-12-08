import { HttpClient } from '@angular/common/http';
import { inject, Injectable } from '@angular/core';
import { environment } from '../environments/environment';
import { Observable } from 'rxjs';
import { PaymentAddDto } from '../models/dtos/payment-add-dto';
import { PaymentRequestUpdateDto } from '../models/dtos/payment-update-request-dto';

@Injectable({
  providedIn: 'root'
})
export class PaymentService {
  http = inject(HttpClient);
  baseUrl = environment.apiUrl;

  getTrainerPayments(trainerId: number) : Observable<any>{
    return this.http.get(this.baseUrl + `payment/getAllTrainerPayments?trainerId=${trainerId}`);
  }

  updatePaymentInfo(paymentInfo: PaymentRequestUpdateDto) : Observable<any> {
    return this.http.put(this.baseUrl + 'payment/updateExistingPayment', paymentInfo);
  }

  addTrainerPayment(paymentInfo: PaymentAddDto) : Observable<any> {
    return this.http.post(this.baseUrl + 'payment/addPayment', paymentInfo); 
  }

  filterOldClientPayments(trainerId: number) : Observable<any> {
    return this.http.delete(this.baseUrl + `payment/filterClientPayments?trainerId=${trainerId}`);
  }

  deleteTrainerPayment(paymentId: number) : Observable<any> {
    return this.http.delete(this.baseUrl + `payment/deletePayment?paymentId=${paymentId}`)
  }
}
