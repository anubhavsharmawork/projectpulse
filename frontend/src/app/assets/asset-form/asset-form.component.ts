import { Component, OnInit } from '@angular/core';
import { ActivatedRoute, Router } from '@angular/router';
import { AssetService } from '../asset.service';
import { ProjectsService } from '../../projects/projects.service';
import {
  AssetStatus,
  AssetType,
  AssetCategory,
  DepreciationMethod,
  DomainType,
  DomainAssetConfigItem,
  CreateAssetRequest,
  UpdateAssetRequest
} from '../asset.model';

@Component({
  selector: 'app-asset-form',
  templateUrl: './asset-form.component.html',
  styles: [`
    .form-container { max-width: 800px; }
    .form-header { display: flex; align-items: center; gap: 1rem; margin-bottom: 1.5rem; }
    .form-header h2 { margin: 0; font-size: 1.5rem; font-weight: 600; color: #1e293b; }
    .btn-back { background: none; border: none; color: #3b82f6; cursor: pointer; font-size: 0.875rem; padding: 0; }
    .asset-form { background: #fff; border: 1px solid #e2e8f0; border-radius: 12px; padding: 1.5rem; }
    .form-row { display: flex; gap: 1rem; margin-bottom: 1rem; flex-wrap: wrap; }
    .form-group { display: flex; flex-direction: column; flex: 1; min-width: 180px; margin-bottom: 0.5rem; }
    .form-group.flex-2 { flex: 2; }
    .form-group label { font-size: 0.813rem; font-weight: 500; color: #4b5563; margin-bottom: 0.375rem; }
    .form-group input, .form-group select, .form-group textarea { padding: 0.5rem 0.875rem; border: 1px solid #e2e8f0; border-radius: 8px; font-size: 0.875rem; background: #fff; }
    .form-group input:focus, .form-group select:focus, .form-group textarea:focus { border-color: #3b82f6; box-shadow: 0 0 0 3px rgba(59,130,246,0.15); outline: none; }
    .form-group textarea { resize: vertical; }
    .form-actions { display: flex; gap: 0.75rem; margin-top: 1.5rem; }
    .btn-primary { background: linear-gradient(135deg, #3b82f6, #2563eb); color: #fff; border: none; padding: 0.625rem 1.5rem; border-radius: 8px; font-weight: 500; cursor: pointer; }
    .btn-primary:disabled { opacity: 0.6; cursor: not-allowed; }
    .btn-cancel { padding: 0.625rem 1.5rem; border: 1px solid #e2e8f0; border-radius: 8px; background: #fff; cursor: pointer; }
    .error-msg { color: #dc2626; margin-top: 1rem; font-size: 0.875rem; font-weight: 500; }
    .loading { text-align: center; padding: 2rem; color: #6b7280; }
    .info-banner { background: #eff6ff; border: 1px solid #bfdbfe; border-radius: 8px; padding: 0.625rem 1rem; margin-bottom: 1rem; font-size: 0.813rem; color: #1e40af; display: flex; align-items: flex-start; gap: 0.5rem; }
    .compliance-banner { background: #fefce8; border: 1px solid #fde68a; border-radius: 8px; padding: 0.625rem 1rem; margin-bottom: 1rem; font-size: 0.813rem; color: #92400e; display: flex; align-items: flex-start; gap: 0.5rem; }
    .info-icon { flex-shrink: 0; }
    .section-block { border: 1px solid #e2e8f0; border-radius: 8px; padding: 1rem; margin-bottom: 1rem; background: #f8fafc; }
    .section-title { margin: 0 0 0.75rem; font-size: 0.875rem; font-weight: 600; color: #334155; }
  `]
})
export class AssetFormComponent implements OnInit {
  projectId = '';
  assetId = '';
  isEdit = false;
  busy = false;
  loadingConfig = false;
  error = '';

  domainConfigs: DomainAssetConfigItem[] = [];
  selectedConfig: DomainAssetConfigItem | null = null;

  form: any = {
    assetTag: '',
    name: '',
    description: '',
    purchaseDate: '',
    purchasePrice: 0,
    currentValue: 0,
    status: AssetStatus.Available,
    location: '',
    serialNumber: '',
    manufacturer: '',
    model: '',
    warrantyExpiryDate: '',
    notes: '',
    depreciationMethod: DepreciationMethod.StraightLine,
    usefulLifeYears: 5,
    assetType: null as AssetType | null,
    category: AssetCategory.Physical,
    weight: null,
    dimensions: '',
    barcodeValue: '',
    maintenanceIntervalDays: null,
    licenseKey: '',
    licensedSeats: null,
    licenseExpiryDate: '',
    vendor: '',
    gridReference: '',
    capacity: '',
    regulatoryId: '',
    domainAssetConfigId: null as string | null
  };

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

  get showLicenseFields(): boolean {
    return this.form.category === AssetCategory.License || this.form.category === AssetCategory.Digital;
  }

  get showInfraFields(): boolean {
    return this.form.category === AssetCategory.Infrastructure || this.form.category === AssetCategory.Facility;
  }

  constructor(
    private route: ActivatedRoute,
    private router: Router,
    private assetService: AssetService,
    private projectsService: ProjectsService
  ) {}

