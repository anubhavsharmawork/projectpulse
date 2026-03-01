export enum AssetStatus {
  Available = 1,
  InUse = 2,
  UnderMaintenance = 3,
  Retired = 4,
  Disposed = 5,
  Lost = 6,
  Damaged = 7
}

export enum AssetType {
  Equipment = 1,
  Vehicle = 2,
  ITHardware = 3,
  Tool = 4,
  Furniture = 5,
  Material = 6,

  // IT / Technology
  Server = 10,
  Laptop = 11,
  Desktop = 12,
  Monitor = 13,
  NetworkSwitch = 14,
  Router = 15,
  Firewall = 16,
  MobileDevice = 17,
  Printer = 18,
  SoftwareLicense = 19,
  CloudSubscription = 20,

  // Healthcare
  MedicalDevice = 30,
  ImagingEquipment = 31,
  LabInstrument = 32,
  PatientMonitor = 33,
  Defibrillator = 34,
  InfusionPump = 35,
  SurgicalInstrument = 36,
  HospitalBed = 37,
  Wheelchair = 38,
  Ambulance = 39,
  PharmaceuticalStorage = 40,

  // Public Safety
  PatrolVehicle = 50,
  BodyCamera = 51,
  Radio = 52,
  Firearm = 53,
  ProtectiveGear = 54,
  TacticalEquipment = 55,
  ForensicKit = 56,
  EmergencyVehicle = 57,
  Drone = 58,
  SurveillanceSystem = 59,

  // Construction
  Excavator = 70,
  Crane = 71,
  Scaffolding = 72,
  Bulldozer = 73,
  ConcreteEquipment = 74,
  SurveyingInstrument = 75,
  SafetyEquipment = 76,
  PowerTool = 77,
  HeavyTruck = 78,
  Generator = 79,

  // Infrastructure / Utility
  Transformer = 90,
  PowerLine = 91,
  SmartMeter = 92,
  Substation = 93,
  Pipeline = 94,
  PumpingStation = 95,
  WaterMain = 96,
  TelecomTower = 97,
  TrafficSignal = 98,
  Bridge = 100,
  SolarPanel = 101,
  WindTurbine = 102,

  // Economic Development
  OfficeEquipment = 110,
  CommunityFacility = 111,
  GrantFundedAsset = 112,
  ConferenceSystem = 113,
  PublicVehicle = 114,

  // Technology / R&D
  DevWorkstation = 120,
  TestDevice = 121,
  ServerRack = 122,
  PrototypingEquipment = 123,
  ARVRHeadset = 124,
  RoboticSystem = 125,
  ThreeDPrinter = 126,

  Other = 999
}

export enum AssetCategory {
  Physical = 1,
  Digital = 2,
  License = 3,
  Consumable = 4,
  Infrastructure = 5,
  Vehicle = 6,
  Facility = 7,
  MedicalEquipment = 8,
  SafetyGear = 9,
  Instrument = 10
}

export enum DomainType {
  IT = 1,
  Healthcare = 2,
  PublicSafety = 3,
  Construction = 4,
  Infrastructure = 5,
  EconomicDevelopment = 6,
  Technology = 7
}

export interface DomainAssetConfigItem {
  id: string;
  assetType: AssetType;
  category: AssetCategory;
  displayLabel: string;
  description?: string;
  defaultDepreciationMethod: DepreciationMethod;
  defaultUsefulLifeYears: number;
  defaultMaintenanceIntervalDays?: number;
  complianceNotes?: string;
  sortOrder: number;
}

export interface DomainAssetConfigResult {
  domainType: DomainType;
  assetTypes: DomainAssetConfigItem[];
}

export enum DepreciationMethod {
  StraightLine = 1,
  DecliningBalance = 2,
  NoDepreciation = 3
}

export enum MaintenanceType {
  Preventive = 1,
  Corrective = 2,
  Inspection = 3
}

export enum AssetChangeType {
  Created = 1,
  StatusChanged = 2,
  AssignmentChanged = 3,
  MaintenancePerformed = 4,
  ValueAdjusted = 5,
  LocationMoved = 6,
  Disposed = 7
}

export interface AssetDetail {
  id: string;
  projectId: string;
  assetTag: string;
  name: string;
  description?: string;
  purchaseDate: string;
  purchasePrice: number;
  currentValue: number;
  status: AssetStatus;
  location: string;
  assignedToUserId?: string;
  assignedToUserName?: string;
  serialNumber?: string;
  manufacturer?: string;
  model?: string;
  warrantyExpiryDate?: string;
  notes?: string;
  depreciationMethod: DepreciationMethod;
  usefulLifeYears: number;
  type: AssetType;
  createdAt: string;
  updatedAt: string;
  createdBy: string;
  isActive: boolean;
  weight?: number;
  dimensions?: string;
  barcodeValue?: string;
  maintenanceIntervalDays?: number;
  lastMaintenanceDate?: string;
  nextMaintenanceDate?: string;
}

