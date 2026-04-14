import { TestBed } from '@angular/core/testing';
import { HttpEvent, HttpHandlerFn, HttpInterceptorFn, HttpRequest, HttpResponse } from '@angular/common/http';
import { Observable, of } from 'rxjs';
import { AccountService } from '../services/account.service';

import { jwtInterceptorInterceptor } from './jwt-interceptor.interceptor';

describe('jwtInterceptorInterceptor', () => {
  const interceptor: HttpInterceptorFn = (req, next) => 
    TestBed.runInInjectionContext(() => jwtInterceptorInterceptor(req, next));

  let accountServiceSpy: jasmine.SpyObj<AccountService>;
  let nextSpy: jasmine.Spy<HttpHandlerFn>;

  const createRequest = () => new HttpRequest('GET', '/api/resource');
  const createNext = (): HttpHandlerFn =>
    ((req: HttpRequest<unknown>): Observable<HttpEvent<unknown>> => {
      return of(new HttpResponse({ status: 200, body: req.url }));
    }) as HttpHandlerFn;

  beforeEach(() => {
    // Shared dependency mock for all test scenarios.
    accountServiceSpy = jasmine.createSpyObj<AccountService>('AccountService', ['currentUser']);

    TestBed.configureTestingModule({
      providers: [{ provide: AccountService, useValue: accountServiceSpy }]
    });

    nextSpy = spyOn({ handle: createNext() }, 'handle').and.callThrough();
  });

  describe('when a logged-in user exists', () => {
    it('adds Authorization bearer header before forwarding request', () => {
      accountServiceSpy.currentUser.and.returnValue({ token: 'jwt-token-123' } as any);
      const request = createRequest();

      interceptor(request, nextSpy);
      const forwardedRequest = nextSpy.calls.mostRecent().args[0] as HttpRequest<unknown>;

      expect(forwardedRequest).not.toBe(request);
      expect(forwardedRequest.headers.get('Authorization')).toBe('Bearer jwt-token-123');
      expect(request.headers.has('Authorization')).toBeFalse();
    });
  });

  describe('when no logged-in user exists', () => {
    it('forwards original request unchanged', () => {
      accountServiceSpy.currentUser.and.returnValue(null);
      const request = createRequest();

      interceptor(request, nextSpy);
      const forwardedRequest = nextSpy.calls.mostRecent().args[0] as HttpRequest<unknown>;

      expect(forwardedRequest).toBe(request);
      expect(forwardedRequest.headers.has('Authorization')).toBeFalse();
    });
  });

  describe('documentation: how this interceptor test works', () => {
    it('executes inside runInInjectionContext so inject(AccountService) resolves the mock', () => {
      accountServiceSpy.currentUser.and.returnValue({ token: 'context-token' } as any);

      interceptor(createRequest(), nextSpy);

      expect(accountServiceSpy.currentUser).toHaveBeenCalled();
      expect(nextSpy).toHaveBeenCalled();
    });
  });
});
