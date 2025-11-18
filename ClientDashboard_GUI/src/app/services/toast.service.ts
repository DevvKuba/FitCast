import { inject, Injectable } from '@angular/core';
import { MessageService } from 'primeng/api';
import { Toast } from 'primeng/toast';

@Injectable({
  providedIn: 'root',
})
export class ToastService {
  messageService = inject(MessageService);

  showSuccess(toastSummary: string, toastDetail: string) {
        this.messageService.add({ severity: 'success', summary: toastSummary, detail: toastDetail });
  }

  showNeutral(toastSummary: string, toastDetail: string){
    this.messageService.add({severity: 'warn', summary: toastSummary, detail: toastDetail});
  }

  showError(toastSummary: string, toastDetail: string) {
        this.messageService.add({ severity: 'error', summary: toastSummary, detail: toastDetail });
  }
}
