import { TestBed } from '@angular/core/testing';
import { HttpClientTestingModule, HttpTestingController } from '@angular/common/http/testing';
import { HTTP_INTERCEPTORS, HttpClient, HttpErrorResponse } from '@angular/common/http';
import { Router } from '@angular/router';
import { AuthInterceptor } from './auth.interceptor';
import { DemoAuthService } from './demo-auth.service';

describe('AuthInterceptor', () => {
  let httpClient: HttpClient;
  let httpMock: HttpTestingController;
  let authService: jasmine.SpyObj<DemoAuthService>;
  let router: jasmine.SpyObj<Router>;

  beforeEach(() => {
    authService = jasmine.createSpyObj('DemoAuthService', ['clear']);
    router = jasmine.createSpyObj('Router', ['navigate'], { url: '/projects' });
    router.navigate.and.returnValue(Promise.resolve(true));

    TestBed.configureTestingModule({
      imports: [HttpClientTestingModule],
      providers: [
        { provide: HTTP_INTERCEPTORS, useClass: AuthInterceptor, multi: true },
        { provide: DemoAuthService, useValue: authService },
        { provide: Router, useValue: router }
      ]
    });

    httpClient = TestBed.inject(HttpClient);
    httpMock = TestBed.inject(HttpTestingController);
  });

  afterEach(() => httpMock.verify());

  it('should pass through successful responses', () => {
    httpClient.get('/api/test').subscribe(res => {
      expect(res).toEqual({ ok: true });
    });
    httpMock.expectOne('/api/test').flush({ ok: true });
  });

  it('should clear auth and navigate on 401 when Authorization header present', () => {
    httpClient.get('/api/test', {
      headers: { Authorization: 'Bearer token123' }
    }).subscribe({
      error: (err: HttpErrorResponse) => {
        expect(err.status).toBe(401);
      }
    });

    httpMock.expectOne('/api/test').flush(null, { status: 401, statusText: 'Unauthorized' });
    expect(authService.clear).toHaveBeenCalled();
    expect(router.navigate).toHaveBeenCalledWith(['/auth/login'], {
      queryParams: { redirectUrl: '/projects' }
    });
  });

  it('should NOT clear auth on 401 without Authorization header', () => {
    httpClient.get('/api/tenants/current').subscribe({
      error: (err: HttpErrorResponse) => {
        expect(err.status).toBe(401);
      }
    });

    httpMock.expectOne('/api/tenants/current').flush(null, { status: 401, statusText: 'Unauthorized' });
    expect(authService.clear).not.toHaveBeenCalled();
    expect(router.navigate).not.toHaveBeenCalled();
  });

  it('should NOT navigate when already on auth page', () => {
    (Object.getOwnPropertyDescriptor(router, 'url')!.get as jasmine.Spy).and.returnValue('/auth/login');

    httpClient.get('/api/test', {
      headers: { Authorization: 'Bearer token123' }
    }).subscribe({
      error: () => {}
    });

    httpMock.expectOne('/api/test').flush(null, { status: 401, statusText: 'Unauthorized' });
    expect(authService.clear).toHaveBeenCalled();
    expect(router.navigate).not.toHaveBeenCalled();
  });

  it('should redirect to /legal/accept on 451', () => {
    httpClient.get('/api/test', {
      headers: { Authorization: 'Bearer token123' }
    }).subscribe({
      error: (err: HttpErrorResponse) => {
        expect(err.status).toBe(451);
      }
    });

    httpMock.expectOne('/api/test').flush(null, { status: 451, statusText: 'Unavailable For Legal Reasons' });
    expect(router.navigate).toHaveBeenCalledWith(['/legal/accept']);
  });

  it('should NOT redirect to legal when already on /legal page', () => {
    (Object.getOwnPropertyDescriptor(router, 'url')!.get as jasmine.Spy).and.returnValue('/legal/accept');

    httpClient.get('/api/test').subscribe({
      error: () => {}
    });

    httpMock.expectOne('/api/test').flush(null, { status: 451, statusText: 'Unavailable For Legal Reasons' });
    expect(router.navigate).not.toHaveBeenCalledWith(['/legal/accept']);
  });

  it('should NOT redirect to legal when on /auth page', () => {
    (Object.getOwnPropertyDescriptor(router, 'url')!.get as jasmine.Spy).and.returnValue('/auth/login');

    httpClient.get('/api/test').subscribe({
      error: () => {}
    });

    httpMock.expectOne('/api/test').flush(null, { status: 451, statusText: 'Unavailable For Legal Reasons' });
    expect(router.navigate).not.toHaveBeenCalledWith(['/legal/accept']);
  });

  it('should pass through non-401/451 errors untouched', () => {
    httpClient.get('/api/test').subscribe({
      error: (err: HttpErrorResponse) => {
        expect(err.status).toBe(500);
      }
    });

    httpMock.expectOne('/api/test').flush(null, { status: 500, statusText: 'Server Error' });
    expect(authService.clear).not.toHaveBeenCalled();
  });
});
