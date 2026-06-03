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
  faStar,
  faLayerGroup,
} from '@fortawesome/free-solid-svg-icons';

import { AuthService, UserRole } from '@core/auth';

import { ThemeToggle } from '@shared/components';

import { dashboardStyles } from './dashboard.variants';

interface TabEntry {
  label: string;
  icon: IconDefinition;
  path: string;
  minIndex: number;
}

const TABS_BY_ROLE: Record<UserRole, TabEntry[]> = {
  Client: [
    { label: 'Resumen', icon: faGauge, path: 'resumen', minIndex: 0 },
    { label: 'Cursos', icon: faChalkboardUser, path: 'clases', minIndex: 1 },
    { label: 'Horario', icon: faCalendarDays, path: 'horario', minIndex: 1 },
    { label: 'Estudiantes', icon: faGraduationCap, path: 'estudiantes', minIndex: 2 },
    { label: 'Profesores', icon: faUsers, path: 'profesores', minIndex: 2 },
    { label: 'Recarga', icon: faCreditCard, path: 'recarga', minIndex: 3 },
    { label: 'Plantillas de Cobro', icon: faReceipt, path: 'plantillas-cobro', minIndex: 3 },
    { label: 'Suscripción', icon: faStar, path: 'suscripcion', minIndex: 0 },
    { label: 'Configuración', icon: faGear, path: 'configuracion', minIndex: 0 },
  ],
  Teacher: [{ label: 'Horario', icon: faCalendarDays, path: 'horario', minIndex: 1 }],
  Student: [
    { label: 'Horario', icon: faCalendarDays, path: 'horario', minIndex: 1 },
    { label: 'Resumen', icon: faGauge, path: 'resumen', minIndex: 2 },
    { label: 'Marcar Asistencia', icon: faQrcode, path: 'marcar-asistencia', minIndex: 2 },
    { label: 'Mis Asistencias', icon: faCalendarCheck, path: 'mis-asistencias', minIndex: 2 },
    { label: 'Pagar Clases', icon: faMoneyBill, path: 'pagar-clases', minIndex: 3 },
    { label: 'Estado de Deudas', icon: faReceipt, path: 'estado-deudas', minIndex: 3 },
  ],
  Admin: [
    { label: 'Tenants', icon: faBuilding, path: 'tenants', minIndex: 0 },
    { label: 'Planes de Suscripción', icon: faLayerGroup, path: 'planes-suscripcion', minIndex: 0 },
  ],
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
    ThemeToggle,
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
    if (!role) {
      return [];
    }
    const effectiveIndex = this.authService.effectiveSubscriptionIndex();
    return TABS_BY_ROLE[role].filter((tab) => effectiveIndex >= tab.minIndex);
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
