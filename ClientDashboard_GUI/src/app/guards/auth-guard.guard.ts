import { CanActivateFn } from '@angular/router';
import { AccountService } from '../services/account.service';
import { inject } from '@angular/core';
import { ToastService } from '../services/toast.service';

export const authGuard: CanActivateFn = () => {
  const accountService = inject(AccountService);
  const toast = inject(ToastService);
  if(accountService.currentUser()?.token){
    return true;
  }
  toast.toastSummary = "Unauthorized";
  toast.toastDetail = "You must log in before accessing this page";
  toast.showError();
  return false;
};
