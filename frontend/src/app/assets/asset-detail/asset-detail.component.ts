import { Component, OnInit } from '@angular/core';
import { ActivatedRoute, Router } from '@angular/router';
import { AssetService } from '../asset.service';
import {
  AssetDetail,
  AssetHistoryDto,
  MaintenanceRecordDto,
  AssetCheckoutDto,
  formatAssetStatus,
  formatAssetType,
  formatChangeType,
  getStatusColor
} from '../asset.model';

@Component({
  selector: 'app-asset-detail',
  templateUrl: './asset-detail.component.html',
  styles: [`
    .detail-header { display: flex; justify-content: space-between; align-items: center; margin-bottom: 1.5rem; flex-wrap: wrap; gap: 0.5rem; }
    .btn-back { background: none; border: none; color: #3b82f6; cursor: pointer; font-size: 0.875rem; padding: 0; }
    .header-actions { display: flex; gap: 0.5rem; }
    .btn-action { padding: 0.5rem 1rem; border: 1px solid #e2e8f0; border-radius: 8px; background: #fff; cursor: pointer; font-size: 0.875rem; }
    .btn-action:hover { background: #f8fafc; }
    .btn-assign { border-color: #3b82f6; color: #3b82f6; }
    .btn-return { border-color: #22c55e; color: #22c55e; }
    .btn-maint { border-color: #eab308; color: #92400e; }
    .info-card { background: #fff; border: 1px solid #e2e8f0; border-radius: 12px; padding: 1.5rem; margin-bottom: 1.5rem; }
    .info-row { display: flex; gap: 2rem; margin-bottom: 1rem; flex-wrap: wrap; }
    .info-group { display: flex; flex-direction: column; min-width: 150px; flex: 1; }
    .info-group.full-width { min-width: 100%; }
    .info-group label { font-size: 0.75rem; font-weight: 600; color: #64748b; text-transform: uppercase; margin-bottom: 0.25rem; }
    .tag-value { font-family: monospace; font-weight: 600; color: #6366f1; }
    .status-badge { display: inline-block; padding: 0.2rem 0.5rem; border-radius: 999px; font-size: 0.75rem; font-weight: 600; width: fit-content; }
    .tabs { display: flex; gap: 0.25rem; margin-bottom: 1rem; background: #f1f5f9; border-radius: 10px; padding: 0.25rem; width: fit-content; }
    .tabs button { padding: 0.5rem 1rem; border: none; border-radius: 8px; background: transparent; color: #64748b; font-size: 0.875rem; cursor: pointer; }
    .tabs button.active { background: #fff; color: #1e293b; box-shadow: 0 1px 3px rgba(0,0,0,0.1); font-weight: 600; }
    .tab-content { background: #fff; border: 1px solid #e2e8f0; border-radius: 12px; overflow: hidden; }
    .tab-content table { width: 100%; border-collapse: collapse; }
    .tab-content th { padding: 0.75rem 1rem; text-align: left; font-size: 0.8rem; font-weight: 600; color: #64748b; background: #f8fafc; border-bottom: 1px solid #e2e8f0; }
    .tab-content td { padding: 0.75rem 1rem; font-size: 0.875rem; border-bottom: 1px solid #f1f5f9; }
    .history-list { padding: 1rem; }
    .history-item { padding: 0.75rem 0; border-bottom: 1px solid #f1f5f9; }
    .history-item:last-child { border-bottom: none; }
    .history-type { font-weight: 600; font-size: 0.875rem; color: #1e293b; }
    .history-detail { font-size: 0.875rem; color: #374151; margin-top: 0.25rem; }
    .history-reason { color: #6b7280; font-style: italic; }
    .history-meta { font-size: 0.75rem; color: #94a3b8; margin-top: 0.25rem; }
    .empty-tab { padding: 2rem; text-align: center; color: #6b7280; }
    .empty-state { text-align: center; padding: 3rem; color: #6b7280; }
    .loading { text-align: center; padding: 2rem; color: #6b7280; }
    .modal-overlay { position: fixed; top: 0; left: 0; right: 0; bottom: 0; background: rgba(0,0,0,0.4); display: flex; align-items: center; justify-content: center; z-index: 1000; }
    .modal { background: #fff; border-radius: 12px; padding: 1.5rem; min-width: 400px; max-width: 500px; }
    .modal h3 { margin: 0 0 1rem; font-size: 1.125rem; }
    .form-group { margin-bottom: 1rem; display: flex; flex-direction: column; }
    .form-group label { font-size: 0.813rem; font-weight: 500; color: #4b5563; margin-bottom: 0.375rem; }
    .form-group input, .form-group select, .form-group textarea { padding: 0.5rem 0.75rem; border: 1px solid #e2e8f0; border-radius: 8px; font-size: 0.875rem; }
    .form-group textarea { min-height: 80px; resize: vertical; }
    .modal-actions { display: flex; gap: 0.5rem; justify-content: flex-end; }
    .btn-primary { background: linear-gradient(135deg, #3b82f6, #2563eb); color: #fff; border: none; padding: 0.5rem 1.25rem; border-radius: 8px; cursor: pointer; }
    .btn-primary:disabled { opacity: 0.6; cursor: not-allowed; }
    .btn-cancel { padding: 0.5rem 1.25rem; border: 1px solid #e2e8f0; border-radius: 8px; background: #fff; cursor: pointer; }
  `]
})
export class AssetDetailComponent implements OnInit {
  projectId = '';
  assetId = '';
  asset: AssetDetail | null = null;
  loading = false;
  activeTab: 'maintenance' | 'checkouts' | 'history' = 'maintenance';

