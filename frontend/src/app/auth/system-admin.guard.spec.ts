import { TestBed } from '@angular/core/testing';
import { Router, UrlTree } from '@angular/router';
import { SystemAdminGuard } from './system-admin.guard';
import { TenantService } from '../core/services/tenant.service';

describe('SystemAdminGuard', () => {
  let guard: SystemAdminGuard;
  let tenantService: jasmine.SpyObj<TenantService>;
  let router: jasmine.SpyObj<Router>;

  beforeEach(() => {
    tenantService = jasmine.createSpyObj('TenantService', ['isSystemAdmin']);
    router = jasmine.createSpyObj('Router', ['createUrlTree']);
    router.createUrlTree.and.returnValue({} as UrlTree);

    TestBed.configureTestingModule({
      providers: [
        SystemAdminGuard,
        { provide: TenantService, useValue: tenantService },
        { provide: Router, useValue: router }
      ]
    });

    guard = TestBed.inject(SystemAdminGuard);
  });

  describe('canMatch', () => {
    it('should return true for system admins', () => {
      tenantService.isSystemAdmin.and.returnValue(true);
      expect(guard.canMatch({} as any, [])).toBeTrue();
    });

    it('should redirect non-admins to /projects', () => {
      tenantService.isSystemAdmin.and.returnValue(false);
      guard.canMatch({} as any, []);
      expect(router.createUrlTree).toHaveBeenCalledWith(['/projects']);
    });
  });

  describe('canActivate', () => {
    it('should return true for system admins', () => {
      tenantService.isSystemAdmin.and.returnValue(true);
      expect(guard.canActivate({} as any, {} as any)).toBeTrue();
    });

    it('should redirect non-admins to /projects', () => {
      tenantService.isSystemAdmin.and.returnValue(false);
      guard.canActivate({} as any, {} as any);
      expect(router.createUrlTree).toHaveBeenCalledWith(['/projects']);
    });
  });
});
