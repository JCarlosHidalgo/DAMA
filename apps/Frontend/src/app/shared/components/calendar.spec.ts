import {
  Component,
  input,
  signal,
  provideZonelessChangeDetection,
  WritableSignal,
} from '@angular/core';
import type { CalendarOptions } from '@fullcalendar/core';
import { TestBed } from '@angular/core/testing';
import { FullCalendarModule } from '@fullcalendar/angular';
import { describe, it, expect, beforeEach, afterEach, vi } from 'vitest';

import { Calendar } from './calendar';
import { AuthService } from '@core/auth';
import { CourseScheduleEntry } from '@core/models';

interface FakeCalendarApi {
  prev: ReturnType<typeof vi.fn>;
  next: ReturnType<typeof vi.fn>;
  today: ReturnType<typeof vi.fn>;
  gotoDate: ReturnType<typeof vi.fn>;
  view: { type: string; title: string; activeStart: Date };
  changeView: ReturnType<typeof vi.fn>;
}

function buildFakeCalendarApi(): FakeCalendarApi {
  return {
    prev: vi.fn(),
    next: vi.fn(),
    today: vi.fn(),
    gotoDate: vi.fn(),
    view: {
      type: 'timeGridWeek',
      title: 'Semana de prueba',
      activeStart: new Date('2026-04-06T00:00:00Z'),
    },
    changeView: vi.fn(),
  };
}

let sharedFakeApi: FakeCalendarApi = buildFakeCalendarApi();

@Component({
  // eslint-disable-next-line @angular-eslint/component-selector
  selector: 'full-calendar',
  standalone: true,
  template: '',
})
class StubFullCalendarComponent {
  readonly options = input<CalendarOptions | undefined>(undefined);
  getApi(): FakeCalendarApi {
    return sharedFakeApi;
  }
}

