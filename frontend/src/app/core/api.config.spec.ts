import { TestBed } from '@angular/core/testing';
import { API_BASE_URL } from './api.config';

describe('API_BASE_URL', () => {
  it('should provide a string value', () => {
    TestBed.configureTestingModule({});
    const baseUrl = TestBed.inject(API_BASE_URL);
    expect(typeof baseUrl).toBe('string');
  });
});
