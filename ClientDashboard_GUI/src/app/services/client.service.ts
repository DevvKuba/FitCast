import { HttpClient } from '@angular/common/http';
import { inject, Injectable } from '@angular/core';
import { Observable } from 'rxjs';
import { Client } from '../models/client';
import { ApiResponse } from '../models/api-response';
import { environment } from '../environments/environment.prod';

@Injectable({
  providedIn: 'root'
})

export class ClientService {
  http = inject(HttpClient);
  baseUrl = environment.apiUrl;

  getAllClients() : Observable<ApiResponse<Client[]>>{
    return this.http.get<ApiResponse<Client[]>>( this.baseUrl + "client/allClients")
  }

  updateClient(newClient : Client) : Observable<ApiResponse<any>>{
    return this.http.put<any>( this.baseUrl + "client/newClientInformation", newClient);
  }

  deleteClient(clientId: number) : Observable<ApiResponse<any>>{
    return this.http.delete<any>( this.baseUrl + `client/ById?clientId=${clientId}`);
  }

  addClient(client: Client): Observable<ApiResponse<Client>>{
    return this.http.post<ApiResponse<Client>>(this.baseUrl + `client/ByBody`, client);
  }
}
