import { Component, OnInit } from '@angular/core';
import { ActivatedRoute, Router } from '@angular/router';
import { AssetService } from '../asset.service';
import { ProjectsService } from '../../projects/projects.service';
import {
  AssetListItem,
  AssetStatus,
  AssetType,
  DomainType,
  DomainAssetConfigItem,
  formatAssetStatus,
  formatAssetType,
  getStatusColor
} from '../asset.model';

@Component({
  selector: 'app-asset-list',
  templateUrl: './asset-list.component.html',
  styles: [`
    .assets-header { display: flex; justify-content: space-between; align-items: center; margin-bottom: 1rem; }
    .assets-header h2 { margin: 0; font-size: 1.75rem; font-weight: 600; color: #1e293b; }
    .asset-count { background: #e2e8f0; color: #374151; padding: 0.25rem 0.75rem; border-radius: 999px; font-size: 0.875rem; }
    .controls { display: flex; justify-content: space-between; align-items: center; margin-bottom: 1.5rem; flex-wrap: wrap; gap: 0.75rem; }
    .filters { display: flex; gap: 0.5rem; flex-wrap: wrap; flex: 1; }
    .search-input { padding: 0.5rem 0.875rem; border: 1px solid #e2e8f0; border-radius: 8px; font-size: 0.875rem; min-width: 200px; }
    .search-input:focus { border-color: #3b82f6; box-shadow: 0 0 0 3px rgba(59,130,246,0.15); outline: none; }
    select { padding: 0.5rem 0.75rem; border: 1px solid #e2e8f0; border-radius: 8px; font-size: 0.875rem; background: #fff; }
    .btn-primary { background: linear-gradient(135deg, #3b82f6, #2563eb); color: #fff; border: none; padding: 0.5rem 1.25rem; border-radius: 8px; font-weight: 500; cursor: pointer; white-space: nowrap; }
    .btn-primary:hover { transform: translateY(-1px); box-shadow: 0 4px 8px rgba(37,99,235,0.3); }
    .asset-table { background: #fff; border: 1px solid #e2e8f0; border-radius: 12px; overflow: hidden; }
    table { width: 100%; border-collapse: collapse; }
    thead { background: #f8fafc; }
    th { padding: 0.75rem 1rem; text-align: left; font-size: 0.8rem; font-weight: 600; color: #64748b; text-transform: uppercase; letter-spacing: 0.05em; border-bottom: 1px solid #e2e8f0; }
    td { padding: 0.75rem 1rem; font-size: 0.875rem; color: #374151; border-bottom: 1px solid #f1f5f9; }
    .clickable-row { cursor: pointer; transition: background 0.1s; }
    .clickable-row:hover { background: #f8fafc; }
    .tag-cell { font-family: monospace; font-weight: 600; color: #6366f1; }
    .value-cell { font-weight: 500; }
    .status-badge { display: inline-block; padding: 0.2rem 0.5rem; border-radius: 999px; font-size: 0.75rem; font-weight: 600; }
    .actions-cell { display: flex; gap: 0.25rem; }
    .btn-sm { padding: 0.25rem 0.5rem; border: 1px solid #e2e8f0; border-radius: 6px; background: #fff; cursor: pointer; font-size: 0.75rem; }
    .btn-sm:hover { background: #f1f5f9; }
    .pagination { display: flex; align-items: center; justify-content: center; gap: 1rem; margin-top: 1rem; }
    .pagination button { padding: 0.5rem 1rem; border: 1px solid #e2e8f0; border-radius: 6px; background: #fff; cursor: pointer; }
    .pagination button:disabled { opacity: 0.5; cursor: not-allowed; }
    .empty-state { text-align: center; padding: 3rem; background: #fff; border: 2px dashed #e2e8f0; border-radius: 12px; color: #6b7280; }
    .loading { text-align: center; padding: 2rem; color: #6b7280; }
  `]
})
export class AssetListComponent implements OnInit {
  projectId = '';
  items: AssetListItem[] = [];
  totalCount = 0;
  page = 1;
  pageSize = 50;
  loading = false;
  search = '';
  statusFilter: AssetStatus | null = null;
  typeFilter: AssetType | null = null;
  domainConfigs: DomainAssetConfigItem[] = [];

