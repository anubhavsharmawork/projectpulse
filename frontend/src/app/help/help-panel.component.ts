import {
  Component,
  EventEmitter,
  Input,
  Output,
  OnChanges,
  SimpleChanges,
  ViewChild,
  ElementRef,
  AfterViewInit,
  OnDestroy
} from '@angular/core';
import { trigger, transition, style, animate } from '@angular/animations';
import {
  HelpContent,
  HelpArticle,
  HelpCategory,
  HELP_CONTENT_EN
} from './help-data';

/**
 * Search result entry linking a matched article back to its category.
 * Kept lightweight so the template can render results without extra lookups.
 */
interface SearchResult {
  categoryId: string;
  article: HelpArticle;
  /** Relevance weight — higher is a better match. */
  score: number;
}

/**
 * Help panel component — a slide-out panel providing searchable,
 * keyboard-navigable help content.
 *
 * Accessibility
 * ─────────────
 * • Traps focus inside the panel while open (WCAG 2.4.3).
 * • Escape key closes the panel.
 * • All interactive elements have visible focus indicators.
 * • Live regions announce search result counts to screen readers.
 *
 * Performance
 * ───────────
 * • Fuzzy search runs synchronously on a small data-set — no debounce needed.
 * • Animations respect `prefers-reduced-motion` via CSS (no JS check).
 */
@Component({
  selector: 'app-help-panel',
  templateUrl: './help-panel.component.html',
  animations: [
    trigger('slideInOut', [
      transition(':enter', [
        style({ transform: 'translateX(100%)' }),
        animate('250ms cubic-bezier(0.4,0,0.2,1)', style({ transform: 'translateX(0)' }))
      ]),
      transition(':leave', [
        animate('200ms cubic-bezier(0.4,0,0.6,1)', style({ transform: 'translateX(100%)' }))
      ])
    ]),
    trigger('fadeInOut', [
      transition(':enter', [
        style({ opacity: 0 }),
        animate('200ms ease', style({ opacity: 1 }))
      ]),
      transition(':leave', [
        animate('150ms ease', style({ opacity: 0 }))
      ])
    ])
  ]
})
export class HelpPanelComponent implements OnChanges, AfterViewInit, OnDestroy {
  @Input() isOpen = false;
  @Output() isOpenChange = new EventEmitter<boolean>();

  @ViewChild('searchInput') searchInputRef!: ElementRef<HTMLInputElement>;
  @ViewChild('closeBtn') closeBtnRef!: ElementRef<HTMLButtonElement>;

  /** Active help locale — swap this object to change language at runtime. */
  content: HelpContent = HELP_CONTENT_EN;

  searchQuery = '';
  searchResults: SearchResult[] = [];
  expandedCategories: boolean[] = [];
  activeArticle: HelpArticle | null = null;

  /** Stores which category the active article belongs to (for breadcrumb). */
  private activeArticleCategoryId: string | null = null;

  /** Reference kept so we can remove it on destroy. */
  private boundTrapFocus: ((e: KeyboardEvent) => void) | null = null;

  /* ── Lifecycle ── */

  ngOnChanges(changes: SimpleChanges): void {
    if (changes['isOpen'] && this.isOpen) {
      this.resetState();
      // Delay focus until the panel has rendered
      setTimeout(() => this.focusSearchInput(), 100);
    }
  }

  ngAfterViewInit(): void {
    /* intentionally empty — focus is set in ngOnChanges to handle open state */
  }

  ngOnDestroy(): void {
    this.removeFocusTrap();
  }

  /* ── Public API used by template ── */

  close(): void {
    this.isOpen = false;
    this.isOpenChange.emit(false);
    this.removeFocusTrap();
  }

