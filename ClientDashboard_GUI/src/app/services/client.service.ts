import { HttpClient } from '@angular/common/http';
import { inject, Injectable } from '@angular/core';
import { Observable } from 'rxjs';
import { Client } from '../models/client';
import { ApiResponse } from '../models/api-response';

@Injectable({
  providedIn: 'root'
})
export class ClientService {
  http = inject(HttpClient);

  getAllClients() : Observable<ApiResponse<Client[]>>{
    return this.http.get<ApiResponse<Client[]>>("https://clientdashboardapp-dfdja3c4hxffdsg0.uksouth-01.azurewebsites.net/allClients")
  }

  updateClient(newClient : Client) : Observable<ApiResponse<any>>{
    return this.http.put<any>("https://clientdashboardapp-dfdja3c4hxffdsg0.uksouth-01.azurewebsites.net/newClientInformation", newClient);
  }

  deleteClient(clientId: number) : Observable<ApiResponse<any>>{
    return this.http.delete<any>(`https://clientdashboardapp-dfdja3c4hxffdsg0.uksouth-01.azurewebsites.net/ById?clientId=${clientId}`);
  }

  addClient(client: Client): Observable<ApiResponse<Client>>{
    return this.http.post<ApiResponse<Client>>(`https://clientdashboardapp-dfdja3c4hxffdsg0.uksouth-01.azurewebsites.net/ByBody`, client);
  }
}
