import { Component, Inject, HostListener, OnInit, AfterViewInit, ElementRef, OnDestroy, Injector, Type } from '@angular/core';
import { Router, NavigationEnd } from '@angular/router';
import { HttpClient, HttpHeaders, HttpParams } from '@angular/common/http';
import { API_BASE_URL } from './core/api.config';
import { DemoAuthService } from './core/demo-auth.service';
import { Subscription, filter } from 'rxjs';

@Component({
  selector: 'app-root',
  templateUrl: './app.component.html',
  styles: [
    `.spacer{flex:1 1 auto}`
  ]
})
export class AppComponent implements OnInit, AfterViewInit, OnDestroy {
  token$ = this.auth.tokenChanges$;
  year = new Date().getFullYear();
  scrollProgress = 0;
  isAuthPage = false;

  overall = { completed: 0, total: 0, percent: 0 };
  perProject: Array<{ name: string; completed: number; total: number; percent: number }> = [];
  loadingStats = false;
  statsError = '';
  private hubConnected = false;
  private tokenSubscription?: Subscription;
  private routerSubscription?: Subscription;
  private signalrService: any = null;
  private notifyService: any = null;
  private mentionNotificationsService: any = null;
  
  // Lazy loaded component
  mentionNotificationsComponent: Type<any> | null = null;

  constructor(
    private http: HttpClient,
    private auth: DemoAuthService,
    private router: Router,
    @Inject(API_BASE_URL) private baseUrl: string,
    private el: ElementRef<HTMLElement>,
    private injector: Injector
  ) {
    this.checkAuthRoute(this.router.url);
  }

  private checkAuthRoute(url: string) {
    this.isAuthPage = url.startsWith('/auth');
  }

  /** Lazy load NotificationsService */
  private async getNotify() {
    if (!this.notifyService) {
      const { NotificationsService } = await import('./notifications/notifications.service');
      this.notifyService = this.injector.get(NotificationsService);
    }
    return this.notifyService;
  }

  /** Lazy load SignalRService */
  private async getSignalR() {
    if (!this.signalrService) {
      const { SignalRService } = await import('./core/signalr.service');
      this.signalrService = this.injector.get(SignalRService);
    }
    return this.signalrService;
  }

  /** Lazy load MentionNotificationsService */
  private async getMentionNotifications() {
    if (!this.mentionNotificationsService) {
      const { MentionNotificationsService } = await import('./mentions/mention-notifications.service');
      this.mentionNotificationsService = this.injector.get(MentionNotificationsService);
    }
    return this.mentionNotificationsService;
  }

  ngOnInit(){
    this.updateScrollProgress();
    // When auth state changes, refresh progress and ensure SignalR is connected
    this.tokenSubscription = this.token$.subscribe((tok: string | null) => {
      if (tok) {
        this.loadProgress();
        this.loadMentionsComponent();
        this.connectSignalR(tok);
      } else {
        this.perProject = []; 
        this.overall = {completed:0,total:0,percent:0};
        this.mentionNotificationsComponent = null;
        this.disconnectSignalR();
      }
    });

    this.routerSubscription = this.router.events.pipe(
      filter((event): event is NavigationEnd => event instanceof NavigationEnd)
    ).subscribe(event => {
      this.checkAuthRoute(event.url);
    });
  }

  private async connectSignalR(tok: string) {
    if (!this.hubConnected) {
      try {
        const signalr = await this.getSignalR();
        await signalr.connect(tok);
        this.hubConnected = true;
        signalr.onTaskUpdated((_payload: any) => this.loadProgress());
        
        // Handle mention notifications in real-time
        signalr.onNotification(async (payload: any) => {
          if (payload?.type === 'mention') {
            const mentionSvc = await this.getMentionNotifications();
            mentionSvc.refreshUnreadCount();
          }
        });
      } catch { /* ignore */ }
    }
  }

  private async disconnectSignalR() {
    if (this.signalrService) {
      this.signalrService.disconnect();
      this.hubConnected = false;
    }
  }

  ngOnDestroy() {
    this.tokenSubscription?.unsubscribe();
    this.disconnectSignalR();
    this.routerSubscription?.unsubscribe();
  }

