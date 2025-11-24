import { Pipe, PipeTransform } from '@angular/core';
import { Client } from '../models/client';

@Pipe({
  name: 'clientName',
  pure: true // only recalculates when inputs change not during Angular detection
})
export class ClientNamePipe implements PipeTransform {

  transform(clientId: number , clients: Client[]): string {
    const client = clients.find(c => c.id === clientId);
    return client?.firstName ?? "";
  }

}
