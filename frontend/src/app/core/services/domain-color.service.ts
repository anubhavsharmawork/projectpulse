import { Injectable } from '@angular/core';

export interface DomainColorScheme {
  /** Primary color for card borders, main accents */
  primary: string;
  /** Light background for badges and button backgrounds */
  badgeBg: string;
  /** Text color for badges and buttons */
  badgeText: string;
  /** Secondary button background (for tasks/level3 buttons) */
  secondaryBg: string;
  /** Secondary button text */
  secondaryText: string;
  /** Secondary button border */
  secondaryBorder: string;
  /** Tertiary button background (for board buttons) */
  tertiaryBg: string;
  /** Tertiary button text */
  tertiaryText: string;
  /** Tertiary button border */
  tertiaryBorder: string;
  /** Border color for the badge outline */
  badgeBorder: string;
  /** Level-1 work item border-left (e.g., Epics) */
  level1Border: string;
  /** Level-2 work item border-left (e.g., Stories) */
  level2Border: string;
  /** Level-3 work item border-left (e.g., Tasks) */
  level3Border: string;
}

const COLOR_SCHEMES: { [domain: string]: DomainColorScheme } = {
  IT: {
    primary: '#7c3aed',
    badgeBg: '#f3e8ff',
    badgeText: '#6d28d9',
    badgeBorder: '#ddd6fe',
    secondaryBg: '#ede9fe',
    secondaryText: '#6d28d9',
    secondaryBorder: '#ddd6fe',
    tertiaryBg: '#f5f3ff',
    tertiaryText: '#7c3aed',
    tertiaryBorder: '#e9d5ff',
    level1Border: '#7c3aed',
    level2Border: '#8b5cf6',
    level3Border: '#a78bfa',
  },
  Healthcare: {
    primary: '#2563eb',
    badgeBg: '#dbeafe',
    badgeText: '#1e40af',
    badgeBorder: '#bfdbfe',
    secondaryBg: '#eff6ff',
    secondaryText: '#1d4ed8',
    secondaryBorder: '#bfdbfe',
    tertiaryBg: '#f0f9ff',
    tertiaryText: '#2563eb',
    tertiaryBorder: '#bae6fd',
    level1Border: '#2563eb',
    level2Border: '#3b82f6',
    level3Border: '#60a5fa',
  },
  PublicSafety: {
    primary: '#dc2626',
    badgeBg: '#fef2f2',
    badgeText: '#991b1b',
    badgeBorder: '#fecaca',
    secondaryBg: '#fff1f2',
    secondaryText: '#be123c',
    secondaryBorder: '#fecdd3',
    tertiaryBg: '#fef2f2',
    tertiaryText: '#dc2626',
    tertiaryBorder: '#fecaca',
    level1Border: '#dc2626',
    level2Border: '#ef4444',
    level3Border: '#f87171',
  },
  Construction: {
    primary: '#ea580c',
    badgeBg: '#fff7ed',
    badgeText: '#9a3412',
    badgeBorder: '#fed7aa',
    secondaryBg: '#fff7ed',
    secondaryText: '#c2410c',
    secondaryBorder: '#fed7aa',
    tertiaryBg: '#fffbeb',
    tertiaryText: '#ea580c',
    tertiaryBorder: '#fde68a',
    level1Border: '#ea580c',
    level2Border: '#f97316',
    level3Border: '#fb923c',
  },
  Infrastructure: {
    primary: '#16a34a',
    badgeBg: '#f0fdf4',
    badgeText: '#166534',
    badgeBorder: '#bbf7d0',
    secondaryBg: '#f0fdf4',
    secondaryText: '#15803d',
    secondaryBorder: '#bbf7d0',
    tertiaryBg: '#ecfdf5',
    tertiaryText: '#16a34a',
    tertiaryBorder: '#a7f3d0',
    level1Border: '#16a34a',
    level2Border: '#22c55e',
    level3Border: '#4ade80',
  },
  EconomicDevelopment: {
    primary: '#0d9488',
    badgeBg: '#f0fdfa',
    badgeText: '#115e59',
    badgeBorder: '#99f6e4',
    secondaryBg: '#f0fdfa',
    secondaryText: '#0f766e',
    secondaryBorder: '#99f6e4',
    tertiaryBg: '#f0fdfa',
    tertiaryText: '#0d9488',
    tertiaryBorder: '#5eead4',
    level1Border: '#0d9488',
    level2Border: '#14b8a6',
    level3Border: '#2dd4bf',
  },
  Technology: {
    primary: '#4f46e5',
    badgeBg: '#eef2ff',
    badgeText: '#3730a3',
    badgeBorder: '#c7d2fe',
    secondaryBg: '#eef2ff',
    secondaryText: '#4338ca',
    secondaryBorder: '#c7d2fe',
    tertiaryBg: '#f5f3ff',
    tertiaryText: '#4f46e5',
    tertiaryBorder: '#ddd6fe',
    level1Border: '#4f46e5',
    level2Border: '#6366f1',
    level3Border: '#818cf8',
  },
};

const DEFAULT_SCHEME: DomainColorScheme = {
  primary: '#64748b',
  badgeBg: '#f1f5f9',
  badgeText: '#475569',
  badgeBorder: '#e2e8f0',
  secondaryBg: '#f1f5f9',
  secondaryText: '#475569',
  secondaryBorder: '#e2e8f0',
  tertiaryBg: '#f8fafc',
  tertiaryText: '#64748b',
  tertiaryBorder: '#e2e8f0',
  level1Border: '#64748b',
  level2Border: '#94a3b8',
  level3Border: '#cbd5e1',
};

@Injectable({ providedIn: 'root' })
export class DomainColorService {

  getColors(domainType?: string | null): DomainColorScheme {
    if (!domainType) return DEFAULT_SCHEME;
    return COLOR_SCHEMES[domainType] || DEFAULT_SCHEME;
  }

  getBorderColor(domainType?: string | null): string {
    return this.getColors(domainType).primary;
  }

  getBadgeBg(domainType?: string | null): string {
    return this.getColors(domainType).badgeBg;
  }

  getBadgeText(domainType?: string | null): string {
    return this.getColors(domainType).badgeText;
  }

  getBadgeBorder(domainType?: string | null): string {
    return this.getColors(domainType).badgeBorder;
  }

  getLevel1Border(domainType?: string | null): string {
    return this.getColors(domainType).level1Border;
  }

  getLevel2Border(domainType?: string | null): string {
    return this.getColors(domainType).level2Border;
  }

  getLevel3Border(domainType?: string | null): string {
    return this.getColors(domainType).level3Border;
  }
}
