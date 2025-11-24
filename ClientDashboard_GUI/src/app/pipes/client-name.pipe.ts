import { Pipe, PipeTransform } from '@angular/core';
import { Client } from '../models/client';

@Pipe({
  name: 'clientName',
  pure: true, // only recalculates when inputs change not during Angular detection
  standalone: true
})
export class ClientNamePipe implements PipeTransform {

  transform(clientId: number , clients: {id: number, name: string}[]): string {
    const client = clients.find(c => c.id === clientId);
    return client?.name || `Client: ${clientId}`;
  }

}
