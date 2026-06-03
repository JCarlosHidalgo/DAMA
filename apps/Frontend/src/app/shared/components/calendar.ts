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
import { courseColor, isoWeekdayIndex, shiftIsoDate } from '@core/utils';

import { calendarStyles } from './calendar.variants';

const MOBILE_BREAKPOINT_PX = 768;
const DEFAULT_SLOT_MIN_HOUR = 8;
const SLOT_MAX_TIME = '23:00:00';
const SUNDAY_DAY_INDEX = 0;
const SUNDAY_WEEKDAY_INDEX = 7;

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
    <div [class]="styles.toolbar()">
      <div [class]="styles.nav()">
        <button mat-icon-button (click)="prev()" matTooltip="Anterior">
          <fa-icon [icon]="faChevronLeft" />
        </button>
        <button mat-stroked-button (click)="today()">Hoy</button>
        <button mat-icon-button (click)="next()" matTooltip="Siguiente">
          <fa-icon [icon]="faChevronRight" />
        </button>
        <span [class]="styles.title()">{{ currentTitle() }}</span>
      </div>
      <div [class]="styles.spacer()"></div>
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

    <div [class]="styles.calendarHost()" #calendarHost>
      <full-calendar #fc [options]="calendarOptions()" />
    </div>
  `,
  host: { class: 'flex flex-col gap-3' },
  changeDetection: ChangeDetectionStrategy.OnPush,
})
export class Calendar {
  private readonly authService = inject(AuthService);
  private readonly fullCalendar = viewChild<FullCalendarComponent>('fc');
  private readonly calendarHost = viewChild<ElementRef<HTMLElement>>('calendarHost');

  protected readonly styles = calendarStyles();

  readonly entries = input<CourseScheduleEntry[]>([]);
  readonly anchorDate = input<string | null>(null);
  readonly mobile = input<boolean | null>(null);
  readonly selectedDayIndex = input<number | null>(null);

  readonly eventClick = output<CourseScheduleEntry>();
  readonly dateClick = output<string>();
  readonly weekDelta = output<number>();
  readonly dayDelta = output<number>();

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
      const dayIndex = this.selectedDayIndex();
      const dayView = this.activeView() === 'timeGridDay';
      this.entries();
      if (!calendarApi || !anchor) {
        return;
      }
      const target = dayView && dayIndex !== null ? shiftIsoDate(anchor, dayIndex - 1) : anchor;
      calendarApi.gotoDate(target);
    });
  }

  private activeView = computed(() => {
    const forcedMobile = this.mobile();
    const isMobile = forcedMobile ?? this.viewportIsMobile();
    return isMobile ? 'timeGridDay' : 'timeGridWeek';
  });

  private dayNavigationActive = computed(
    () => this.activeView() === 'timeGridDay' && this.selectedDayIndex() !== null,
  );

  private slotMinTime = computed(() => {
    const earliestHour = this.entries().reduce(
      (minHour, scheduleEntry) => Math.min(minHour, Number(scheduleEntry.startTime.slice(0, 2))),
      DEFAULT_SLOT_MIN_HOUR,
    );
    return `${String(earliestHour).padStart(2, '0')}:00:00`;
  });

  private hiddenDays = computed(() => {
    if (this.activeView() === 'timeGridDay') {
      return [];
    }
    const hasSunday = this.entries().some(
      (scheduleEntry) =>
        (scheduleEntry.dayOfWeekIndex ?? isoWeekdayIndex(scheduleEntry.date)) ===
        SUNDAY_WEEKDAY_INDEX,
    );
    return hasSunday ? [] : [SUNDAY_DAY_INDEX];
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
      hiddenDays: this.hiddenDays(),
      headerToolbar: false,
      height: 'auto',
      allDaySlot: false,
      slotMinTime: this.slotMinTime(),
      slotMaxTime: SLOT_MAX_TIME,
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
    if (this.dayNavigationActive()) {
      this.dayDelta.emit(-1);
      return;
    }
    this.weekDelta.emit(-1);
  }
  next(): void {
    if (this.dayNavigationActive()) {
      this.dayDelta.emit(1);
      return;
    }
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