  ngAfterViewInit(){
    // Section reveal on scroll - exclude above-the-fold content (header, hero)
    const observer = new IntersectionObserver((entries) => {
      entries.forEach(e => {
        if (e.isIntersecting) {
          e.target.classList.add('in');
          observer.unobserve(e.target);
        }
      });
    }, { threshold: .12 });

    // Only observe below-the-fold reveal elements (skip header/hero for LCP)
    this.el.nativeElement.querySelectorAll('.reveal:not(.site-header):not(.hero)').forEach((sec: Element) => observer.observe(sec));
    
    // Immediately show above-the-fold content
    this.el.nativeElement.querySelectorAll('.site-header.reveal, .hero.reveal').forEach((sec: Element) => sec.classList.add('in'));
  }

  /** Lazy load the MentionNotificationsComponent */
  private async loadMentionsComponent() {
    if (!this.mentionNotificationsComponent) {
      const { MentionNotificationsComponent } = await import('./mentions/mention-notifications.component');
      this.mentionNotificationsComponent = MentionNotificationsComponent;
    }
  }

  /** Lazy load progress - only imports ProjectsService/TasksService when needed */
  async loadProgress(){
    this.loadingStats = true; 
    this.statsError = '';
    
    try {
      // Dynamic imports - these modules only load when user is authenticated
      const [{ ProjectsService }, { TasksService }, { forkJoin }] = await Promise.all([
        import('./projects/projects.service'),
        import('./tasks/tasks.service'),
        import('rxjs')
      ]);
      
      // Get services from Angular's injector (they're providedIn: 'root')
      const projects = this.injector.get(ProjectsService);
      const tasks = this.injector.get(TasksService);
      
      projects.getAll().subscribe({
        next: async (projs) => {
          if (!projs.length) { 
            this.perProject = []; 
            this.overall = {completed:0,total:0,percent:0}; 
            this.loadingStats = false; 
            return; 
          }
          const calls = projs.map(p => tasks.getAll(p.id));
          forkJoin(calls).subscribe({
            next: (tasksByProject) => {
              let total = 0, completed = 0;
              this.perProject = projs.map((p, i) => {
                const list = tasksByProject[i] || [];
                const t = list.length;
                const c = list.filter(x => x.isCompleted).length;
                total += t; completed += c;
                const percent = t ? Math.round((c / t) * 100) : 0;
                return { name: p.name, completed: c, total: t, percent };
              });
              const overallPercent = total ? Math.round((completed / total) * 100) : 0;
              this.overall = { completed, total, percent: overallPercent };
              this.loadingStats = false;
            },
            error: async _ => { 
              this.statsError = 'Failed to load progress'; 
              (await this.getNotify()).show('Failed to load progress'); 
              this.loadingStats = false; 
            }
          });
        },
        error: async _ => { 
          this.statsError = 'Failed to load projects'; 
          (await this.getNotify()).show('Failed to load projects'); 
          this.loadingStats = false; 
        }
      });
    } catch (e) {
      this.statsError = 'Failed to load progress';
      this.loadingStats = false;
    }
  }

  @HostListener('window:scroll')
  @HostListener('window:resize')
  updateScrollProgress(){
    const scrollTop = window.scrollY || document.documentElement.scrollTop || 0;
    const docHeight = document.documentElement.scrollHeight - document.documentElement.clientHeight;
    this.scrollProgress = docHeight > 0 ? Math.min(100, Math.max(0, (scrollTop / docHeight) * 100)) : 0;
  }

  primaryCta(){
    const token = this.auth.getToken();
    if (token) this.goHome();
    else this.router.navigate(['/auth/login'], { queryParams: { redirectUrl: '/projects' } });
  }

  goHome(){ this.router.navigateByUrl('/projects'); }

  async loginDemo() {
    const body = new HttpParams()
      .set('Email', 'admin@demo.local')
      .set('Password', 'demo123!');
    const headers = new HttpHeaders({ 'Content-Type': 'application/x-www-form-urlencoded' });
    this.http.post<{ token: string }>(`${this.baseUrl}/api/auth/login`, body.toString(), { headers })
      .subscribe({ 
        next: async r => { 
          this.auth.setToken(r.token); 
          (await this.getNotify()).show('Logged in'); 
          // Navigate to projects after demo login
          this.router.navigateByUrl('/projects', { replaceUrl: true });
        }, 
        error: async _ => (await this.getNotify()).show('Login failed') 
      });
  }

  async logout() { 
    this.auth.clear(); 
    this.router.navigateByUrl('/auth/login');
    (await this.getNotify()).show('Logged out');
  }
}
