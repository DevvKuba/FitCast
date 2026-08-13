import { HttpClient } from '@angular/common/http';
import { inject, Injectable } from '@angular/core';
import { catchError, map, Observable, of } from 'rxjs';
import { Client } from '../models/client';
import { ApiResponse } from '../models/api-response';
import { environment } from '../environments/environment';
import { ClientAddDto } from '../models/dtos/client-add-dto';
import { AccountService } from './account.service';
import { ClientUpdateDto } from '../models/dtos/client-update-dto';

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

  getClientNameById(clientId: number) : Observable<ApiResponse<string>>{
    return this.http.get<ApiResponse<string>>(this.baseUrl + `client/getClientById?clientId=${clientId}`)
  }

  updateClient(newClient : ClientUpdateDto) : Observable<ApiResponse<any>>{
    return this.http.put<any>( this.baseUrl + "client/newClientInformation", newClient);
  }

  deleteClient(clientId: number) : Observable<ApiResponse<any>>{
    return this.http.delete<any>( this.baseUrl + `client/removeClientById?clientId=${clientId}`);
  }

  addClient(newClient: ClientAddDto): Observable<ApiResponse<ClientAddDto>>{
    return this.http.post<ApiResponse<ClientAddDto>>(this.baseUrl + `client/addNewClient`, newClient);
  }

  gatherClientNames(trainerId: number): Observable<{id: number, name: string}[]>{
    return this.getAllTrainerClients(trainerId).pipe(
        map(response => response.data?.map(x => ({id: x.id , name: x.firstName})) ?? []),
        catchError(() => of([]))
    );
  }
}
