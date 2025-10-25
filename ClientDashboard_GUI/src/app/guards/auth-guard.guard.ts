import { CanActivateFn, Router} from '@angular/router';
import { AccountService } from '../services/account.service';
import { inject } from '@angular/core';
import { ToastService } from '../services/toast.service';

export const authGuard: CanActivateFn = () => {
  const accountService = inject(AccountService);
  const toast = inject(ToastService);
  const router = inject(Router);


  if(accountService.currentUser()?.token){
    return true;
  }
  var toastSummary = "Unauthorized";
  var toastDetail = "You must log in before accessing this page";
  toast.showError(toastSummary, toastDetail);
  return router.createUrlTree(['']);
};
