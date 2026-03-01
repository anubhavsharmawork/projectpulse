import { DemoAuthService } from './demo-auth.service';

describe('DemoAuthService', () => {
  let service: DemoAuthService;
  const tokenKey = 'demo_jwt';

  function makeJwt(payloadObj: object): string {
    const header = btoa(JSON.stringify({ alg: 'HS256', typ: 'JWT' }));
    const payload = btoa(JSON.stringify(payloadObj));
    return `${header}.${payload}.sig`;
  }

  beforeEach(() => {
    localStorage.clear();
    service = new DemoAuthService();
  });

  afterEach(() => {
    localStorage.clear();
  });

  it('should return null when no token stored', () => {
    expect(service.getToken()).toBeNull();
  });

  it('should store and retrieve a valid token', () => {
    const futureExp = Math.floor(Date.now() / 1000) + 3600;
    const token = makeJwt({ exp: futureExp, sub: 'user1' });
    service.setToken(token);
    expect(service.getToken()).toBe(token);
    expect(localStorage.getItem(tokenKey)).toBe(token);
  });

  it('should emit token via tokenChanges$', (done) => {
    const futureExp = Math.floor(Date.now() / 1000) + 3600;
    const token = makeJwt({ exp: futureExp });
    const emissions: (string | null)[] = [];
    const sub = service.tokenChanges$.subscribe(t => {
      emissions.push(t);
      if (emissions.length === 2) {
        expect(emissions[0]).toBeNull();
        expect(emissions[1]).toBe(token);
        sub.unsubscribe();
        done();
      }
    });
    service.setToken(token);
  });

  it('should clear token', () => {
    const futureExp = Math.floor(Date.now() / 1000) + 3600;
    service.setToken(makeJwt({ exp: futureExp }));
    service.clear();
    expect(service.getToken()).toBeNull();
    expect(localStorage.getItem(tokenKey)).toBeNull();
  });

  it('should return null for expired token', () => {
    const pastExp = Math.floor(Date.now() / 1000) - 3600;
    service.setToken(makeJwt({ exp: pastExp }));
    expect(service.getToken()).toBeNull();
  });

  it('should auto-remove expired token from localStorage', () => {
    const pastExp = Math.floor(Date.now() / 1000) - 3600;
    const token = makeJwt({ exp: pastExp });
    localStorage.setItem(tokenKey, token);
    const svc = new DemoAuthService();
    expect(svc.getToken()).toBeNull();
    expect(localStorage.getItem(tokenKey)).toBeNull();
  });

  describe('isTokenExpired', () => {
    it('should return true when no token', () => {
      expect(service.isTokenExpired()).toBeTrue();
    });

    it('should return true for expired token', () => {
      const pastExp = Math.floor(Date.now() / 1000) - 100;
      localStorage.setItem(tokenKey, makeJwt({ exp: pastExp }));
      expect(service.isTokenExpired()).toBeTrue();
    });

    it('should return false for valid token', () => {
      const futureExp = Math.floor(Date.now() / 1000) + 3600;
      localStorage.setItem(tokenKey, makeJwt({ exp: futureExp }));
      expect(service.isTokenExpired()).toBeFalse();
    });

    it('should return true for token within 60s buffer', () => {
      const almostExpired = Math.floor(Date.now() / 1000) + 30;
      localStorage.setItem(tokenKey, makeJwt({ exp: almostExpired }));
      expect(service.isTokenExpired()).toBeTrue();
    });

    it('should return true for malformed token', () => {
      localStorage.setItem(tokenKey, 'not-a-jwt');
      expect(service.isTokenExpired()).toBeTrue();
    });

    it('should return true for token without exp claim', () => {
      localStorage.setItem(tokenKey, makeJwt({ sub: 'user1' }));
      expect(service.isTokenExpired()).toBeTrue();
    });

    it('should return true for token with invalid base64', () => {
      localStorage.setItem(tokenKey, 'a.!!!.c');
      expect(service.isTokenExpired()).toBeTrue();
    });
  });
});
