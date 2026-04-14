import { TestBed } from '@angular/core/testing';
import { provideHttpClient } from '@angular/common/http';
import { UserRole } from '../enums/user-role';

import { AccountService } from './account.service';

describe('AccountService', () => {
  let service: AccountService;

  const createJwt = (payload: Record<string, unknown>): string => {
    const header = btoa(JSON.stringify({ alg: 'HS256', typ: 'JWT' }));
    const body = btoa(JSON.stringify(payload));
    return `${header}.${body}.signature`;
  };

  beforeEach(() => {
    TestBed.configureTestingModule({
      providers: [provideHttpClient()]
    });

    service = TestBed.inject(AccountService);
  });

  it('should be created', () => {
    expect(service).toBeTruthy();
  });

  describe('isAuthenticated', () => {
    it('returns true when current user has a token', () => {
      service.currentUser.set({
        id: 1,
        firstName: 'Alex',
        role: UserRole.Trainer,
        token: 'token-123'
      });

      expect(service.isAuthenticated()).toBeTrue();
    });

    it('returns false when no current user exists', () => {
      service.currentUser.set(null);

      expect(service.isAuthenticated()).toBeFalse();
    });
  });

  describe('initializeAuthState', () => {
    it('hydrates current user from a valid, non-expired token in localStorage', async () => {
      const futureExp = Math.floor(Date.now() / 1000) + 3600;
      const token = createJwt({
        sub: 17,
        given_name: 'Sam',
        role: 'Trainer',
        exp: futureExp
      });
      spyOn(localStorage, 'getItem').and.returnValue(token);

      await service.initializeAuthState();

      expect(service.currentUser()).toEqual({
        id: 17,
        firstName: 'Sam',
        role: UserRole.Trainer,
        token
      });
    });

    it('does not hydrate current user from an expired token', async () => {
      const expiredToken = createJwt({
        sub: 5,
        given_name: 'Chris',
        role: 'Client',
        exp: Math.floor(Date.now() / 1000) - 60
      });
      spyOn(localStorage, 'getItem').and.returnValue(expiredToken);

      await service.initializeAuthState();

      expect(service.currentUser()).toBeNull();
    });

    it('does not throw and keeps user null for malformed token values', async () => {
      spyOn(localStorage, 'getItem').and.returnValue('invalid-token');

      await expectAsync(service.initializeAuthState()).toBeResolved();
      expect(service.currentUser()).toBeNull();
    });

    it('leaves user null when no token exists in storage', async () => {
      spyOn(localStorage, 'getItem').and.returnValue(null);

      await service.initializeAuthState();

      expect(service.currentUser()).toBeNull();
    });
  });

  describe('logout', () => {
    it('removes the storage item and clears current user signal', () => {
      service.currentUser.set({
        id: 8,
        firstName: 'Taylor',
        role: UserRole.Client,
        token: 'abc'
      });
      const removeItemSpy = spyOn(localStorage, 'removeItem');

      service.logout('token');

      expect(removeItemSpy).toHaveBeenCalledWith('token');
      expect(service.currentUser()).toBeNull();
    });
  });

  describe('documentation: how these auth tests operate', () => {
    it('builds fake JWT payloads to test token parsing and expiry logic through public methods', async () => {
      const token = createJwt({
        sub: 99,
        given_name: 'Casey',
        role: 'Client',
        exp: Math.floor(Date.now() / 1000) + 120
      });
      spyOn(localStorage, 'getItem').and.returnValue(token);

      await service.initializeAuthState();

      expect(service.isAuthenticated()).toBeTrue();
      expect(service.currentUser()?.firstName).toBe('Casey');
    });
  });
});
