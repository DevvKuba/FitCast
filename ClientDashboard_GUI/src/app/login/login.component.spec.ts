import { ComponentFixture, TestBed } from '@angular/core/testing';
import { Router } from '@angular/router';
import { ActivatedRoute } from '@angular/router';
import { MessageService } from 'primeng/api';
import { of, throwError } from 'rxjs';
import { UserRole } from '../enums/user-role';
import { AccountService } from '../services/account.service';
import { ToastService } from '../services/toast.service';

import { LoginComponent } from './login.component';

describe('LoginComponent', () => {
  let component: LoginComponent;
  let fixture: ComponentFixture<LoginComponent>;
  let accountServiceMock: {
    login: jasmine.Spy;
    resendEmailVerification: jasmine.Spy;
    sendPasswordResetEmail: jasmine.Spy;
    logout: jasmine.Spy;
    currentUser: any;
  };
  let toastServiceSpy: jasmine.SpyObj<ToastService>;
  let routerSpy: jasmine.SpyObj<Router>;
  let messageServiceSpy: jasmine.SpyObj<MessageService>;

  beforeEach(async () => {
    // Simulate Angular signal-like API: callable function + .set(...) method.
    const currentUserSignal = jasmine.createSpy('currentUser').and.returnValue(null) as any;
    currentUserSignal.set = jasmine.createSpy('set');

    accountServiceMock = {
      login: jasmine.createSpy('login'),
      resendEmailVerification: jasmine.createSpy('resendEmailVerification'),
      sendPasswordResetEmail: jasmine.createSpy('sendPasswordResetEmail'),
      logout: jasmine.createSpy('logout'),
      currentUser: currentUserSignal
    };

    toastServiceSpy = jasmine.createSpyObj<ToastService>('ToastService', ['showSuccess', 'showError']);
    routerSpy = jasmine.createSpyObj<Router>('Router', ['navigateByUrl']);
    messageServiceSpy = jasmine.createSpyObj<MessageService>('MessageService', ['add']);

    await TestBed.configureTestingModule({
      imports: [LoginComponent],
      providers: [
        { provide: AccountService, useValue: accountServiceMock },
        { provide: ToastService, useValue: toastServiceSpy },
        { provide: Router, useValue: routerSpy },
        { provide: MessageService, useValue: messageServiceSpy },
        { provide: ActivatedRoute, useValue: {} }
      ]
    })
    .compileComponents();

    fixture = TestBed.createComponent(LoginComponent);
    component = fixture.componentInstance;
    fixture.detectChanges();
  });

  it('should create', () => {
    expect(component).toBeTruthy();
  });

  describe('validation behavior', () => {
    it('shows validation error and stops login when email is missing', () => {
      component.userLogin('', 'secret', 'trainer');

      expect(toastServiceSpy.showError).toHaveBeenCalledWith('Validation Error', 'Email is required');
      expect(accountServiceMock.login).not.toHaveBeenCalled();
    });

    it('shows validation error and stops login when password is missing', () => {
      component.userLogin('trainer@fitcast.com', '', 'trainer');

      expect(toastServiceSpy.showError).toHaveBeenCalledWith('Validation Error', 'Password is required');
      expect(accountServiceMock.login).not.toHaveBeenCalled();
    });

    it('shows validation error and stops login when user type is missing', () => {
      component.userLogin('trainer@fitcast.com', 'secret', '');

      expect(toastServiceSpy.showError).toHaveBeenCalledWith(
        'Validation Error',
        'You must select a user type'
      );
      expect(accountServiceMock.login).not.toHaveBeenCalled();
    });
  });

  describe('role mapping and navigation behavior', () => {
    it('maps trainer selection to trainer role and redirects to client-info on success', () => {
      const trainerUser = {
        id: 1,
        firstName: 'Alex',
        role: UserRole.Trainer,
        token: 'trainer-token'
      };
      accountServiceMock.login.and.returnValue(of({ success: true, message: 'ok', data: trainerUser }));
      const localStorageSpy = spyOn(localStorage, 'setItem');

      component.userLogin('trainer@fitcast.com', 'secret', 'trainer');

      expect(accountServiceMock.login).toHaveBeenCalledWith({
        email: 'trainer@fitcast.com',
        password: 'secret',
        role: UserRole.Trainer
      });
      expect(localStorageSpy).toHaveBeenCalledWith('token', 'trainer-token');
      expect(accountServiceMock.currentUser.set).toHaveBeenCalledWith(trainerUser);
      expect(toastServiceSpy.showSuccess).toHaveBeenCalledWith('Logged In', 'Redirected to client-info page');
      expect(routerSpy.navigateByUrl).toHaveBeenCalledWith('client-info');
    });

    it('maps client selection to client role and redirects to personal workouts on success', () => {
      const clientUser = {
        id: 5,
        firstName: 'Sam',
        role: UserRole.Client,
        token: 'client-token'
      };
      accountServiceMock.login.and.returnValue(of({ success: true, message: 'ok', data: clientUser }));

      component.userLogin('client@fitcast.com', 'secret', 'client');

      expect(accountServiceMock.login).toHaveBeenCalledWith({
        email: 'client@fitcast.com',
        password: 'secret',
        role: UserRole.Client
      });
      expect(accountServiceMock.currentUser.set).toHaveBeenCalledWith(clientUser);
      expect(toastServiceSpy.showSuccess).toHaveBeenCalledWith(
        'Logged In',
        'Redirected to personal workouts page'
      );
      expect(routerSpy.navigateByUrl).toHaveBeenCalledWith('client-personal-workouts');
    });

    it('shows an explicit error when provided user type is not trainer or client', () => {
      component.userLogin('user@fitcast.com', 'secret', 'admin');

      expect(accountServiceMock.login).not.toHaveBeenCalled();
      expect(toastServiceSpy.showError).toHaveBeenCalledWith(
        'Error Logging In',
        'You must select a user type'
      );
    });
  });

  describe('error handling behavior', () => {
    it('shows API error toast when login request fails', () => {
      accountServiceMock.login.and.returnValue(
        throwError(() => ({ error: { message: 'Invalid credentials' } }))
      );

      component.userLogin('trainer@fitcast.com', 'wrong-password', 'trainer');

      expect(toastServiceSpy.showError).toHaveBeenCalledWith('Unable to log in', 'Invalid credentials');
      expect(routerSpy.navigateByUrl).not.toHaveBeenCalled();
    });
  });

  describe('documentation: how these tests operate', () => {
    it('uses service spies to test component decision logic without real HTTP calls', () => {
      accountServiceMock.login.and.returnValue(
        of({
          success: true,
          message: 'ok',
          data: { id: 9, firstName: 'Casey', role: UserRole.Client, token: 't' }
        })
      );

      component.userLogin('client@fitcast.com', 'secret', 'client');

      // The component logic decides which toast + route to use based on selected role.
      expect(accountServiceMock.login).toHaveBeenCalled();
      expect(toastServiceSpy.showSuccess).toHaveBeenCalled();
    });
  });
});
