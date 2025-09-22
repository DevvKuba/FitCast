import { HttpClient } from '@angular/common/http';
import { inject, Injectable } from '@angular/core';
import { Observable } from 'rxjs';
import { Client } from '../models/client';

@Injectable({
  providedIn: 'root'
})
export class ClientService {
  http = inject(HttpClient);

  getAllClients() : Observable<Client[]>{
    return this.http.get<Client[]>("https://clientdashboardapp-dfdja3c4hxffdsg0.uksouth-01.azurewebsites.net/allClients")
  }

  updateClient(newClient : Client) : Observable<any>{
    return this.http.put("https://clientdashboardapp-dfdja3c4hxffdsg0.uksouth-01.azurewebsites.net/newClientInformation", newClient);
  }

  deleteClient(clientId: number) : Observable<any>{
    return this.http.delete(`https://clientdashboardapp-dfdja3c4hxffdsg0.uksouth-01.azurewebsites.net?clientId=${clientId}`);
  }
}
