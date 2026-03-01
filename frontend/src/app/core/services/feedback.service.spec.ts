import { TestBed } from '@angular/core/testing';
import { HttpClientTestingModule, HttpTestingController } from '@angular/common/http/testing';
import { FeedbackService, SubmitFeedbackResponse } from './feedback.service';
import { DemoAuthService } from '../demo-auth.service';
import { API_BASE_URL } from '../api.config';

describe('FeedbackService', () => {
  let service: FeedbackService;
  let httpMock: HttpTestingController;
  let authService: jasmine.SpyObj<DemoAuthService>;

  beforeEach(() => {
    authService = jasmine.createSpyObj('DemoAuthService', ['getToken']);

    TestBed.configureTestingModule({
      imports: [HttpClientTestingModule],
      providers: [
        FeedbackService,
        { provide: DemoAuthService, useValue: authService },
        { provide: API_BASE_URL, useValue: 'http://test' }
      ]
    });

    service = TestBed.inject(FeedbackService);
    httpMock = TestBed.inject(HttpTestingController);
  });

  afterEach(() => httpMock.verify());

  it('should submit feedback with auth header when token exists', (done) => {
    authService.getToken.and.returnValue('my-token');
    const response: SubmitFeedbackResponse = { feedbackId: 'fb-1' };

    service.submit('Great app!').subscribe(res => {
      expect(res.feedbackId).toBe('fb-1');
      done();
    });

    const req = httpMock.expectOne('http://test/api/v1/feedback');
    expect(req.request.method).toBe('POST');
    expect(req.request.body).toEqual({ message: 'Great app!' });
    expect(req.request.headers.get('Authorization')).toBe('Bearer my-token');
    req.flush(response);
  });

  it('should submit feedback without auth header when no token', (done) => {
    authService.getToken.and.returnValue(null);
    const response: SubmitFeedbackResponse = { feedbackId: 'fb-2' };

    service.submit('Bug report').subscribe(res => {
      expect(res.feedbackId).toBe('fb-2');
      done();
    });

    const req = httpMock.expectOne('http://test/api/v1/feedback');
    expect(req.request.headers.has('Authorization')).toBeFalse();
    req.flush(response);
  });
});