  /**
   * Lightweight fuzzy search.
   *
   * Strategy: for each article, score it by how well the query tokens
   * match against the title, summary, body, and hidden keywords.
   * Title matches are weighted highest so the most relevant result
   * appears first.
   */
  onSearch(): void {
    const raw = this.searchQuery.trim().toLowerCase();
    if (!raw) {
      this.searchResults = [];
      return;
    }

    const tokens = raw.split(/\s+/).filter(t => t.length > 0);
    const results: SearchResult[] = [];

    for (const cat of this.content.categories) {
      for (const article of cat.articles) {
        const score = this.scoreArticle(article, tokens);
        if (score > 0) {
          results.push({ categoryId: cat.id, article, score });
        }
      }
    }

    // Sort descending by relevance score
    results.sort((a, b) => b.score - a.score);
    this.searchResults = results;
  }

  clearSearch(): void {
    this.searchQuery = '';
    this.searchResults = [];
    this.focusSearchInput();
  }

  toggleCategory(index: number): void {
    this.expandedCategories[index] = !this.expandedCategories[index];
  }

  openArticle(categoryId: string, articleId: string): void {
    const cat = this.content.categories.find(c => c.id === categoryId);
    if (!cat) return;
    const article = cat.articles.find(a => a.id === articleId);
    if (!article) return;

    this.activeArticle = article;
    this.activeArticleCategoryId = categoryId;
    this.searchQuery = '';
    this.searchResults = [];
  }

  closeArticle(): void {
    // Expand the category the user was reading so they can continue browsing
    if (this.activeArticleCategoryId) {
      const idx = this.content.categories.findIndex(c => c.id === this.activeArticleCategoryId);
      if (idx >= 0) {
        this.expandedCategories[idx] = true;
      }
    }
    this.activeArticle = null;
    this.activeArticleCategoryId = null;
  }

  /** Top-level keydown handler on the panel element. */
  onPanelKeydown(event: KeyboardEvent): void {
    if (event.key === 'Escape') {
      event.preventDefault();
      this.close();
    }
  }

  /* ── Private helpers ── */

  private resetState(): void {
    this.searchQuery = '';
    this.searchResults = [];
    this.activeArticle = null;
    this.activeArticleCategoryId = null;
    // Collapse all categories — user discovers content progressively
    this.expandedCategories = this.content.categories.map(() => false);
    this.installFocusTrap();
  }

  private focusSearchInput(): void {
    this.searchInputRef?.nativeElement?.focus();
  }

  /**
   * Score an article against search tokens.
   *
   * Weighting:
   *   title match   → 4 points per token
   *   keyword match  → 3 points per token
   *   summary match  → 2 points per token
   *   body match     → 1 point per token
   *
   * Supports fuzzy substring matching — the user does not need to
   * type an exact word.
   */
  private scoreArticle(article: HelpArticle, tokens: string[]): number {
    const title = article.title.toLowerCase();
    const summary = article.summary.toLowerCase();
    const body = article.body.toLowerCase();
    const keywords = article.keywords.join(' ').toLowerCase();

    let score = 0;
    for (const token of tokens) {
      if (title.includes(token)) score += 4;
      if (keywords.includes(token)) score += 3;
      if (summary.includes(token)) score += 2;
      if (body.includes(token)) score += 1;
    }
    return score;
  }

  /**
   * Basic focus trap — keeps Tab / Shift+Tab cycling inside the panel.
   * Necessary for modal dialog accessibility (WCAG 2.4.3).
   */
  private installFocusTrap(): void {
    this.removeFocusTrap();
    this.boundTrapFocus = (e: KeyboardEvent) => {
      if (e.key !== 'Tab') return;
      const panel = document.querySelector('.help-panel') as HTMLElement | null;
      if (!panel) return;

      const focusable = panel.querySelectorAll<HTMLElement>(
        'button, [href], input, select, textarea, [tabindex]:not([tabindex="-1"])'
      );
      if (focusable.length === 0) return;

      const first = focusable[0];
      const last = focusable[focusable.length - 1];

      if (e.shiftKey && document.activeElement === first) {
        e.preventDefault();
        last.focus();
      } else if (!e.shiftKey && document.activeElement === last) {
        e.preventDefault();
        first.focus();
      }
    };
    document.addEventListener('keydown', this.boundTrapFocus);
  }

  private removeFocusTrap(): void {
    if (this.boundTrapFocus) {
      document.removeEventListener('keydown', this.boundTrapFocus);
      this.boundTrapFocus = null;
    }
  }
}
