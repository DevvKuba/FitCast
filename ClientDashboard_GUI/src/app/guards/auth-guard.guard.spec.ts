import { TestBed } from '@angular/core/testing';
import { CanActivateFn } from '@angular/router';
import { Router, UrlTree } from '@angular/router';
import { AccountService } from '../services/account.service';
import { ToastService } from '../services/toast.service';

import { authGuard } from './auth-guard.guard';

describe('authGuardGuard', () => {
  const executeGuard: CanActivateFn = (...guardParameters) => 
      TestBed.runInInjectionContext(() => authGuard(...guardParameters));

  let accountServiceSpy: jasmine.SpyObj<AccountService>;
  let toastServiceSpy: jasmine.SpyObj<ToastService>;
  let routerSpy: jasmine.SpyObj<Router>;
  let blockedNavigationTree: UrlTree;

  beforeEach(() => {
    // Shared test doubles used by all guard scenarios.
    accountServiceSpy = jasmine.createSpyObj<AccountService>('AccountService', ['currentUser']);
    toastServiceSpy = jasmine.createSpyObj<ToastService>('ToastService', ['showError']);
    routerSpy = jasmine.createSpyObj<Router>('Router', ['createUrlTree']);
    blockedNavigationTree = {} as UrlTree;
    routerSpy.createUrlTree.and.returnValue(blockedNavigationTree);

    TestBed.configureTestingModule({
      providers: [
        { provide: AccountService, useValue: accountServiceSpy },
        { provide: ToastService, useValue: toastServiceSpy },
        { provide: Router, useValue: routerSpy }
      ]
    });
  });

  describe('when user is authenticated', () => {
    it('returns true and does not show an error toast', () => {
      accountServiceSpy.currentUser.and.returnValue({ token: 'valid-token' } as any);

      const result = executeGuard({} as any, {} as any);

      expect(result).toBeTrue();
      expect(toastServiceSpy.showError).not.toHaveBeenCalled();
      expect(routerSpy.createUrlTree).not.toHaveBeenCalled();
    });
  });

  describe('when user is not authenticated', () => {
    it('blocks access, shows a toast, and redirects to the home route', () => {
      accountServiceSpy.currentUser.and.returnValue(null);

      const result = executeGuard({} as any, {} as any);

      expect(toastServiceSpy.showError).toHaveBeenCalledWith(
        'Unauthorized',
        'You must log in before accessing this page'
      );
      expect(routerSpy.createUrlTree).toHaveBeenCalledWith(['']);
      expect(result).toBe(blockedNavigationTree);
    });
  });

  describe('documentation: how this guard test works', () => {
    it('uses runInInjectionContext so inject() calls inside the guard can resolve mocks', () => {
      accountServiceSpy.currentUser.and.returnValue({ token: 'context-check' } as any);

      const result = executeGuard({} as any, {} as any);

      // This assertion confirms we executed the real guard function, not a stub.
      expect(result).toBeTrue();
      expect(accountServiceSpy.currentUser).toHaveBeenCalled();
    });
  });
});
