import { TestBed } from '@angular/core/testing';
import { HttpClientTestingModule, HttpTestingController } from '@angular/common/http/testing';
import { TenantService, TenantInfo, TenantUsage } from './tenant.service';
import { DemoAuthService } from '../demo-auth.service';
import { API_BASE_URL } from '../api.config';

describe('TenantService', () => {
  let service: TenantService;
  let httpMock: HttpTestingController;
  let authService: jasmine.SpyObj<DemoAuthService>;

  function makeJwt(payload: object): string {
    const h = btoa(JSON.stringify({ alg: 'HS256' }));
    const p = btoa(JSON.stringify(payload));
    return `${h}.${p}.sig`;
  }

  beforeEach(() => {
    authService = jasmine.createSpyObj('DemoAuthService', ['getToken']);

    TestBed.configureTestingModule({
      imports: [HttpClientTestingModule],
      providers: [
        TenantService,
        { provide: DemoAuthService, useValue: authService },
        { provide: API_BASE_URL, useValue: 'http://test' }
      ]
    });

    service = TestBed.inject(TenantService);
    httpMock = TestBed.inject(HttpTestingController);
  });

  afterEach(() => httpMock.verify());

  it('should load current tenant', (done) => {
    const mockTenant: TenantInfo = {
      id: '1', name: 'Test', subdomain: 'test', tier: 'Pro',
      maxUsers: 10, maxProjects: 5, maxStorageBytes: 1024,
      isActive: true, createdAt: '2024-01-01'
    };

    service.loadCurrentTenant().subscribe(tenant => {
      expect(tenant.name).toBe('Test');
      done();
    });

    httpMock.expectOne('http://test/api/v1/tenants/current').flush(mockTenant);
  });

  it('should update tenant$ subject on load', (done) => {
    const mockTenant: TenantInfo = {
      id: '1', name: 'Updated', subdomain: 'test', tier: 'Free',
      maxUsers: 5, maxProjects: 3, maxStorageBytes: 512,
      isActive: true, createdAt: '2024-01-01'
    };

    service.tenant$.subscribe(t => {
      if (t) {
        expect(t.name).toBe('Updated');
        done();
      }
    });

    service.loadCurrentTenant().subscribe();
    httpMock.expectOne('http://test/api/v1/tenants/current').flush(mockTenant);
  });

  it('should handle tenant load error gracefully', (done) => {
    service.loadCurrentTenant().subscribe(result => {
      expect(result).toBeNull();
      done();
    });

    httpMock.expectOne('http://test/api/v1/tenants/current')
      .flush(null, { status: 500, statusText: 'Error' });
  });

  it('should load usage', (done) => {
    const mockUsage: TenantUsage = {
      tier: 'Pro',
      users: { current: 3, max: 10, unlimited: false },
      projects: { current: 2, max: 5, unlimited: false },
      storage: { currentBytes: 100, maxBytes: 1024, unlimited: false }
    };

    service.loadUsage().subscribe(usage => {
      expect(usage.tier).toBe('Pro');
      done();
    });

    httpMock.expectOne('http://test/api/v1/tenants/current/usage').flush(mockUsage);
  });

  it('should handle usage load error gracefully', (done) => {
    service.loadUsage().subscribe(result => {
      expect(result).toBeNull();
      done();
    });

    httpMock.expectOne('http://test/api/v1/tenants/current/usage')
      .flush(null, { status: 500, statusText: 'Error' });
  });

  it('should update tenant via PUT', (done) => {
    const updated: TenantInfo = {
      id: '1', name: 'New Name', subdomain: 'test', tier: 'Pro',
      maxUsers: 10, maxProjects: 5, maxStorageBytes: 1024,
      isActive: true, createdAt: '2024-01-01'
    };

    service.updateTenant('New Name').subscribe(t => {
      expect(t.name).toBe('New Name');
      done();
    });

    const req = httpMock.expectOne('http://test/api/v1/tenants/current');
    expect(req.request.method).toBe('PUT');
    expect(req.request.body).toEqual({ name: 'New Name', settings: undefined });
    req.flush(updated);
  });

  it('should extract tenant ID from JWT', () => {
    authService.getToken.and.returnValue(makeJwt({ tenant_id: 'abc-123' }));
    expect(service.getTenantId()).toBe('abc-123');
  });

  it('should return null tenant ID when no token', () => {
    authService.getToken.and.returnValue(null);
    expect(service.getTenantId()).toBeNull();
  });

  it('should return null tenant ID for malformed token', () => {
    authService.getToken.and.returnValue('bad-token');
    expect(service.getTenantId()).toBeNull();
  });

  it('should extract user role from JWT', () => {
    authService.getToken.and.returnValue(makeJwt({ role: 'Admin' }));
    expect(service.getUserRole()).toBe('Admin');
  });

  it('should return null role when no token', () => {
    authService.getToken.and.returnValue(null);
    expect(service.getUserRole()).toBeNull();
  });

  it('should extract user role from claims URI', () => {
    authService.getToken.and.returnValue(makeJwt({
      'http://schemas.microsoft.com/ws/2008/06/identity/claims/role': 'Manager'
    }));
    expect(service.getUserRole()).toBe('Manager');
  });

  it('should return null role for malformed token', () => {
    authService.getToken.and.returnValue('bad-token');
    expect(service.getUserRole()).toBeNull();
  });

  it('should extract system role from JWT', () => {
    authService.getToken.and.returnValue(makeJwt({ system_role: 'SystemAdmin' }));
    expect(service.getSystemRole()).toBe('SystemAdmin');
  });

  it('should return null system role when no token', () => {
    authService.getToken.and.returnValue(null);
    expect(service.getSystemRole()).toBeNull();
  });

  it('should return null system role for malformed token', () => {
    authService.getToken.and.returnValue('bad');
    expect(service.getSystemRole()).toBeNull();
  });

  it('should detect system admin', () => {
    authService.getToken.and.returnValue(makeJwt({ system_role: 'SystemAdmin' }));
    expect(service.isSystemAdmin()).toBeTrue();
  });

  it('should detect non-system-admin', () => {
    authService.getToken.and.returnValue(makeJwt({ system_role: 'User' }));
    expect(service.isSystemAdmin()).toBeFalse();
  });

  it('should detect tenant admin', () => {
    authService.getToken.and.returnValue(makeJwt({ role: 'Admin' }));
    expect(service.isTenantAdmin()).toBeTrue();
  });

  it('should detect non-tenant-admin', () => {
    authService.getToken.and.returnValue(makeJwt({ role: 'Member' }));
    expect(service.isTenantAdmin()).toBeFalse();
  });

  describe('hasFeature', () => {
    it('should allow Enterprise tier all features', () => {
      expect(service.hasFeature('Enterprise', 'sso')).toBeTrue();
      expect(service.hasFeature('Enterprise', 'custom-workflows')).toBeTrue();
    });

    it('should allow Business tier level-2 features', () => {
      expect(service.hasFeature('Business', 'custom-workflows')).toBeTrue();
      expect(service.hasFeature('Business', 'audit-logs')).toBeTrue();
    });

    it('should deny Starter tier level-2 features', () => {
      expect(service.hasFeature('Starter', 'custom-workflows')).toBeFalse();
      expect(service.hasFeature('Starter', 'sso')).toBeFalse();
    });

    it('should deny Business tier level-3 features', () => {
      expect(service.hasFeature('Business', 'sso')).toBeFalse();
      expect(service.hasFeature('Business', 'unlimited-users')).toBeFalse();
    });

    it('should default unknown tier to level 1', () => {
      expect(service.hasFeature('Unknown', 'custom-workflows')).toBeFalse();
    });

    it('should default unknown feature to level 1', () => {
      expect(service.hasFeature('Starter', 'unknown-feature')).toBeTrue();
    });
  });

  it('should clear tenant and usage subjects', () => {
    service.clear();
    service.tenant$.subscribe(t => expect(t).toBeNull());
    service.usage$.subscribe(u => expect(u).toBeNull());
  });
});
