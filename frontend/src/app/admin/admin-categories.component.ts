import { Component, OnInit } from '@angular/core';
import { CustomFieldService, CustomFieldDto } from '../core/services/custom-field.service';
import { DomainColorService, DomainColorScheme } from '../core/services/domain-color.service';
import { AdminAuthService } from '../core/services/admin-auth.service';

@Component({
  selector: 'app-admin-categories',
  templateUrl: './admin-categories.component.html',
  styles: [`
    h3 { margin: 0 0 0.25rem; font-size: 1.25rem; font-weight: 600; color: #1e293b; }
    h4 { margin: 1.5rem 0 0.75rem; font-size: 1rem; font-weight: 600; color: #374151; }
    .admin-subtitle { color: #64748b; font-size: 0.875rem; margin-bottom: 1.5rem; }

    .readonly-banner {
      display: flex; align-items: center; gap: 0.5rem;
      padding: 0.625rem 1rem; border-radius: 8px; margin-bottom: 1rem;
      background: #fef3c7; color: #92400e; font-size: 0.8125rem; font-weight: 500;
      border: 1px solid #fde68a;
    }

    .domain-selector { display: flex; align-items: center; gap: 0.75rem; margin-bottom: 1rem; }
    .domain-selector label { font-size: 0.875rem; font-weight: 500; color: #374151; }
    .domain-selector select { padding: 0.5rem 0.875rem; border: 1px solid #e2e8f0; border-radius: 8px; font-size: 0.875rem; }
    .domain-indicator {
      display: inline-block; padding: 0.2rem 0.625rem; border-radius: 999px;
      font-size: 0.6875rem; font-weight: 600; border: 1px solid transparent;
    }

    .category-cards { display: grid; grid-template-columns: repeat(auto-fill, minmax(240px, 1fr)); gap: 1rem; }
    .cat-card {
      background: #fff; border: 1px solid #e2e8f0; border-radius: 10px;
      padding: 1rem; box-shadow: 0 1px 3px rgba(0,0,0,0.04);
      border-left: 3px solid #94a3b8;
    }
    .cat-name { font-weight: 600; color: #1e293b; margin-bottom: 0.25rem; }
    .cat-desc { font-size: 0.8125rem; color: #64748b; margin-bottom: 0.5rem; }
    .cat-roles { display: flex; flex-wrap: wrap; gap: 0.375rem; }
    .role-tag { display: inline-block; padding: 1px 6px; border-radius: 4px; font-size: 0.6875rem; font-weight: 600; }

    .fields-table {
      width: 100%; border-collapse: collapse; background: #fff; border-radius: 12px;
      overflow: hidden; box-shadow: 0 1px 3px rgba(0,0,0,0.04); border: 1px solid #e2e8f0;
    }
    .fields-table th {
      background: #f8fafc; padding: 0.625rem 1rem; text-align: left;
      font-size: 0.75rem; font-weight: 600; color: #64748b; text-transform: uppercase; letter-spacing: 0.05em;
      border-bottom: 1px solid #e2e8f0;
    }
    .fields-table td { padding: 0.625rem 1rem; font-size: 0.8125rem; color: #374151; border-bottom: 1px solid #f1f5f9; }
    .fields-table tr:last-child td { border-bottom: none; }
    .field-name { font-weight: 600; }
    .type-badge { display: inline-block; padding: 1px 6px; border-radius: 4px; font-size: 0.6875rem; font-weight: 600; }
    .entity-type-badge { display: inline-block; padding: 1px 6px; border-radius: 4px; font-size: 0.6875rem; font-weight: 500; background: #f1f5f9; color: #475569; }
    .req-yes { color: #ef4444; font-weight: 600; }
    .req-no { color: #94a3b8; }

    /* Options/Validation cells: allow wrapping so full content is visible */
    .options-cell {
      font-size: 0.75rem; color: #64748b;
      max-width: 320px; white-space: normal; word-break: break-word; line-height: 1.45;
    }

    .loading-text { color: #94a3b8; font-style: italic; }
    .error-text { color: #ef4444; font-size: 0.875rem; padding: 0.5rem 0; }
    .empty { color: #94a3b8; font-style: italic; padding: 0.5rem 0; }
  `]
})
export class AdminCategoriesComponent implements OnInit {
  domains = ['IT', 'Healthcare', 'Construction', 'Infrastructure', 'Technology', 'PublicSafety', 'EconomicDevelopment'];
  selectedDomain = 'IT';
  fields: CustomFieldDto[] = [];
  loadingFields = false;
  fieldsError = '';
  isAdmin = false;

  /** Domain color scheme — updates when selectedDomain changes */
  colors: DomainColorScheme;

  // Static category definitions matching seeded data
  allCategories: { domain: string; name: string; description: string; roles: string[] }[] = [
    { domain: 'IT', name: 'Software Development', description: 'Agile development projects', roles: ['Developer', 'QA', 'Scrum Master', 'Product Owner'] },
    { domain: 'IT', name: 'Infrastructure', description: 'IT infrastructure and DevOps', roles: ['DevOps Engineer', 'SRE', 'Network Admin'] },
    { domain: 'Healthcare', name: 'Clinical Compliance', description: 'Regulatory and compliance initiatives', roles: ['Compliance Officer', 'Clinical Lead', 'Auditor'] },
    { domain: 'Healthcare', name: 'Patient Safety', description: 'Patient safety improvement projects', roles: ['Safety Officer', 'Nurse Manager', 'Quality Analyst'] },
    { domain: 'Construction', name: 'Building Construction', description: 'Commercial and residential builds', roles: ['Site Manager', 'Foreman', 'Safety Inspector'] },
    { domain: 'Construction', name: 'Civil Engineering', description: 'Roads, bridges, and utilities', roles: ['Project Engineer', 'Surveyor', 'Inspector'] },
    { domain: 'Infrastructure', name: 'Public Works', description: 'Council infrastructure', roles: ['Project Manager', 'Engineer', 'Inspector'] },
    { domain: 'Technology', name: 'Product Development', description: 'New product R&D', roles: ['Product Manager', 'Engineer', 'Designer'] },
    { domain: 'PublicSafety', name: 'Emergency Management', description: 'Disaster preparedness', roles: ['Coordinator', 'First Responder Lead'] },
    { domain: 'EconomicDevelopment', name: 'Grant Management', description: 'Government and regional grants', roles: ['Grant Writer', 'Program Manager'] }
  ];

  get categoriesForDomain() {
    return this.allCategories.filter(c => c.domain === this.selectedDomain);
  }

  constructor(
    private cfSvc: CustomFieldService,
    private domainColorsSvc: DomainColorService,
    private adminAuth: AdminAuthService
  ) {
    this.colors = this.domainColorsSvc.getColors(this.selectedDomain);
  }

  ngOnInit() {
    this.isAdmin = this.adminAuth.isAdmin();
    this.loadFields();
  }

  /** Called when the domain dropdown changes — refreshes colors, categories, and fields */
  onDomainChange() {
    this.colors = this.domainColorsSvc.getColors(this.selectedDomain);
    this.loadFields();
  }

  loadFields() {
    this.loadingFields = true;
    this.fieldsError = '';
    this.cfSvc.getFieldsByDomain(this.selectedDomain).subscribe({
      next: f => { this.fields = f; this.loadingFields = false; },
      error: () => {
        this.fields = [];
        this.fieldsError = 'Failed to load custom fields. Please try again.';
        this.loadingFields = false;
      }
    });
  }
}
