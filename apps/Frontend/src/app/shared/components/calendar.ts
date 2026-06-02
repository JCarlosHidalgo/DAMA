import {
  ChangeDetectionStrategy,
  Component,
  computed,
  effect,
  ElementRef,
  inject,
  input,
  output,
  signal,
  viewChild,
} from '@angular/core';
import { MatButtonModule } from '@angular/material/button';
import { MatSlideToggleModule } from '@angular/material/slide-toggle';
import { MatTooltipModule } from '@angular/material/tooltip';
import { FaIconComponent } from '@fortawesome/angular-fontawesome';
import { faChevronLeft, faChevronRight, faImage } from '@fortawesome/free-solid-svg-icons';
import { FullCalendarModule, FullCalendarComponent } from '@fullcalendar/angular';
import { CalendarOptions, EventClickArg, EventInput } from '@fullcalendar/core';
import esLocale from '@fullcalendar/core/locales/es';
import dayGridPlugin from '@fullcalendar/daygrid';
import interactionPlugin, { DateClickArg } from '@fullcalendar/interaction';
import timeGridPlugin from '@fullcalendar/timegrid';

import { AuthService } from '@core/auth';
import { CourseScheduleEntry } from '@core/models';
import { courseColor } from '@core/utils';

const MOBILE_BREAKPOINT_PX = 768;

@Component({
  selector: 'app-calendar',
  imports: [
    FullCalendarModule,
    MatButtonModule,
    MatSlideToggleModule,
    MatTooltipModule,
    FaIconComponent,
  ],
  template: `
    <div class="toolbar">
      <div class="nav">
        <button mat-icon-button (click)="prev()" matTooltip="Anterior">
          <fa-icon [icon]="faChevronLeft" />
        </button>
        <button mat-stroked-button (click)="today()">Hoy</button>
        <button mat-icon-button (click)="next()" matTooltip="Siguiente">
          <fa-icon [icon]="faChevronRight" />
        </button>
        <span class="title">{{ currentTitle() }}</span>
      </div>
      <div class="spacer"></div>
      <mat-slide-toggle [checked]="showDetails()" (change)="showDetails.set($event.checked)">
        Mostrar datos de clases
      </mat-slide-toggle>
      <button
        mat-stroked-button
        (click)="captureScreenshot()"
        matTooltip="Descargar imagen"
        [disabled]="capturing()"
      >
        <fa-icon [icon]="faImage" /> Captura
      </button>
    </div>

    <div class="calendar-host" #calendarHost>
      <full-calendar #fc [options]="calendarOptions()" />
    </div>
  `,
  styles: `
    :host {
      display: flex;
      flex-direction: column;
      gap: 12px;
    }
    .toolbar {
      display: flex;
      align-items: center;
      gap: 12px;
      flex-wrap: wrap;
    }
    .nav {
      display: flex;
      align-items: center;
      gap: 4px;
    }
    .title {
      margin-left: 8px;
      font-weight: 600;
    }
    .spacer {
      flex: 1;
    }
    .calendar-host {
      background: var(--mat-sys-surface);
      border-radius: 8px;
      padding: 8px;
    }
    .fc-event-body {
      font-size: 12px;
      line-height: 1.25;
      padding: 2px 4px;
      display: flex;
      flex-direction: column;
      gap: 1px;
    }
    .fc-event-body .ev-course {
      font-weight: 600;
    }
    .fc-event-body .ev-time {
      opacity: 0.9;
      font-variant-numeric: tabular-nums;
    }
    .fc-event-body .ev-teachers {
      opacity: 0.85;
    }
  `,
  changeDetection: ChangeDetectionStrategy.OnPush,
})
export class Calendar {
  private readonly authService = inject(AuthService);
  private readonly fullCalendar = viewChild<FullCalendarComponent>('fc');
  private readonly calendarHost = viewChild<ElementRef<HTMLElement>>('calendarHost');

  readonly entries = input<CourseScheduleEntry[]>([]);
  readonly anchorDate = input<string | null>(null);
  readonly mobile = input<boolean | null>(null);

  readonly eventClick = output<CourseScheduleEntry>();
  readonly dateClick = output<string>();
  readonly weekDelta = output<number>();

  protected readonly showDetails = signal(true);
  protected readonly currentTitle = signal('');
  protected readonly capturing = signal(false);
  protected readonly faChevronLeft = faChevronLeft;
  protected readonly faChevronRight = faChevronRight;
  protected readonly faImage = faImage;
  private readonly viewportIsMobile = signal(this.detectMobile());

  private readonly resizeHandler = () => this.viewportIsMobile.set(this.detectMobile());

