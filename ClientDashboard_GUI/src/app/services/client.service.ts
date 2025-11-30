import { HttpClient } from '@angular/common/http';
import { inject, Injectable } from '@angular/core';
import { catchError, map, Observable, of } from 'rxjs';
import { Client } from '../models/client';
import { ApiResponse } from '../models/api-response';
import { environment } from '../environments/environment';
import { ClientAddDto } from '../models/dtos/client-add-dto';
import { AccountService } from './account.service';
import { ClientPhoneNumberUpdateDto } from '../models/dtos/client-phone-number-update-dto';

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

  getClientPhoneNumber(clientId: number) : Observable<ApiResponse<string>> {
    return this.http.get<any>(this.baseUrl + `getClientPhoneNumber?clientId=${clientId}`);
  }

  updateClient(newClient : Client) : Observable<ApiResponse<any>>{
    return this.http.put<any>( this.baseUrl + "client/newClientInformation", newClient);
  }

  updateClientPhoneNumber(clientInfo: ClientPhoneNumberUpdateDto) : Observable<ApiResponse<string>>{
    return this.http.put<any>(this.baseUrl + 'client/setClientPhoneNumber', clientInfo);
  }

  deleteClient(clientId: number) : Observable<ApiResponse<any>>{
    return this.http.delete<any>( this.baseUrl + `client/ById?clientId=${clientId}`);
  }

  addClient(newClient: ClientAddDto): Observable<ApiResponse<ClientAddDto>>{
    return this.http.post<ApiResponse<ClientAddDto>>(this.baseUrl + `client/ByBody`, newClient);
  }

  gatherClientNames(trainerId: number): Observable<{id: number, name: string}[]>{
    return this.getAllTrainerClients(trainerId).pipe(
        map(response => response.data?.map(x => ({id: x.id , name: x.firstName})) ?? []),
        catchError(() => of([]))
    );
  }
}
