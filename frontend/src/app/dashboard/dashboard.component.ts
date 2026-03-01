import { Component, OnInit } from '@angular/core';
import {
  DashboardService,
  DashboardResult,
  ProjectBudgetDto,
  CommonKpis,
  ItKpis,
  HealthcareKpis,
  ConstructionKpis,
  InfrastructureKpis
} from '../core/services/dashboard.service';
import { NotificationsService } from '../notifications/notifications.service';

@Component({
  selector: 'app-dashboard',
  templateUrl: './dashboard.component.html',
  styles: [`
    :host { display: block; }
    .dashboard-header {
      display: flex; justify-content: space-between; align-items: center;
      flex-wrap: wrap; gap: 1rem; margin-bottom: 1.5rem;
    }
    .dashboard-header h2 { margin: 0; font-size: 1.75rem; font-weight: 700; color: #1e293b; }
    .domain-select {
      padding: 0.5rem 0.875rem; border: 1px solid #e2e8f0; border-radius: 8px;
      font-size: 0.875rem; background: #fff; color: #374151; cursor: pointer;
    }

    /* ── KPI cards grid ── */
    .kpi-grid {
      display: grid;
      grid-template-columns: repeat(auto-fill, minmax(200px, 1fr));
      gap: 1rem; margin-bottom: 2rem;
    }
    .kpi-card {
      background: #fff; border: 1px solid #e2e8f0; border-radius: 12px;
      padding: 1.25rem; box-shadow: 0 1px 3px rgba(0,0,0,0.04);
      transition: transform 0.15s, box-shadow 0.15s;
    }
    .kpi-card:hover { transform: translateY(-2px); box-shadow: 0 4px 12px rgba(0,0,0,0.08); }
    .kpi-label { font-size: 0.75rem; font-weight: 600; color: #64748b; text-transform: uppercase; letter-spacing: 0.05em; margin-bottom: 0.5rem; }
    .kpi-value { font-size: 1.75rem; font-weight: 700; color: #1e293b; line-height: 1; }
    .kpi-sub { font-size: 0.8125rem; color: #94a3b8; margin-top: 0.375rem; }

    /* ── Color accents ── */
    .kpi-card.green { border-left: 3px solid #22c55e; }
    .kpi-card.blue { border-left: 3px solid #3b82f6; }
    .kpi-card.amber { border-left: 3px solid #f59e0b; }
    .kpi-card.red { border-left: 3px solid #ef4444; }
    .kpi-card.purple { border-left: 3px solid #8b5cf6; }
    .kpi-card.cyan { border-left: 3px solid #06b6d4; }

    /* ── Section headings ── */
    .section-title {
      font-size: 1.125rem; font-weight: 600; color: #374151;
      margin: 2rem 0 1rem; padding-bottom: 0.5rem; border-bottom: 2px solid #e2e8f0;
      display: flex; align-items: center; gap: 0.5rem;
    }
    .section-title .dot { width: 8px; height: 8px; border-radius: 50%; }
    .dot-it { background: #3b82f6; }
    .dot-health { background: #22c55e; }
    .dot-construction { background: #f59e0b; }
    .dot-infra { background: #8b5cf6; }

    /* ── Velocity sparkline ── */
    .velocity-bars {
      display: flex; align-items: flex-end; gap: 6px; height: 48px; margin-top: 0.5rem;
    }
    .velocity-bar {
      flex: 1; background: linear-gradient(to top, #3b82f6, #60a5fa);
      border-radius: 4px 4px 0 0; min-width: 16px;
      transition: height 0.4s ease;
    }
    .velocity-label { font-size: 0.6875rem; color: #94a3b8; text-align: center; margin-top: 4px; }

    /* ── Budget table ── */
    .budget-section { margin-top: 2.5rem; }
    .budget-table {
      width: 100%; border-collapse: collapse; background: #fff; border-radius: 12px;
      overflow: hidden; box-shadow: 0 1px 3px rgba(0,0,0,0.04); border: 1px solid #e2e8f0;
    }
    .budget-table th {
      background: #f8fafc; padding: 0.75rem 1rem; text-align: left;
      font-size: 0.75rem; font-weight: 600; color: #64748b;
      text-transform: uppercase; letter-spacing: 0.05em; border-bottom: 1px solid #e2e8f0;
    }
    .budget-table td {
      padding: 0.875rem 1rem; font-size: 0.875rem; color: #374151;
      border-bottom: 1px solid #f1f5f9;
    }
    .budget-table tr:last-child td { border-bottom: none; }
    .budget-table tr:hover td { background: #f8fafc; }
    .variance-positive { color: #ef4444; font-weight: 600; }
    .variance-negative { color: #22c55e; font-weight: 600; }
    .variance-zero { color: #64748b; }
    .domain-badge {
      display: inline-block; padding: 2px 8px; border-radius: 4px;
      font-size: 0.6875rem; font-weight: 600; background: #f1f5f9; color: #475569;
    }

    /* ── Map legend items ── */
    .map-items {
      display: flex; flex-wrap: wrap; gap: 0.75rem; margin-top: 0.5rem;
    }
    .map-item {
      background: #f8fafc; border: 1px solid #e2e8f0; border-radius: 8px;
      padding: 0.5rem 0.875rem; font-size: 0.8125rem;
      display: flex; justify-content: space-between; gap: 1rem;
      min-width: 140px;
    }
    .map-item-label { color: #64748b; }
    .map-item-value { font-weight: 600; color: #1e293b; }

    .loading { padding: 3rem; text-align: center; color: #94a3b8; }
    .empty { padding: 2rem; text-align: center; color: #94a3b8; font-style: italic; }
  `]
})
export class DashboardComponent implements OnInit {
  result: DashboardResult | null = null;
  budget: ProjectBudgetDto[] = [];
  selectedDomain = '';
  loading = true;
  budgetLoading = false;

