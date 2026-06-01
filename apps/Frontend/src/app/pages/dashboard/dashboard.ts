import {
  ChangeDetectionStrategy,
  Component,
  computed,
  DestroyRef,
  inject,
  signal,
  viewChild,
} from '@angular/core';
import { takeUntilDestroyed, toSignal } from '@angular/core/rxjs-interop';
import { NavigationEnd, Router, RouterOutlet, RouterLink } from '@angular/router';
import { filter, map, startWith } from 'rxjs';
import { MatSidenav, MatSidenavModule } from '@angular/material/sidenav';
import { MatToolbarModule } from '@angular/material/toolbar';
import { MatButtonModule } from '@angular/material/button';
import { MatListModule } from '@angular/material/list';
import { BreakpointObserver, Breakpoints } from '@angular/cdk/layout';
import { FaIconComponent } from '@fortawesome/angular-fontawesome';
import {
  IconDefinition,
  faGauge,
  faCreditCard,
  faChalkboardUser,
  faCalendarDays,
  faReceipt,
  faGraduationCap,
  faUsers,
  faMoneyBill,
  faQrcode,
  faCalendarCheck,
  faBars,
  faRightFromBracket,
  faBuilding,
  faGear,
} from '@fortawesome/free-solid-svg-icons';

import { AuthService, UserRole } from '@core/auth';

import { dashboardStyles } from './dashboard.variants';

interface TabEntry {
  label: string;
  icon: IconDefinition;
  path: string;
}

const TABS_BY_ROLE: Record<UserRole, TabEntry[]> = {
  Client: [
    { label: 'Resumen', icon: faGauge, path: 'resumen' },
    { label: 'Recarga', icon: faCreditCard, path: 'recarga' },
    { label: 'Cursos', icon: faChalkboardUser, path: 'clases' },
    { label: 'Horario', icon: faCalendarDays, path: 'horario' },
    { label: 'Plantillas de Cobro', icon: faReceipt, path: 'plantillas-cobro' },
    { label: 'Estudiantes', icon: faGraduationCap, path: 'estudiantes' },
    { label: 'Profesores', icon: faUsers, path: 'profesores' },
    { label: 'Configuración', icon: faGear, path: 'configuracion' },
  ],
  Teacher: [{ label: 'Horario', icon: faCalendarDays, path: 'horario' }],
  Student: [
    { label: 'Resumen', icon: faGauge, path: 'resumen' },
    { label: 'Horario', icon: faCalendarDays, path: 'horario' },
    { label: 'Pagar Clases', icon: faMoneyBill, path: 'pagar-clases' },
    { label: 'Estado de Deudas', icon: faReceipt, path: 'estado-deudas' },
    { label: 'Marcar Asistencia', icon: faQrcode, path: 'marcar-asistencia' },
    { label: 'Mis Asistencias', icon: faCalendarCheck, path: 'mis-asistencias' },
  ],
  Admin: [{ label: 'Tenants', icon: faBuilding, path: 'tenants' }],
};

@Component({
  selector: 'app-dashboard',
  imports: [
    RouterOutlet,
    RouterLink,
    MatSidenavModule,
    MatToolbarModule,
    MatButtonModule,
    MatListModule,
    FaIconComponent,
  ],
  templateUrl: './dashboard.html',
  changeDetection: ChangeDetectionStrategy.OnPush,
})
export class Dashboard {
  private readonly authService = inject(AuthService);
  private readonly router = inject(Router);
  private readonly breakpoints = inject(BreakpointObserver);
  private readonly destroyRef = inject(DestroyRef);

  readonly sidenav = viewChild.required<MatSidenav>('sidenav');
  readonly isHandset = signal(this.breakpoints.isMatched(Breakpoints.Handset));
  readonly expanded = signal(!this.breakpoints.isMatched(Breakpoints.Handset));
  readonly collapsed = computed(() => !this.expanded() && !this.isHandset());
  protected readonly styles = computed(() => dashboardStyles({ collapsed: this.collapsed() }));
  readonly tabs = computed<TabEntry[]>(() => {
    const role = this.authService.currentRole();
    return role ? TABS_BY_ROLE[role] : [];
  });
  readonly displayName = computed(() => this.authService.claims()?.userName ?? '');
  readonly activeSegment = toSignal(
    this.router.events.pipe(
      filter((event): event is NavigationEnd => event instanceof NavigationEnd),
      map((event) => lastSegment(event.urlAfterRedirects)),
      startWith(lastSegment(this.router.url)),
    ),
    { initialValue: lastSegment(this.router.url) },
  );
  readonly faSchool = faGraduationCap;
  readonly faMenu = faBars;
  readonly faLogout = faRightFromBracket;

  constructor() {
    this.breakpoints
      .observe([Breakpoints.Handset])
      .pipe(takeUntilDestroyed(this.destroyRef))
      .subscribe((state) => {
        this.isHandset.set(state.matches);
        if (state.matches) {
          this.expanded.set(false);
        }
      });
  }

  toggleSidenav(): void {
    if (this.isHandset()) {
      this.sidenav().toggle();
    } else {
      this.expanded.update((isExpanded) => !isExpanded);
    }
  }

  onLogout(): void {
    this.authService.logout();
    this.router.navigateByUrl('/');
  }
}

function lastSegment(url: string): string {
  const pathWithoutQuery = url.split('?')[0].split('#')[0];
  const segments = pathWithoutQuery.split('/').filter((segment) => segment.length > 0);
  return segments[segments.length - 1] ?? '';
}
