import { TestBed } from '@angular/core/testing';
import { Router, UrlTree } from '@angular/router';
import { of, throwError } from 'rxjs';
import { LegalGuard } from './legal.guard';
import { DemoAuthService } from '../core/demo-auth.service';
import { LegalService, LegalStatusDto } from './legal.service';

describe('LegalGuard', () => {
  let guard: LegalGuard;
  let authService: jasmine.SpyObj<DemoAuthService>;
  let legalService: jasmine.SpyObj<LegalService>;
  let router: jasmine.SpyObj<Router>;

  beforeEach(() => {
    authService = jasmine.createSpyObj('DemoAuthService', ['getToken']);
    legalService = jasmine.createSpyObj('LegalService', ['getStatus']);
    router = jasmine.createSpyObj('Router', ['createUrlTree']);
    router.createUrlTree.and.returnValue({} as UrlTree);

    TestBed.configureTestingModule({
      providers: [
        LegalGuard,
        { provide: DemoAuthService, useValue: authService },
        { provide: LegalService, useValue: legalService },
        { provide: Router, useValue: router }
      ]
    });

    guard = TestBed.inject(LegalGuard);
    sessionStorage.clear();
  });

  afterEach(() => sessionStorage.clear());

  it('should return true when not logged in', () => {
    authService.getToken.and.returnValue(null);
    expect(guard.canActivate()).toBeTrue();
  });

  it('should return true when session already accepted', () => {
    authService.getToken.and.returnValue('token');
    sessionStorage.setItem('legal_accepted', 'true');
    expect(guard.canActivate()).toBeTrue();
  });

  it('should allow access when no acceptance required', (done) => {
    authService.getToken.and.returnValue('token');
    legalService.getStatus.and.returnValue(of({
      requiresAcceptance: false
    } as LegalStatusDto));

    const result = guard.canActivate();
    if (result instanceof Object && 'subscribe' in result) {
      (result as any).subscribe((val: boolean | UrlTree) => {
        expect(val).toBeTrue();
        expect(sessionStorage.getItem('legal_accepted')).toBe('true');
        done();
      });
    }
  });

  it('should redirect to /legal/accept when acceptance required', (done) => {
    authService.getToken.and.returnValue('token');
    legalService.getStatus.and.returnValue(of({
      requiresAcceptance: true
    } as LegalStatusDto));

    const result = guard.canActivate();
    if (result instanceof Object && 'subscribe' in result) {
      (result as any).subscribe(() => {
        expect(router.createUrlTree).toHaveBeenCalledWith(['/legal/accept']);
        done();
      });
    }
  });

  it('should allow access on API error', (done) => {
    authService.getToken.and.returnValue('token');
    legalService.getStatus.and.returnValue(throwError(() => new Error('Network error')));

    const result = guard.canActivate();
    if (result instanceof Object && 'subscribe' in result) {
      (result as any).subscribe((val: boolean | UrlTree) => {
        expect(val).toBeTrue();
        done();
      });
    }
  });
});
