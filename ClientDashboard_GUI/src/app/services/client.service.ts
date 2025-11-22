import { HttpClient } from '@angular/common/http';
import { inject, Injectable } from '@angular/core';
import { Observable } from 'rxjs';
import { Client } from '../models/client';
import { ApiResponse } from '../models/api-response';
import { environment } from '../environments/environment';
import { ClientAddDto } from '../models/dtos/client-add-dto';
import { AccountService } from './account.service';

@Injectable({
  providedIn: 'root'
})

export class ClientService {
  http = inject(HttpClient);
  baseUrl = environment.apiUrl;
  accountService = inject(AccountService);

  getAllTrainerClients(trainerId: number) : Observable<ApiResponse<Client[]>>{
    return this.http.get<ApiResponse<Client[]>>( this.baseUrl + `client/allTrainerClients?trainerId=${trainerId}`)
  }

  updateClient(newClient : Client) : Observable<ApiResponse<any>>{
    return this.http.put<any>( this.baseUrl + "client/newClientInformation", newClient);
  }

  deleteClient(clientId: number) : Observable<ApiResponse<any>>{
    return this.http.delete<any>( this.baseUrl + `client/ById?clientId=${clientId}`);
  }

  addClient(newClient: ClientAddDto): Observable<ApiResponse<ClientAddDto>>{
    return this.http.post<ApiResponse<ClientAddDto>>(this.baseUrl + `client/ByBody`, newClient);
  }

  gatherClientNames(): any[]{
    this.getAllTrainerClients(this.accountService.currentUser()?.id ?? 0).subscribe({
      next: (response) => {
        return response.data?.map(x => ({id: x.id , name: x.firstName})) ?? [];
      },
      error: () => {
        console.log('Failed to display client for which you may add a workout for');
        return [];
      }
    })
    return [];
  }
}