  constructor() {
    window.addEventListener('resize', this.resizeHandler);
    effect((onCleanup) => {
      onCleanup(() => window.removeEventListener('resize', this.resizeHandler));
    });
    effect(() => {
      const calendarApi = this.fullCalendar()?.getApi();
      if (!calendarApi) {
        return;
      }
      const wantedView = this.activeView();
      if (calendarApi.view.type !== wantedView) {
        calendarApi.changeView(wantedView);
      }
      this.currentTitle.set(calendarApi.view.title);
    });
    effect(() => {
      const calendarApi = this.fullCalendar()?.getApi();
      const anchor = this.anchorDate();
      this.entries();
      if (!calendarApi || !anchor) {
        return;
      }
      calendarApi.gotoDate(anchor);
    });
  }

  private activeView = computed(() => {
    const forcedMobile = this.mobile();
    const isMobile = forcedMobile ?? this.viewportIsMobile();
    return isMobile ? 'timeGridDay' : 'timeGridWeek';
  });

  protected readonly calendarOptions = computed<CalendarOptions>(() => {
    const tenantTimezone = this.authService.tenantTimezone();
    const showDetails = this.showDetails();
    const events: EventInput[] = this.entries().map((scheduleEntry) => ({
      id: `${scheduleEntry.classKind}:${scheduleEntry.classId}`,
      title: scheduleEntry.courseName,
      start: `${scheduleEntry.date}T${scheduleEntry.startTime}`,
      end: `${scheduleEntry.date}T${scheduleEntry.endTime}`,
      backgroundColor: courseColor(scheduleEntry.courseId),
      borderColor: courseColor(scheduleEntry.courseId),
      extendedProps: { entry: scheduleEntry, showDetails },
    }));

    return {
      plugins: [dayGridPlugin, timeGridPlugin, interactionPlugin],
      initialView: this.activeView(),
      initialDate: this.anchorDate() ?? undefined,
      locale: esLocale,
      timeZone: tenantTimezone,
      firstDay: 1,
      headerToolbar: false,
      height: 'auto',
      allDaySlot: false,
      slotMinTime: '06:00:00',
      slotMaxTime: '23:00:00',
      nowIndicator: true,
      events,
      eventClick: (clickArg: EventClickArg) => {
        const entry = clickArg.event.extendedProps['entry'] as CourseScheduleEntry;
        this.eventClick.emit(entry);
      },
      dateClick: (dateArg: DateClickArg) => this.dateClick.emit(dateArg.dateStr),
      eventContent: (contentArg) => {
        const entry = contentArg.event.extendedProps['entry'] as CourseScheduleEntry;
        const eventBodyElement = document.createElement('div');
        eventBodyElement.className = 'fc-event-body';

        const courseElement = document.createElement('div');
        courseElement.className = 'ev-course';
        courseElement.textContent = entry.courseName;
        eventBodyElement.appendChild(courseElement);

        const timeElement = document.createElement('div');
        timeElement.className = 'ev-time';
        timeElement.textContent = `${entry.startTime.slice(0, 5)} – ${entry.endTime.slice(0, 5)}`;
        eventBodyElement.appendChild(timeElement);

        if (showDetails) {
          if (entry.teachers.length > 0) {
            const teachersElement = document.createElement('div');
            teachersElement.className = 'ev-teachers';
            teachersElement.textContent = entry.teachers
              .map((teacher) => teacher.teacherName)
              .join(', ');
            eventBodyElement.appendChild(teachersElement);
          }

          const limitElement = document.createElement('div');
          limitElement.className = 'ev-limit';
          limitElement.textContent =
            entry.maxStudentLimit > 0 ? `Cupo: ${entry.maxStudentLimit}` : 'Sin límite';
          eventBodyElement.appendChild(limitElement);
        }

        return { domNodes: [eventBodyElement] };
      },
      datesSet: (datesArg) => this.currentTitle.set(datesArg.view.title),
    };
  });

  prev(): void {
    this.weekDelta.emit(-1);
  }
  next(): void {
    this.weekDelta.emit(1);
  }
  today(): void {
    this.weekDelta.emit(0);
  }

  async captureScreenshot(): Promise<void> {
    const calendarApi = this.fullCalendar()?.getApi();
    const hostNode = this.calendarHost()?.nativeElement;
    if (!calendarApi || !hostNode) {
      return;
    }
    const previousView = calendarApi.view.type;
    const forceDesktop = previousView !== 'timeGridWeek';
    this.capturing.set(true);
    try {
      if (forceDesktop) {
        calendarApi.changeView('timeGridWeek');
      }
      await new Promise((resolve) => requestAnimationFrame(() => resolve(null)));
      const { toPng } = await import('html-to-image');
      const pngDataUrl = await toPng(hostNode, { cacheBust: true, pixelRatio: 2 });
      const downloadLink = document.createElement('a');
      downloadLink.download = `horario-${calendarApi.view.activeStart.toISOString().slice(0, 10)}.png`;
      downloadLink.href = pngDataUrl;
      downloadLink.click();
    } finally {
      if (forceDesktop) {
        calendarApi.changeView(previousView);
      }
      this.capturing.set(false);
    }
  }

  private detectMobile(): boolean {
    return typeof window !== 'undefined' && window.innerWidth < MOBILE_BREAKPOINT_PX;
  }
}