  ngOnInit(): void {
    this.projectId = this.route.snapshot.paramMap.get('projectId') || '';
    this.assetId = this.route.snapshot.paramMap.get('assetId') || '';
    this.isEdit = !!this.assetId;

    this.loadDomainConfig();

    if (this.isEdit) {
      this.assetService.getAsset(this.assetId).subscribe(asset => {
        this.form = {
          ...this.form,
          name: asset.name,
          description: asset.description || '',
          currentValue: asset.currentValue,
          status: asset.status,
          location: asset.location,
          serialNumber: asset.serialNumber || '',
          manufacturer: asset.manufacturer || '',
          model: asset.model || '',
          warrantyExpiryDate: asset.warrantyExpiryDate ? asset.warrantyExpiryDate.substring(0, 10) : '',
          notes: asset.notes || '',
          depreciationMethod: asset.depreciationMethod,
          usefulLifeYears: asset.usefulLifeYears,
          weight: asset.weight,
          dimensions: asset.dimensions || '',
          barcodeValue: asset.barcodeValue || '',
          maintenanceIntervalDays: asset.maintenanceIntervalDays
        };
      });
    }
  }

  onTypeSelected(assetType: AssetType): void {
    const config = this.domainConfigs.find(c => c.assetType === assetType);
    this.selectedConfig = config || null;
    if (config) {
      this.form.category = config.category;
      this.form.depreciationMethod = config.defaultDepreciationMethod;
      this.form.usefulLifeYears = config.defaultUsefulLifeYears;
      this.form.maintenanceIntervalDays = config.defaultMaintenanceIntervalDays ?? null;
      this.form.domainAssetConfigId = config.id;
    } else {
      this.form.category = AssetCategory.Physical;
      this.form.domainAssetConfigId = null;
    }
  }

  submit(): void {
    this.busy = true;
    this.error = '';

    if (this.isEdit) {
      const data: UpdateAssetRequest = {
        name: this.form.name,
        description: this.form.description || undefined,
        status: this.form.status,
        location: this.form.location,
        serialNumber: this.form.serialNumber || undefined,
        manufacturer: this.form.manufacturer || undefined,
        model: this.form.model || undefined,
        warrantyExpiryDate: this.form.warrantyExpiryDate || undefined,
        notes: this.form.notes || undefined,
        currentValue: this.form.currentValue,
        depreciationMethod: this.form.depreciationMethod,
        usefulLifeYears: this.form.usefulLifeYears,
        weight: this.form.weight,
        dimensions: this.form.dimensions || undefined,
        barcodeValue: this.form.barcodeValue || undefined,
        maintenanceIntervalDays: this.form.maintenanceIntervalDays
      };
      this.assetService.updateAsset(this.assetId, data).subscribe({
        next: () => { this.busy = false; this.goBack(); },
        error: () => { this.error = 'Failed to update asset.'; this.busy = false; }
      });
    } else {
      const data: CreateAssetRequest = {
        assetTag: this.form.assetTag,
        name: this.form.name,
        description: this.form.description || undefined,
        purchaseDate: this.form.purchaseDate,
        purchasePrice: this.form.purchasePrice,
        currentValue: this.form.currentValue || this.form.purchasePrice,
        status: this.form.status,
        location: this.form.location,
        serialNumber: this.form.serialNumber || undefined,
        manufacturer: this.form.manufacturer || undefined,
        model: this.form.model || undefined,
        warrantyExpiryDate: this.form.warrantyExpiryDate || undefined,
        notes: this.form.notes || undefined,
        depreciationMethod: this.form.depreciationMethod,
        usefulLifeYears: this.form.usefulLifeYears,
        assetType: this.form.assetType,
        category: this.form.category,
        weight: this.form.weight,
        dimensions: this.form.dimensions || undefined,
        barcodeValue: this.form.barcodeValue || undefined,
        maintenanceIntervalDays: this.form.maintenanceIntervalDays,
        licenseKey: this.form.licenseKey || undefined,
        licensedSeats: this.form.licensedSeats || undefined,
        licenseExpiryDate: this.form.licenseExpiryDate || undefined,
        vendor: this.form.vendor || undefined,
        gridReference: this.form.gridReference || undefined,
        capacity: this.form.capacity || undefined,
        regulatoryId: this.form.regulatoryId || undefined,
        domainAssetConfigId: this.form.domainAssetConfigId || undefined
      };
      this.assetService.createAsset(this.projectId, data).subscribe({
        next: () => { this.busy = false; this.goBack(); },
        error: () => { this.error = 'Failed to create asset.'; this.busy = false; }
      });
    }
  }

  goBack(): void {
    if (this.isEdit) {
      this.router.navigate(['/projects', this.projectId, 'assets', this.assetId]);
    } else {
      this.router.navigate(['/projects', this.projectId, 'assets']);
    }
  }

  private loadDomainConfig(): void {
    if (!this.projectId) {
      this.setFallbackTypeOptions();
      return;
    }
    this.loadingConfig = true;
    this.projectsService.getConfig(this.projectId).subscribe({
      next: config => {
        const domainType = this.parseDomainType(config.domainType);
        if (domainType !== null) {
          this.assetService.getDomainAssetConfig(domainType).subscribe({
            next: result => {
              this.domainConfigs = result.assetTypes || [];
              this.typeOptions = this.domainConfigs.map(c => ({ value: c.assetType, label: c.displayLabel }));
              if (this.typeOptions.length > 0 && !this.isEdit && this.form.assetType === null) {
                this.form.assetType = this.typeOptions[0].value;
                this.onTypeSelected(this.form.assetType);
              }
              this.loadingConfig = false;
            },
            error: () => { this.setFallbackTypeOptions(); this.loadingConfig = false; }
          });
        } else {
          this.setFallbackTypeOptions();
          this.loadingConfig = false;
        }
      },
      error: () => { this.setFallbackTypeOptions(); this.loadingConfig = false; }
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
    if (!this.isEdit && this.form.assetType === null) {
      this.form.assetType = AssetType.Equipment;
      this.form.category = AssetCategory.Physical;
    }
  }
}