describe('Calendar', () => {
  let fixture: ReturnType<typeof TestBed.createComponent<Calendar>>;
  const authStub = {
    tenantTimezone: signal('America/La_Paz'),
  };

  const sampleEntries: CourseScheduleEntry[] = [
    {
      classId: 'sched-1',
      classKind: 'Scheduled',
      courseId: 'course-1',
      courseName: 'Yoga',
      date: '2026-04-06',
      startTime: '08:00:00',
      endTime: '09:00:00',
      teachers: [{ teacherId: 't-1', teacherName: 'Ana' }],
      dayOfWeekIndex: 1,
      maxStudentLimit: 30,
      groupId: 'group-1',
      groupName: 'Grupo 1',
    },
    {
      classId: 'unique-1',
      classKind: 'Unique',
      courseId: 'course-2',
      courseName: 'Pilates',
      date: '2026-04-07',
      startTime: '10:00:00',
      endTime: '11:00:00',
      teachers: [],
      maxStudentLimit: 0,
      groupId: 'group-2',
      groupName: 'Grupo 2',
    },
  ];

  beforeEach(async () => {
    TestBed.resetTestingModule();
    sharedFakeApi = buildFakeCalendarApi();

    await TestBed.configureTestingModule({
      imports: [Calendar],
      providers: [provideZonelessChangeDetection(), { provide: AuthService, useValue: authStub }],
    })
      .overrideComponent(Calendar, {
        remove: { imports: [FullCalendarModule] },
        add: { imports: [StubFullCalendarComponent] },
      })
      .compileComponents();
    fixture = TestBed.createComponent(Calendar);
  });

  afterEach(() => {
    if (fixture) {
      fixture.destroy();
    }
  });

  it('mounts and renders the toolbar', () => {
    fixture.detectChanges();
    expect(fixture.nativeElement.querySelector('.toolbar')).not.toBeNull();
    expect(fixture.nativeElement.querySelector('full-calendar')).not.toBeNull();
  });

  it('builds calendar events from the entries input', () => {
    fixture.componentRef.setInput('entries', sampleEntries);
    fixture.detectChanges();

    const options = (
      fixture.componentInstance as unknown as {
        calendarOptions: () => { events: { id: string; title: string }[] };
      }
    ).calendarOptions();
    expect(options.events).toHaveLength(2);
    expect(options.events[0].id).toBe('Scheduled:sched-1');
    expect(options.events[0].title).toBe('Yoga');
  });

  it('uses the authService tenant timezone in options', () => {
    fixture.detectChanges();
    const options = (
      fixture.componentInstance as unknown as {
        calendarOptions: () => { timeZone: string };
      }
    ).calendarOptions();
    expect(options.timeZone).toBe('America/La_Paz');
  });

  it('prev() emits weekDelta=-1', () => {
    fixture.detectChanges();
    let emitted: number | undefined;
    fixture.componentInstance.weekDelta.subscribe((delta: number) => (emitted = delta));

    fixture.componentInstance.prev();

    expect(emitted).toBe(-1);
  });

  it('next() emits weekDelta=1', () => {
    fixture.detectChanges();
    let emitted: number | undefined;
    fixture.componentInstance.weekDelta.subscribe((delta: number) => (emitted = delta));

    fixture.componentInstance.next();

    expect(emitted).toBe(1);
  });

  it('today() emits weekDelta=0', () => {
    fixture.detectChanges();
    let emitted: number | undefined;
    fixture.componentInstance.weekDelta.subscribe((delta: number) => (emitted = delta));

    fixture.componentInstance.today();

    expect(emitted).toBe(0);
  });

  it('navigates the calendar to the anchorDate when it changes', () => {
    fixture.detectChanges();
    expect(sharedFakeApi.gotoDate).not.toHaveBeenCalled();

    fixture.componentRef.setInput('anchorDate', '2026-04-13');
    fixture.detectChanges();

    expect(sharedFakeApi.gotoDate).toHaveBeenCalledWith('2026-04-13');
  });

  describe('calendarOptions callbacks', () => {
    function options() {
      return (
        fixture.componentInstance as unknown as {
          calendarOptions: () => {
            eventClick: (arg: { event: { extendedProps: Record<string, unknown> } }) => void;
            dateClick: (arg: { dateStr: string }) => void;
            datesSet: (arg: { view: { title: string } }) => void;
            eventContent: (arg: { event: { extendedProps: Record<string, unknown> } }) => {
              domNodes: HTMLElement[];
            };
          };
        }
      ).calendarOptions();
    }

    it('eventClick emits the entry via eventClick output', () => {
      fixture.componentRef.setInput('entries', sampleEntries);
      fixture.detectChanges();

      let emitted: CourseScheduleEntry | undefined;
      fixture.componentInstance.eventClick.subscribe((entry) => (emitted = entry));

      options().eventClick({
        event: { extendedProps: { entry: sampleEntries[0] } },
      });

      expect(emitted?.classId).toBe('sched-1');
    });

    it('dateClick forwards the dateStr', () => {
      fixture.detectChanges();
      let emitted: string | undefined;
      fixture.componentInstance.dateClick.subscribe((date) => (emitted = date));

      options().dateClick({ dateStr: '2026-04-07' });

      expect(emitted).toBe('2026-04-07');
    });

    it('datesSet updates currentTitle signal', () => {
      fixture.detectChanges();
      options().datesSet({ view: { title: 'Mayo 2026' } });
      expect(
        (fixture.componentInstance as unknown as { currentTitle: () => string }).currentTitle(),
      ).toBe('Mayo 2026');
    });

    it('eventContent renders course name and time when showDetails is on', () => {
      fixture.componentRef.setInput('entries', sampleEntries);
      fixture.detectChanges();

      const { domNodes } = options().eventContent({
        event: { extendedProps: { entry: sampleEntries[0] } },
      });

      const root = domNodes[0];
      expect(root.querySelector('.ev-course')?.textContent).toBe('Yoga');
      expect(root.querySelector('.ev-time')?.textContent).toContain('08:00');
      expect(root.querySelector('.ev-teachers')?.textContent).toContain('Ana');
    });

    it('eventContent keeps the time but hides teachers and limit when showDetails is off', () => {
      fixture.componentRef.setInput('entries', sampleEntries);
      (
        fixture.componentInstance as unknown as { showDetails: WritableSignal<boolean> }
      ).showDetails.set(false);
      fixture.detectChanges();

      const { domNodes } = options().eventContent({
        event: { extendedProps: { entry: sampleEntries[0] } },
      });

      const root = domNodes[0];
      expect(root.querySelector('.ev-time')?.textContent).toContain('08:00');
      expect(root.querySelector('.ev-teachers')).toBeNull();
      expect(root.querySelector('.ev-limit')).toBeNull();
    });

    it('eventContent never renders edit or delete buttons', () => {
      fixture.componentRef.setInput('entries', sampleEntries);
      fixture.detectChanges();

      const { domNodes } = options().eventContent({
        event: { extendedProps: { entry: sampleEntries[0] } },
      });
      expect(domNodes[0].querySelectorAll('button').length).toBe(0);
    });

    it('eventContent omits teachers element when teachers array is empty', () => {
      fixture.componentRef.setInput('entries', sampleEntries);
      fixture.detectChanges();

      const { domNodes } = options().eventContent({
        event: { extendedProps: { entry: sampleEntries[1] } },
      });
      expect(domNodes[0].querySelector('.ev-teachers')).toBeNull();
    });
  });
});
