import { TestBed } from '@angular/core/testing';
import { HttpClientTestingModule, HttpTestingController } from '@angular/common/http/testing';
import { LegalService, LegalDocumentDto, LegalStatusDto } from './legal.service';
import { DemoAuthService } from '../core/demo-auth.service';
import { API_BASE_URL } from '../core/api.config';

describe('LegalService', () => {
  let service: LegalService;
  let httpMock: HttpTestingController;
  let authService: jasmine.SpyObj<DemoAuthService>;

  beforeEach(() => {
    authService = jasmine.createSpyObj('DemoAuthService', ['getToken']);
    authService.getToken.and.returnValue('test-token');

    TestBed.configureTestingModule({
      imports: [HttpClientTestingModule],
      providers: [
        LegalService,
        { provide: DemoAuthService, useValue: authService },
        { provide: API_BASE_URL, useValue: 'http://test' }
      ]
    });

    service = TestBed.inject(LegalService);
    httpMock = TestBed.inject(HttpTestingController);
  });

  afterEach(() => httpMock.verify());

  it('should fetch terms', (done) => {
    const terms: LegalDocumentDto = {
      id: '1', documentType: 'TermsOfService', version: '1.0',
      effectiveDate: '2024-01-01', content: 'Terms content'
    };

    service.getTerms().subscribe(res => {
      expect(res.documentType).toBe('TermsOfService');
      done();
    });

    httpMock.expectOne('http://test/api/v1/legal/terms').flush(terms);
  });

  it('should fetch privacy policy', (done) => {
    const privacy: LegalDocumentDto = {
      id: '2', documentType: 'PrivacyPolicy', version: '1.0',
      effectiveDate: '2024-01-01', content: 'Privacy content'
    };

    service.getPrivacy().subscribe(res => {
      expect(res.documentType).toBe('PrivacyPolicy');
      done();
    });

    httpMock.expectOne('http://test/api/v1/legal/privacy').flush(privacy);
  });

  it('should fetch legal status with auth header', (done) => {
    const status: LegalStatusDto = {
      termsAccepted: true,
      acceptedTermsVersion: '1.0',
      currentTermsVersion: '1.0',
      privacyAccepted: true,
      acceptedPrivacyVersion: '1.0',
      currentPrivacyVersion: '1.0',
      requiresAcceptance: false
    };

    service.getStatus().subscribe(res => {
      expect(res.requiresAcceptance).toBeFalse();
      done();
    });

    const req = httpMock.expectOne('http://test/api/v1/legal/status');
    expect(req.request.headers.get('Authorization')).toBe('Bearer test-token');
    req.flush(status);
  });

  it('should accept legal documents', (done) => {
    service.accept('1.0', '1.0').subscribe(res => {
      expect(res.accepted).toBeTrue();
      done();
    });

    const req = httpMock.expectOne('http://test/api/v1/legal/accept');
    expect(req.request.method).toBe('POST');
    req.flush({ accepted: true });
  });
});