  statusOptions = [
    { value: AssetStatus.Available, label: 'Available' },
    { value: AssetStatus.InUse, label: 'In Use' },
    { value: AssetStatus.UnderMaintenance, label: 'Under Maintenance' },
    { value: AssetStatus.Retired, label: 'Retired' },
    { value: AssetStatus.Disposed, label: 'Disposed' },
    { value: AssetStatus.Lost, label: 'Lost' },
    { value: AssetStatus.Damaged, label: 'Damaged' }
  ];

  typeOptions: { value: AssetType; label: string }[] = [];

  private searchTimeout: ReturnType<typeof setTimeout> | null = null;

  get totalPages(): number {
    return Math.ceil(this.totalCount / this.pageSize);
  }

  constructor(
    private route: ActivatedRoute,
    private router: Router,
    private assetService: AssetService,
    private projectsService: ProjectsService
  ) {}

  ngOnInit(): void {
    this.projectId = this.route.snapshot.paramMap.get('projectId') || '';
    this.loadDomainConfig();
    this.load();
  }

  load(): void {
    this.loading = true;
    this.assetService.getAssetsByProject(
      this.projectId,
      this.statusFilter ?? undefined,
      this.typeFilter ?? undefined,
      this.search || undefined,
      this.page,
      this.pageSize
    ).subscribe({
      next: result => {
        this.items = result.items;
        this.totalCount = result.totalCount;
        this.loading = false;
      },
      error: () => { this.loading = false; }
    });
  }

  onSearchChange(): void {
    if (this.searchTimeout) clearTimeout(this.searchTimeout);
    this.searchTimeout = setTimeout(() => {
      this.page = 1;
      this.load();
    }, 300);
  }

  viewDetail(assetId: string): void {
    this.router.navigate(['/projects', this.projectId, 'assets', assetId]);
  }

  editAsset(assetId: string): void {
    this.router.navigate(['/projects', this.projectId, 'assets', assetId, 'edit']);
  }

  createNew(): void {
    this.router.navigate(['/projects', this.projectId, 'assets', 'new']);
  }

  formatStatus(status: AssetStatus): string { return formatAssetStatus(status); }
  formatType(type: AssetType): string { return formatAssetType(type, this.domainConfigs); }
  getStatusColor(status: AssetStatus): string { return getStatusColor(status); }
  getStatusBg(status: AssetStatus): string {
    const color = getStatusColor(status);
    return color + '20';
  }

  private loadDomainConfig(): void {
    if (!this.projectId) {
      this.setFallbackTypeOptions();
      return;
    }
    this.projectsService.getConfig(this.projectId).subscribe({
      next: config => {
        const domainType = this.parseDomainType(config.domainType);
        if (domainType !== null) {
          this.assetService.getDomainAssetConfig(domainType).subscribe({
            next: result => {
              this.domainConfigs = result.assetTypes || [];
              this.typeOptions = this.domainConfigs.map(c => ({ value: c.assetType, label: c.displayLabel }));
            },
            error: () => this.setFallbackTypeOptions()
          });
        } else {
          this.setFallbackTypeOptions();
        }
      },
      error: () => this.setFallbackTypeOptions()
    });
  }

  private parseDomainType(value: string): DomainType | null {
    const map: Record<string, DomainType> = {
      'IT': DomainType.IT,
      'Healthcare': DomainType.Healthcare,
      'PublicSafety': DomainType.PublicSafety,
      'Construction': DomainType.Construction,
      'Infrastructure': DomainType.Infrastructure,
      'EconomicDevelopment': DomainType.EconomicDevelopment,
      'Technology': DomainType.Technology
    };
    return map[value] ?? null;
  }

  private setFallbackTypeOptions(): void {
    this.typeOptions = [
      { value: AssetType.Equipment, label: 'Equipment' },
      { value: AssetType.Vehicle, label: 'Vehicle' },
      { value: AssetType.ITHardware, label: 'IT Hardware' },
      { value: AssetType.Tool, label: 'Tool' },
      { value: AssetType.Furniture, label: 'Furniture' },
      { value: AssetType.Material, label: 'Material' },
      { value: AssetType.Other, label: 'Other' }
    ];
  }
}
