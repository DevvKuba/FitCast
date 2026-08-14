import { ComponentFixture, TestBed } from '@angular/core/testing';
import { ActivatedRoute, Router } from '@angular/router';
import { of } from 'rxjs';
import { AccountService } from '../services/account.service';
import { ToastService } from '../services/toast.service';

import { PasswordResetComponent } from './password-reset.component';

describe('PasswordResetComponent', () => {
  let component: PasswordResetComponent;
  let fixture: ComponentFixture<PasswordResetComponent>;
  let accountServiceSpy: jasmine.SpyObj<AccountService>;
  let toastServiceSpy: jasmine.SpyObj<ToastService>;
  let routerSpy: jasmine.SpyObj<Router>;

  beforeEach(async () => {
    accountServiceSpy = jasmine.createSpyObj<AccountService>('AccountService', ['changeUserPassword']);
    toastServiceSpy = jasmine.createSpyObj<ToastService>('ToastService', ['showSuccess', 'showError']);
    // RouterLink (used by the "Return home" link in the template) subscribes to router.events
    // and calls createUrlTree/serializeUrl internally, same as the login page's mock.
    routerSpy = jasmine.createSpyObj<Router>('Router', ['navigateByUrl', 'createUrlTree', 'serializeUrl'], { events: of() });
    routerSpy.createUrlTree.and.returnValue({} as any);
    routerSpy.serializeUrl.and.returnValue('/');

    await TestBed.configureTestingModule({
      imports: [PasswordResetComponent],
      providers: [
        { provide: AccountService, useValue: accountServiceSpy },
        { provide: ToastService, useValue: toastServiceSpy },
        { provide: Router, useValue: routerSpy },
        { provide: ActivatedRoute, useValue: { snapshot: { queryParamMap: { get: () => 'raw-token' } } } }
      ]
    })
    .compileComponents();

    fixture = TestBed.createComponent(PasswordResetComponent);
    component = fixture.componentInstance;
    fixture.detectChanges();
  });

  it('should create', () => {
    expect(component).toBeTruthy();
  });

  it('reads the reset token from the query string on init', () => {
    expect(component.rawToken).toBe('raw-token');
  });

  describe('resetPassword', () => {
    it('submits the reset when both passwords match', () => {
      accountServiceSpy.changeUserPassword.and.returnValue(of({ success: true, message: 'reset' }));
      component.newPassword = 'newPass123';
      component.confirmPassword = 'newPass123';

      component.resetPassword();

      expect(accountServiceSpy.changeUserPassword).toHaveBeenCalledWith({
        rawToken: 'raw-token',
        newPassword: 'newPass123'
      });
      expect(toastServiceSpy.showSuccess).toHaveBeenCalledWith('Success', 'reset');
    });

    it('shows an error and does not submit when passwords do not match', () => {
      component.newPassword = 'newPass123';
      component.confirmPassword = 'different';

      component.resetPassword();

      expect(accountServiceSpy.changeUserPassword).not.toHaveBeenCalled();
      expect(toastServiceSpy.showError).toHaveBeenCalledWith('Error', 'Passwords do not match');
    });
  });
});
