import { TestBed } from '@angular/core/testing';
import { Router, UrlTree } from '@angular/router';
import { AuthGuard } from './auth.guard';
import { DemoAuthService } from '../core/demo-auth.service';

describe('AuthGuard', () => {
  let guard: AuthGuard;
  let authService: jasmine.SpyObj<DemoAuthService>;
  let router: jasmine.SpyObj<Router>;

  beforeEach(() => {
    authService = jasmine.createSpyObj('DemoAuthService', ['isTokenExpired', 'clear']);
    router = jasmine.createSpyObj('Router', ['createUrlTree']);
    const fakeTree = {} as UrlTree;
    router.createUrlTree.and.returnValue(fakeTree);

    TestBed.configureTestingModule({
      providers: [
        AuthGuard,
        { provide: DemoAuthService, useValue: authService },
        { provide: Router, useValue: router }
      ]
    });

    guard = TestBed.inject(AuthGuard);
  });

  describe('canMatch', () => {
    it('should return true when token is valid', () => {
      authService.isTokenExpired.and.returnValue(false);
      const result = guard.canMatch({} as any, []);
      expect(result).toBeTrue();
    });

    it('should redirect to login when token is expired', () => {
      authService.isTokenExpired.and.returnValue(true);
      guard.canMatch({} as any, []);
      expect(authService.clear).toHaveBeenCalled();
      expect(router.createUrlTree).toHaveBeenCalledWith(['/auth/login'], {
        queryParams: { redirectUrl: '/' }
      });
    });

    it('should reconstruct URL from segments', () => {
      authService.isTokenExpired.and.returnValue(true);
      const segments = [{ path: 'projects' }, { path: '123' }] as any[];
      guard.canMatch({} as any, segments);
      expect(router.createUrlTree).toHaveBeenCalledWith(['/auth/login'], {
        queryParams: { redirectUrl: '/projects/123' }
      });
    });
  });

  describe('canActivate', () => {
    it('should return true when token is valid', () => {
      authService.isTokenExpired.and.returnValue(false);
      const result = guard.canActivate({} as any, { url: '/dashboard' } as any);
      expect(result).toBeTrue();
    });

    it('should redirect to login when token is expired', () => {
      authService.isTokenExpired.and.returnValue(true);
      guard.canActivate({} as any, { url: '/dashboard' } as any);
      expect(authService.clear).toHaveBeenCalled();
      expect(router.createUrlTree).toHaveBeenCalledWith(['/auth/login'], {
        queryParams: { redirectUrl: '/dashboard' }
      });
    });
  });
});
