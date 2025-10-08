import { inject, Injectable } from '@angular/core';
import { MessageService } from 'primeng/api';
import { Toast } from 'primeng/toast';

@Injectable({
  providedIn: 'root',
})
export class ToastService {
  messageService = inject(MessageService);

  toastSummary : string = "";
  toastDetail : string = "";

  showSuccess() {
        this.messageService.add({ severity: 'success', summary: this.toastSummary, detail: this.toastDetail });
  }

  showError() {
        this.messageService.add({ severity: 'error', summary: this.toastSummary, detail: this.toastDetail });
  }
}
