import { ComponentFixture, TestBed } from '@angular/core/testing';
import { ActivatedRoute, Router } from '@angular/router';
import { of } from 'rxjs';
import { UserRole } from '../enums/user-role';
import { AccountService } from '../services/account.service';
import { ToastService } from '../services/toast.service';

import { RegisterComponent } from './register.component';

describe('RegisterComponent', () => {
  let component: RegisterComponent;
  let fixture: ComponentFixture<RegisterComponent>;
  let accountServiceSpy: jasmine.SpyObj<AccountService>;
  let toastServiceSpy: jasmine.SpyObj<ToastService>;
  let routerSpy: jasmine.SpyObj<Router>;

  beforeEach(async () => {
    accountServiceSpy = jasmine.createSpyObj<AccountService>('AccountService', ['register', 'clientVerifyUnderTrainer']);
    toastServiceSpy = jasmine.createSpyObj<ToastService>('ToastService', ['showSuccess', 'showError']);
    // RouterLink (used by the "To Login" link in the template) subscribes to router.events
    // and calls createUrlTree/serializeUrl internally, same as the login page's mock.
    routerSpy = jasmine.createSpyObj<Router>('Router', ['navigateByUrl', 'createUrlTree', 'serializeUrl'], { events: of() });
    routerSpy.createUrlTree.and.returnValue({} as any);
    routerSpy.serializeUrl.and.returnValue('/login');

    await TestBed.configureTestingModule({
      imports: [RegisterComponent],
      providers: [
        { provide: AccountService, useValue: accountServiceSpy },
        { provide: ToastService, useValue: toastServiceSpy },
        { provide: Router, useValue: routerSpy },
        { provide: ActivatedRoute, useValue: {} }
      ]
    })
    .compileComponents();

    fixture = TestBed.createComponent(RegisterComponent);
    component = fixture.componentInstance;
    fixture.detectChanges();
  });

  it('should create', () => {
    expect(component).toBeTruthy();
  });

  describe('validation behavior', () => {
    it('shows a validation error and does not register when required fields are missing', () => {
      component.userRegister('', '', '', '', '', null, null, '', '');

      expect(toastServiceSpy.showError).toHaveBeenCalledWith('Validation Error', 'First name is required');
      expect(accountServiceSpy.register).not.toHaveBeenCalled();
    });

    it('shows a validation error when passwords do not match', () => {
      component.userRegister(
        'jane@fitcast.com', 'Jane', 'Doe', '+44 7700 900000', 'trainer', null, null, 'secret1', 'secret2'
      );

      expect(toastServiceSpy.showError).toHaveBeenCalledWith('Validation Error', 'Passwords do not match');
      expect(accountServiceSpy.register).not.toHaveBeenCalled();
    });
  });

  describe('registration behavior', () => {
    it('registers a trainer and opens the verify-email dialog on success', () => {
      accountServiceSpy.register.and.returnValue(of({ success: true, message: 'registered' }));

      component.userRegister(
        'jane@fitcast.com', 'Jane', 'Doe', '+44 7700 900000', 'trainer', null, null, 'secret1', 'secret1'
      );

      expect(accountServiceSpy.register).toHaveBeenCalledWith({
        firstName: 'Jane',
        surname: 'Doe',
        email: 'jane@fitcast.com',
        phoneNumber: '+44 7700 900000',
        role: UserRole.Trainer,
        clientId: null,
        clientsTrainerId: null,
        password: 'secret1',
        confirmPassword: 'secret1'
      });
      expect(component.verifyEmailDialogVisible).toBeTrue();
    });

    it('registers a client without opening the verify-email dialog', () => {
      accountServiceSpy.register.and.returnValue(of({ success: true, message: 'registered' }));

      component.userRegister(
        'sam@fitcast.com', 'Sam', 'Lee', '+44 7700 900001', 'client', 4, 9, 'secret1', 'secret1'
      );

      expect(component.verifyEmailDialogVisible).toBeFalse();
    });
  });

  describe('client verification behavior', () => {
    it('verifies a client under a trainer by phone number', () => {
      accountServiceSpy.clientVerifyUnderTrainer.and.returnValue(
        of({ success: true, message: 'verified', data: { clientId: 4, trainerId: 9 } })
      );

      component.verifyClientUnderTrainer('+44 7700 900000', 'Sam');

      expect(component.trainerNumberVerified).toBeTrue();
      expect(component.verifiedClientId).toBe(4);
      expect(component.verifiedTrainerId).toBe(9);
    });
  });
});
