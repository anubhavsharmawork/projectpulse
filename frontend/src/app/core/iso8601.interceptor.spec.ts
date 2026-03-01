import { TestBed } from '@angular/core/testing';
import { HttpClientTestingModule, HttpTestingController } from '@angular/common/http/testing';
import { HTTP_INTERCEPTORS, HttpClient } from '@angular/common/http';
import { Iso8601Interceptor } from './iso8601.interceptor';

describe('Iso8601Interceptor', () => {
  let httpClient: HttpClient;
  let httpMock: HttpTestingController;

  beforeEach(() => {
    TestBed.configureTestingModule({
      imports: [HttpClientTestingModule],
      providers: [
        { provide: HTTP_INTERCEPTORS, useClass: Iso8601Interceptor, multi: true }
      ]
    });

    httpClient = TestBed.inject(HttpClient);
    httpMock = TestBed.inject(HttpTestingController);
  });

  afterEach(() => httpMock.verify());

  it('should pass requests with no body unchanged', () => {
    httpClient.get('/api/test').subscribe();
    const req = httpMock.expectOne('/api/test');
    expect(req.request.body).toBeNull();
    req.flush({});
  });

  it('should convert Date objects to ISO strings', () => {
    const date = new Date('2024-06-15T10:30:00Z');
    httpClient.post('/api/test', { createdAt: date }).subscribe();
    const req = httpMock.expectOne('/api/test');
    expect(req.request.body.createdAt).toBe('2024-06-15T10:30:00.000Z');
    req.flush({});
  });

  it('should handle nested Date objects', () => {
    const date = new Date('2024-01-01T00:00:00Z');
    httpClient.post('/api/test', { nested: { when: date } }).subscribe();
    const req = httpMock.expectOne('/api/test');
    expect(req.request.body.nested.when).toBe('2024-01-01T00:00:00.000Z');
    req.flush({});
  });

  it('should handle arrays with Date objects', () => {
    const date = new Date('2024-03-01T12:00:00Z');
    httpClient.post('/api/test', { items: [{ date }] }).subscribe();
    const req = httpMock.expectOne('/api/test');
    expect(req.request.body.items[0].date).toBe('2024-03-01T12:00:00.000Z');
    req.flush({});
  });

  it('should preserve non-date string values', () => {
    httpClient.post('/api/test', { name: 'test', count: 42 }).subscribe();
    const req = httpMock.expectOne('/api/test');
    expect(req.request.body.name).toBe('test');
    expect(req.request.body.count).toBe(42);
    req.flush({});
  });

  it('should not modify FormData bodies', () => {
    const formData = new FormData();
    formData.append('file', 'data');
    httpClient.post('/api/upload', formData).subscribe();
    const req = httpMock.expectOne('/api/upload');
    expect(req.request.body instanceof FormData).toBeTrue();
    req.flush({});
  });

  it('should handle null body values', () => {
    httpClient.post('/api/test', { field: null, other: 'ok' }).subscribe();
    const req = httpMock.expectOne('/api/test');
    expect(req.request.body.field).toBeNull();
    expect(req.request.body.other).toBe('ok');
    req.flush({});
  });
});
