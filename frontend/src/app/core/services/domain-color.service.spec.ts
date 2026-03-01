import { DomainColorService } from './domain-color.service';

describe('DomainColorService', () => {
  let service: DomainColorService;

  beforeEach(() => {
    service = new DomainColorService();
  });

  it('should return default scheme when no domain type', () => {
    const colors = service.getColors(null);
    expect(colors.primary).toBe('#64748b');
  });

  it('should return default scheme for undefined domain', () => {
    const colors = service.getColors(undefined);
    expect(colors.primary).toBe('#64748b');
  });

  it('should return default scheme for unknown domain', () => {
    const colors = service.getColors('UnknownDomain');
    expect(colors.primary).toBe('#64748b');
  });

  it('should return IT color scheme', () => {
    const colors = service.getColors('IT');
    expect(colors.primary).toBe('#7c3aed');
    expect(colors.badgeBg).toBe('#f3e8ff');
  });

  it('should return Healthcare color scheme', () => {
    const colors = service.getColors('Healthcare');
    expect(colors.primary).toBe('#2563eb');
  });

  it('should return PublicSafety color scheme', () => {
    expect(service.getColors('PublicSafety').primary).toBe('#dc2626');
  });

  it('should return Construction color scheme', () => {
    expect(service.getColors('Construction').primary).toBe('#ea580c');
  });

  it('should return Infrastructure color scheme', () => {
    expect(service.getColors('Infrastructure').primary).toBe('#16a34a');
  });

  it('should return EconomicDevelopment color scheme', () => {
    expect(service.getColors('EconomicDevelopment').primary).toBe('#0d9488');
  });

  it('should return Technology color scheme', () => {
    expect(service.getColors('Technology').primary).toBe('#4f46e5');
  });

  describe('convenience methods', () => {
    it('getBorderColor returns primary', () => {
      expect(service.getBorderColor('IT')).toBe('#7c3aed');
    });

    it('getBadgeBg returns badgeBg', () => {
      expect(service.getBadgeBg('IT')).toBe('#f3e8ff');
    });

    it('getBadgeText returns badgeText', () => {
      expect(service.getBadgeText('IT')).toBe('#6d28d9');
    });

    it('getBadgeBorder returns badgeBorder', () => {
      expect(service.getBadgeBorder('IT')).toBe('#ddd6fe');
    });

    it('getLevel1Border returns level1Border', () => {
      expect(service.getLevel1Border('IT')).toBe('#7c3aed');
    });

    it('getLevel2Border returns level2Border', () => {
      expect(service.getLevel2Border('IT')).toBe('#8b5cf6');
    });

    it('getLevel3Border returns level3Border', () => {
      expect(service.getLevel3Border('IT')).toBe('#a78bfa');
    });

    it('convenience methods use default for null', () => {
      expect(service.getBorderColor(null)).toBe('#64748b');
      expect(service.getBadgeBg(null)).toBe('#f1f5f9');
      expect(service.getBadgeText(null)).toBe('#475569');
      expect(service.getBadgeBorder(null)).toBe('#e2e8f0');
      expect(service.getLevel1Border(null)).toBe('#64748b');
      expect(service.getLevel2Border(null)).toBe('#94a3b8');
      expect(service.getLevel3Border(null)).toBe('#cbd5e1');
    });
  });
});