export interface AssetListItem {
  id: string;
  assetTag: string;
  name: string;
  type: AssetType;
  status: AssetStatus;
  location: string;
  assignedToUserId?: string;
  assignedToUserName?: string;
  purchaseDate: string;
  currentValue: number;
  manufacturer?: string;
  model?: string;
}

export interface AssetsByProjectResult {
  items: AssetListItem[];
  totalCount: number;
  page: number;
  pageSize: number;
}

export interface MaintenanceRecordDto {
  id: string;
  assetId: string;
  scheduledDate: string;
  completedDate?: string;
  maintenanceType: MaintenanceType;
  description: string;
  performedBy?: string;
  cost: number;
  notes?: string;
  nextMaintenanceDate?: string;
}

export interface AssetCheckoutDto {
  id: string;
  assetId: string;
  checkedOutToUserId: string;
  checkedOutToUserName?: string;
  checkedOutAt: string;
  expectedReturnDate?: string;
  actualReturnDate?: string;
  checkedOutBy: string;
  checkedInBy?: string;
  condition: string;
  notes?: string;
}

export interface AssetHistoryDto {
  id: string;
  assetId: string;
  changeType: AssetChangeType;
  oldValue?: string;
  newValue?: string;
  changedBy: string;
  changedAt: string;
  reason?: string;
}

export interface CreateAssetRequest {
  assetTag: string;
  name: string;
  description?: string;
  purchaseDate: string;
  purchasePrice: number;
  currentValue: number;
  status: AssetStatus;
  location: string;
  assignedToUserId?: string;
  serialNumber?: string;
  manufacturer?: string;
  model?: string;
  warrantyExpiryDate?: string;
  notes?: string;
  depreciationMethod: DepreciationMethod;
  usefulLifeYears: number;
  assetType: AssetType;
  category: AssetCategory;
  weight?: number;
  dimensions?: string;
  barcodeValue?: string;
  maintenanceIntervalDays?: number;
  licenseKey?: string;
  licensedSeats?: number;
  licenseExpiryDate?: string;
  vendor?: string;
  gridReference?: string;
  capacity?: string;
  regulatoryId?: string;
  domainAssetConfigId?: string;
}

export interface UpdateAssetRequest {
  name: string;
  description?: string;
  status: AssetStatus;
  location: string;
  assignedToUserId?: string;
  serialNumber?: string;
  manufacturer?: string;
  model?: string;
  warrantyExpiryDate?: string;
  notes?: string;
  currentValue: number;
  depreciationMethod: DepreciationMethod;
  usefulLifeYears: number;
  weight?: number;
  dimensions?: string;
  barcodeValue?: string;
  maintenanceIntervalDays?: number;
}

export function formatAssetStatus(status: AssetStatus): string {
  const labels: Record<number, string> = {
    [AssetStatus.Available]: 'Available',
    [AssetStatus.InUse]: 'In Use',
    [AssetStatus.UnderMaintenance]: 'Under Maintenance',
    [AssetStatus.Retired]: 'Retired',
    [AssetStatus.Disposed]: 'Disposed',
    [AssetStatus.Lost]: 'Lost',
    [AssetStatus.Damaged]: 'Damaged'
  };
  return labels[status] || 'Unknown';
}

export function getStatusColor(status: AssetStatus): string {
  const colors: Record<number, string> = {
    [AssetStatus.Available]: '#22c55e',
    [AssetStatus.InUse]: '#3b82f6',
    [AssetStatus.UnderMaintenance]: '#eab308',
    [AssetStatus.Retired]: '#6b7280',
    [AssetStatus.Disposed]: '#ef4444',
    [AssetStatus.Lost]: '#f97316',
    [AssetStatus.Damaged]: '#c2410c'
  };
  return colors[status] || '#6b7280';
}

export function formatAssetType(type: AssetType, domainConfigs?: DomainAssetConfigItem[]): string {
  if (domainConfigs?.length) {
    const match = domainConfigs.find(c => c.assetType === type);
    if (match) return match.displayLabel;
  }
  const labels: Record<number, string> = {
    [AssetType.Equipment]: 'Equipment',
    [AssetType.Vehicle]: 'Vehicle',
    [AssetType.ITHardware]: 'IT Hardware',
    [AssetType.Tool]: 'Tool',
    [AssetType.Furniture]: 'Furniture',
    [AssetType.Material]: 'Material',
    [AssetType.Other]: 'Other'
  };
  return labels[type] || 'Unknown';
}

export function formatChangeType(type: AssetChangeType): string {
  const labels: Record<number, string> = {
    [AssetChangeType.Created]: 'Created',
    [AssetChangeType.StatusChanged]: 'Status Changed',
    [AssetChangeType.AssignmentChanged]: 'Assignment Changed',
    [AssetChangeType.MaintenancePerformed]: 'Maintenance Performed',
    [AssetChangeType.ValueAdjusted]: 'Value Adjusted',
    [AssetChangeType.LocationMoved]: 'Location Moved',
    [AssetChangeType.Disposed]: 'Disposed'
  };
  return labels[type] || 'Unknown';
}
