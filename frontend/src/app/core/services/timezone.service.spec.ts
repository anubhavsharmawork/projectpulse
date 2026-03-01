import { TimezoneService } from './timezone.service';
import { TestBed } from '@angular/core/testing';
import { HttpClientTestingModule, HttpTestingController } from '@angular/common/http/testing';
import { DemoAuthService } from '../demo-auth.service';
import { API_BASE_URL } from '../api.config';

describe('TimezoneService', () => {
  let service: TimezoneService;
  let httpMock: HttpTestingController;
  let authService: jasmine.SpyObj<DemoAuthService>;

  beforeEach(() => {
    authService = jasmine.createSpyObj('DemoAuthService', ['getToken']);
    authService.getToken.and.returnValue('test-token');

    TestBed.configureTestingModule({
      imports: [HttpClientTestingModule],
      providers: [
        TimezoneService,
        { provide: DemoAuthService, useValue: authService },
        { provide: API_BASE_URL, useValue: 'http://test' }
      ]
    });

    service = TestBed.inject(TimezoneService);
    httpMock = TestBed.inject(HttpTestingController);
  });

  afterEach(() => httpMock.verify());

  it('should detect timezone info', () => {
    const info = service.detect();
    expect(info.timeZoneId).toBeTruthy();
    expect(typeof info.timeZoneOffset).toBe('number');
  });

  it('should send timezone update to backend', (done) => {
    const tz = { timeZoneId: 'America/New_York', timeZoneOffset: -300 };

    service.updateTimezone(tz).subscribe(res => {
      expect(res.updated).toBeTrue();
      done();
    });

    const req = httpMock.expectOne('http://test/api/v1/users/timezone');
    expect(req.request.method).toBe('PUT');
    expect(req.request.body).toEqual(tz);
    expect(req.request.headers.get('Authorization')).toBe('Bearer test-token');
    req.flush({ updated: true });
  });
});
