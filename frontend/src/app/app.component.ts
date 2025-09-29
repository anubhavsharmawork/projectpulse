import { Component, Inject, HostListener, OnInit, AfterViewInit, ElementRef } from '@angular/core';
import { Router } from '@angular/router';
import { HttpClient, HttpHeaders, HttpParams } from '@angular/common/http';
import { API_BASE_URL } from './core/api.config';
import { DemoAuthService } from './core/demo-auth.service';
import { ProjectsService, ProjectDto } from './projects/projects.service';
import { TasksService, TaskDto } from './tasks/tasks.service';
import { NotificationsService } from './notifications/notifications.service';
import { forkJoin } from 'rxjs';
import { SignalRService } from './core/signalr.service';

@Component({
  selector: 'app-root',
  template: `
  <div class="scroll-progress" [style.width.%]="scrollProgress" aria-hidden="true"></div>
  <header class="site-header reveal" role="banner">
    <mat-toolbar color="primary" class="topbar">
      <span class="brand" (click)="goHome()" tabindex="0" aria-label="Go to home">Project Pulse Management</span>
      <span class="spacer"></span>
      <nav class="main-nav" aria-label="Primary">
        <a mat-button *ngIf="(token$ | async)" routerLink="/projects">Projects</a>
        <a mat-button *ngIf="!(token$ | async)" routerLink="/auth/login">Login</a>
        <a mat-button *ngIf="!(token$ | async)" routerLink="/auth/register">Register</a>
        <button mat-button *ngIf="(token$ | async)" (click)="logout()">Logout</button>
      </nav>
    </mat-toolbar>

    <section class="hero reveal" aria-labelledby="uvp-heading">
      <div class="hero-bg" aria-hidden="true"></div>
      <div class="container hero-inner">
        <h1 id="uvp-heading" class="uvp">Build and track projects with clarity and speed</h1>
        <p class="tag">A minimalist, accessible project pulse demo that highlights real-time collaboration and thoughtful UX.</p>
        <div class="cta">
          <button class="cta-btn" (click)="primaryCta()">{{ (token$ | async) ? 'View Projects' : 'Login to View Projects' }}</button>
          <button class="ghost-btn" *ngIf="!(token$ | async)" (click)="loginDemo()">Try Demo Login</button>
        </div>
      </div>
    </section>
  </header>

  <main class="container" role="main">
    <router-outlet></router-outlet>

    <section class="skills reveal" aria-label="Progress" *ngIf="(token$ | async)">
      <h2 class="sr-only">Progress</h2>
      <div class="skills-grid">
        <div class="skill" aria-live="polite">
          <div class="skill-top">
            <span class="skill-name">Overall Task Completion</span>
            <span class="skill-val">{{overall.completed}} / {{overall.total}} ({{overall.percent}}%)</span>
          </div>
          <div class="skill-bar" role="progressbar" [attr.aria-valuenow]="overall.percent" aria-valuemin="0" aria-valuemax="100">
            <span class="skill-fill" [style.width.%]="overall.percent"></span>
          </div>
        </div>

        <div class="skill" *ngFor="let p of perProject" aria-live="polite">
          <div class="skill-top">
            <span class="skill-name">{{p.name}}</span>
            <span class="skill-val">{{p.completed}} / {{p.total}} ({{p.percent}}%)</span>
          </div>
          <div class="skill-bar" role="progressbar" [attr.aria-valuenow]="p.percent" aria-valuemin="0" aria-valuemax="100">
            <span class="skill-fill" [style.width.%]="p.percent"></span>
          </div>
        </div>
      </div>
      <div class="small" *ngIf="loadingStats">Calculating progress…</div>
      <div class="small" *ngIf="statsError" style="color:#e53935">{{statsError}}</div>
    </section>
  </main>

  <footer class="site-footer reveal" role="contentinfo">
    <div class="container footer-inner">
      <div class="small">&copy; {{year}} Anubhav Sharma. All rights reserved.</div>
      <div class="social" aria-label="Social links">
        <a href="https://www.linkedin.com/in/anubhav-sharma-/" target="_blank" rel="noreferrer" aria-label="LinkedIn" class="ml-sm">
          <span class="icon" aria-hidden="true">in</span>
          <span class="sr-only">LinkedIn</span>
        </a>
      </div>
    </div>
  </footer>
  `,
  styles: [
    `.spacer{flex:1 1 auto}`
  ]
})
export class AppComponent implements OnInit, AfterViewInit {
  token$ = this.auth.tokenChanges$;
  year = new Date().getFullYear();
  scrollProgress = 0;

  overall = { completed: 0, total: 0, percent: 0 };
  perProject: Array<{ name: string; completed: number; total: number; percent: number }> = [];
  loadingStats = false;
  statsError = '';
  private hubConnected = false;

  constructor(
    private http: HttpClient,
    private auth: DemoAuthService,
    private router: Router,
    @Inject(API_BASE_URL) private baseUrl: string,
    private el: ElementRef<HTMLElement>,
    private projects: ProjectsService,
    private tasks: TasksService,
    private notify: NotificationsService,
    private signalr: SignalRService
  ) {}

  ngOnInit(){
    this.updateScrollProgress();
    // When auth state changes, refresh progress and ensure SignalR is connected
    this.token$.subscribe((tok: string | null) => {
      if (tok) {
        this.loadProgress();
        if (!this.hubConnected) {
          this.signalr.connect(tok).then(() => {
            this.hubConnected = true;
            this.signalr.onTaskUpdated((_payload: any) => this.loadProgress());
          }).catch(() => { /* ignore */ });
        }
      } else {
        this.perProject = []; this.overall = {completed:0,total:0,percent:0};
      }
    });
  }

  ngAfterViewInit(){
    // Section reveal on scroll
    const observer = new IntersectionObserver((entries) => {
      entries.forEach(e => {
        if (e.isIntersecting) {
          e.target.classList.add('in');
          observer.unobserve(e.target);
        }
      });
    }, { threshold: .12 });

    this.el.nativeElement.querySelectorAll('.reveal').forEach(sec => observer.observe(sec));
  }

  loadProgress(){
    this.loadingStats = true; this.statsError = '';
    this.projects.getAll().subscribe({
      next: (projs: ProjectDto[]) => {
        if (!projs.length) { this.perProject = []; this.overall = {completed:0,total:0,percent:0}; this.loadingStats = false; return; }
        const calls = projs.map(p => this.tasks.getAll(p.id));
        forkJoin(calls).subscribe({
          next: (tasksByProject: TaskDto[][]) => {
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
          error: _ => { this.statsError = 'Failed to load progress'; this.notify.show('Failed to load progress'); this.loadingStats = false; }
        });
      },
      error: _ => { this.statsError = 'Failed to load projects'; this.notify.show('Failed to load projects'); this.loadingStats = false; }
    });
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

  loginDemo() {
    const body = new HttpParams()
      .set('Email', 'admin@demo.local')
      .set('Password', 'demo123!');
    const headers = new HttpHeaders({ 'Content-Type': 'application/x-www-form-urlencoded' });
    this.http.post<{ token: string }>(`${this.baseUrl}/api/auth/login`, body.toString(), { headers })
      .subscribe({ next: r => { this.auth.setToken(r.token); this.notify.show('Logged in'); }, error: _ => this.notify.show('Login failed') });
  }

  logout() { this.auth.clear(); this.router.navigateByUrl('/auth/login'); this.notify.show('Logged out'); }
}