  maintenanceRecords: MaintenanceRecordDto[] = [];
  checkouts: AssetCheckoutDto[] = [];
  history: AssetHistoryDto[] = [];

  showAssignModal = false;
  assignUserId = '';
  assignReturnDate = '';
  assignNotes = '';

  showReturnModal = false;
  returnCondition = 'Good';
  returnNotes = '';

  showMaintenanceModal = false;
  maintType = 1;
  maintDate = '';
  maintDescription = '';
  maintCost = 0;

  constructor(
    private route: ActivatedRoute,
    private router: Router,
    private assetService: AssetService
  ) {}

  ngOnInit(): void {
    this.projectId = this.route.snapshot.paramMap.get('projectId') || '';
    this.assetId = this.route.snapshot.paramMap.get('assetId') || '';
    this.loadAsset();
  }

  loadAsset(): void {
    this.loading = true;
    this.assetService.getAsset(this.assetId).subscribe({
      next: asset => { this.asset = asset; this.loading = false; this.loadMaintenance(); },
      error: () => { this.loading = false; }
    });
  }

  loadMaintenance(): void {
    this.assetService.getMaintenanceHistory(this.assetId).subscribe(r => this.maintenanceRecords = r);
  }

  loadCheckouts(): void {
    if (this.checkouts.length === 0) {
      this.assetService.getCheckoutHistory(this.assetId).subscribe(r => this.checkouts = r);
    }
  }

  loadHistory(): void {
    if (this.history.length === 0) {
      this.assetService.getAssetHistory(this.assetId).subscribe(r => this.history = r);
    }
  }

  goBack(): void {
    this.router.navigate(['/projects', this.projectId, 'assets']);
  }

  edit(): void {
    this.router.navigate(['/projects', this.projectId, 'assets', this.assetId, 'edit']);
  }

  assign(): void {
    this.assetService.assignAsset(this.assetId, this.assignUserId, this.assignReturnDate || undefined, this.assignNotes || undefined).subscribe({
      next: () => { this.showAssignModal = false; this.loadAsset(); },
      error: () => {}
    });
  }

  returnAsset(): void {
    this.assetService.returnAsset(this.assetId, this.returnCondition, this.returnNotes || undefined).subscribe({
      next: () => { this.showReturnModal = false; this.loadAsset(); },
      error: () => {}
    });
  }

  scheduleMaintenance(): void {
    this.assetService.scheduleMaintenance(this.assetId, this.maintType, this.maintDate, this.maintDescription, this.maintCost).subscribe({
      next: () => { this.showMaintenanceModal = false; this.loadMaintenance(); },
      error: () => {}
    });
  }

  formatStatus = formatAssetStatus;
  formatType = formatAssetType;
  formatChangeType = formatChangeType;
  getStatusColor = getStatusColor;
}