  domains = [
    { value: '', label: 'All Domains' },
    { value: 'IT', label: 'IT / Agile' },
    { value: 'Healthcare', label: 'Healthcare' },
    { value: 'Construction', label: 'Construction' },
    { value: 'Infrastructure', label: 'Infrastructure' },
    { value: 'Technology', label: 'Technology' },
    { value: 'PublicSafety', label: 'Public Safety' },
    { value: 'EconomicDevelopment', label: 'Economic Dev' }
  ];

  constructor(private dashSvc: DashboardService, private notify: NotificationsService) {}

  ngOnInit() {
    this.loadMetrics();
    this.loadBudget();
  }

  onDomainChange() {
    this.result = null;
    this.loadMetrics();
  }

  loadMetrics() {
    this.loading = true;
    this.dashSvc.getMetrics(this.selectedDomain || undefined).subscribe({
      next: r => { this.result = r; this.loading = false; },
      error: () => { this.notify.error('Failed to load dashboard metrics'); this.loading = false; }
    });
  }

  loadBudget() {
    this.budgetLoading = true;
    this.dashSvc.getBudgetStatus().subscribe({
      next: b => { this.budget = b; this.budgetLoading = false; },
      error: () => { this.budgetLoading = false; }
    });
  }

  get common(): CommonKpis | null { return this.result?.common ?? null; }
  get itKpis(): ItKpis | null { return this.result?.it ?? null; }
  get healthcareKpis(): HealthcareKpis | null { return this.result?.healthcare ?? null; }
  get constructionKpis(): ConstructionKpis | null { return this.result?.construction ?? null; }
  get infraKpis(): InfrastructureKpis | null { return this.result?.infrastructure ?? null; }

  velocityMax(): number {
    if (!this.itKpis) return 1;
    return Math.max(...this.itKpis.velocityTrend, 1);
  }

  varianceClass(pct: number): string {
    if (pct > 0) return 'variance-positive';
    if (pct < 0) return 'variance-negative';
    return 'variance-zero';
  }

  formatCurrency(n: number): string {
    return '$' + Math.round(n).toLocaleString('en-GB');
  }

  objectEntries(obj: { [key: string]: number } | undefined): [string, number][] {
    if (!obj) return [];
    return Object.entries(obj);
  }
}
