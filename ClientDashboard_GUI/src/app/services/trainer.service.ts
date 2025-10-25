import { HttpClient } from '@angular/common/http';
import { inject, Injectable } from '@angular/core';
import { environment } from '../environments/environment';
import { Observable } from 'rxjs';

@Injectable({
  providedIn: 'root'
})
export class TrainerService {
  http = inject(HttpClient);
  baseUrl = environment.apiUrl;

  assignClient(clientId : number, trainerId : number) : Observable<any>{
  return this.http.put(this.baseUrl + `assignClient?clientId=${clientId}&trainerId=${trainerId}`, null);
  } 
}
