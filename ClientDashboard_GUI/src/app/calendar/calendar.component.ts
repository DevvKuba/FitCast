import { AfterViewInit, Component, ElementRef, OnDestroy, OnInit, ViewChild, inject } from '@angular/core';
import { BookingService } from '../services/booking.service';
import { TrainerService } from '../services/trainer.service';
import { ToastService } from '../services/toast.service';
import { BookedSessionSlotViewDto } from '../models/dtos/booked-session-slot-view-dto';

// Grid measurements - one hour row is 56px tall, as in the Stitch design
const HOUR_HEIGHT = 56;
const HOURS = Array.from({ length: 24 }, (_, hour) => hour);
const FIRST_VISIBLE_HOUR = 6; // the grid opens scrolled to 06:00
const MIN_BLOCK_HEIGHT = 18;

interface CalendarDay {
  date: Date;
  isToday: boolean;
  isWeekend: boolean;
}

interface DurationColours {
  block: string;
  name: string;
  time: string;
}

// A session placed on the grid: positions are px from the top of the day, and % across the day column
interface CalendarSession {
  slot: BookedSessionSlotViewDto;
  start: Date;
  end: Date;
  durationMinutes: number;
  top: number;
  height: number;
  left: number;
  width: number;
  colours: DurationColours;
}

@Component({
  selector: 'app-calendar',
  imports: [],
  templateUrl: './calendar.component.html',
  styleUrl: './calendar.component.css'
})
export class CalendarComponent implements OnInit, AfterViewInit, OnDestroy {
  @ViewChild('gridScroll') gridScroll!: ElementRef<HTMLDivElement>;

  bookingService = inject(BookingService);
  trainerService = inject(TrainerService);
  toastService = inject(ToastService);

  hours = HOURS;
  hourHeight = HOUR_HEIGHT;

  // Times are shown in the device's time zone (browser), not the trainer's profile zone
  browserTimeZoneId = Intl.DateTimeFormat().resolvedOptions().timeZone;
  trainerTimeZoneId = '';

  weekStart: Date = this.getMondayOf(new Date());
  days: CalendarDay[] = [];
  sessionsByDay: CalendarSession[][] = [];

  bookingsCount = 0;
  coachedHours = 0;

  now = new Date();
  private clockTimer?: ReturnType<typeof setInterval>;

  ngOnInit(): void {
    this.loadWeek();
    this.retrieveTrainerTimeZone();

    // keeps the current-time line moving
    this.clockTimer = setInterval(() => this.now = new Date(), 60 * 1000);
  }

  ngAfterViewInit(): void {
    // a little above 06:00 so its hour label isn't clipped by the top of the grid
    this.gridScroll.nativeElement.scrollTop = FIRST_VISIBLE_HOUR * HOUR_HEIGHT - 12;
  }

  ngOnDestroy(): void {
    clearInterval(this.clockTimer);
  }

  // ---- Week navigation ----

  goToPreviousWeek(): void {
    this.weekStart = this.addDays(this.weekStart, -7);
    this.loadWeek();
  }

  goToNextWeek(): void {
    this.weekStart = this.addDays(this.weekStart, 7);
    this.loadWeek();
  }

  goToToday(): void {
    this.weekStart = this.getMondayOf(new Date());
    this.loadWeek();
  }

  // ---- Data loading ----

  loadWeek(): void {
    this.days = this.buildDays();

    // Monday 00:00 to next Monday 00:00 in the browser's zone, sent as UTC
    const fromUtc = this.weekStart.toISOString();
    const toUtc = this.addDays(this.weekStart, 7).toISOString();

    this.bookingService.getTrainerBookedSessionSlots(fromUtc, toUtc).subscribe({
      next: (response) => {
        this.placeSessions(response.data ?? []);
      },
      error: (response) => {
        this.toastService.showError('Error Loading Calendar', response.error?.message ?? 'Sessions could not be loaded');
      }
    });
  }

  retrieveTrainerTimeZone(): void {
    this.trainerService.retrieveTrainerById().subscribe({
      next: (response) => {
        this.trainerTimeZoneId = response.data?.timeZoneId ?? '';
      }
    });
  }

  // ---- Template helpers ----

  // e.g. "November 2026", or "Nov – Dec 2026" / "Dec 2026 – Jan 2027" for a week spanning two months
  get monthLabel(): string {
    const weekEnd = this.addDays(this.weekStart, 6);

    if (this.weekStart.getMonth() === weekEnd.getMonth()) {
      return this.weekStart.toLocaleDateString('en-GB', { month: 'long', year: 'numeric' });
    }
    if (this.weekStart.getFullYear() === weekEnd.getFullYear()) {
      return `${this.weekStart.toLocaleDateString('en-GB', { month: 'short' })} – ${weekEnd.toLocaleDateString('en-GB', { month: 'short', year: 'numeric' })}`;
    }
    return `${this.weekStart.toLocaleDateString('en-GB', { month: 'short', year: 'numeric' })} – ${weekEnd.toLocaleDateString('en-GB', { month: 'short', year: 'numeric' })}`;
  }

  // e.g. "9 – 15 Nov 2026", "30 Nov – 6 Dec 2026", "29 Dec 2026 – 4 Jan 2027"
  get weekRangeLabel(): string {
    const weekEnd = this.addDays(this.weekStart, 6);
    const sameMonth = this.weekStart.getMonth() === weekEnd.getMonth();
    const sameYear = this.weekStart.getFullYear() === weekEnd.getFullYear();

    const endLabel = weekEnd.toLocaleDateString('en-GB', { day: 'numeric', month: 'short', year: 'numeric' });

    if (sameMonth && sameYear) {
      return `${this.weekStart.getDate()} – ${endLabel}`;
    }
    if (sameYear) {
      return `${this.weekStart.toLocaleDateString('en-GB', { day: 'numeric', month: 'short' })} – ${endLabel}`;
    }
    return `${this.weekStart.toLocaleDateString('en-GB', { day: 'numeric', month: 'short', year: 'numeric' })} – ${endLabel}`;
  }

  // e.g. "GMT+1" - the browser's offset for the week on screen
  get browserOffsetLabel(): string {
    const parts = new Intl.DateTimeFormat('en-GB', { timeZoneName: 'shortOffset' }).formatToParts(this.weekStart);
    const offset = parts.find(part => part.type === 'timeZoneName')?.value ?? 'GMT';
    return offset === 'GMT' ? 'GMT+0' : offset;
  }

  // Only shown when the device is in a different zone to the trainer's saved schedule zone
  get showTimeZoneNote(): boolean {
    return this.trainerTimeZoneId !== '' && this.trainerTimeZoneId !== this.browserTimeZoneId;
  }

  get currentTimeTop(): number {
    return (this.now.getHours() * 60 + this.now.getMinutes()) / 60 * HOUR_HEIGHT;
  }

  formatWeekday(date: Date): string {
    return date.toLocaleDateString('en-GB', { weekday: 'short' });
  }

  formatTime(date: Date): string {
    return date.toLocaleTimeString('en-GB', { hour: '2-digit', minute: '2-digit' });
  }

  formatHour(hour: number): string {
    return `${hour.toString().padStart(2, '0')}:00`;
  }

  // ---- Session layout ----

  private placeSessions(slots: BookedSessionSlotViewDto[]): void {
    this.sessionsByDay = [[], [], [], [], [], [], []];
    let totalMinutes = 0;

    for (const slot of slots) {
      const start = new Date(slot.startDateTime);
      const end = new Date(slot.endDateTime);
      const durationMinutes = Math.round((end.getTime() - start.getTime()) / 60000);
      const dayIndex = this.getDayIndex(start);

      totalMinutes += durationMinutes;

      if (dayIndex < 0 || dayIndex > 6) {
        continue;
      }

      this.sessionsByDay[dayIndex].push(this.buildSession(slot, start, end, durationMinutes));
    }

    for (const daySessions of this.sessionsByDay) {
      this.layOutOverlaps(daySessions);
    }

    this.bookingsCount = slots.length;
    this.coachedHours = Math.round(totalMinutes / 6) / 10; // hours to one decimal place
  }

  private buildSession(slot: BookedSessionSlotViewDto, start: Date, end: Date, durationMinutes: number): CalendarSession {
    const minutesFromMidnight = start.getHours() * 60 + start.getMinutes();
    const top = minutesFromMidnight / 60 * HOUR_HEIGHT;

    // a session running past midnight is cut off at the bottom of its start day
    const visibleMinutes = Math.min(durationMinutes, 24 * 60 - minutesFromMidnight);
    const height = Math.max(visibleMinutes / 60 * HOUR_HEIGHT - 2, MIN_BLOCK_HEIGHT);

    return {
      slot: slot,
      start: start,
      end: end,
      durationMinutes: durationMinutes,
      top: top,
      height: height,
      left: 0,
      width: 100,
      colours: this.getDurationColours(durationMinutes)
    };
  }

  // Overlapping sessions sit side by side. Sessions that overlap (directly or through a chain)
  // form a group; each one takes the first column that is free at its start time.
  private layOutOverlaps(daySessions: CalendarSession[]): void {
    daySessions.sort((a, b) => a.start.getTime() - b.start.getTime() || b.durationMinutes - a.durationMinutes);

    let group: CalendarSession[] = [];
    let groupEnd = 0;

    for (const session of daySessions) {
      if (group.length > 0 && session.start.getTime() >= groupEnd) {
        this.assignColumns(group);
        group = [];
      }
      group.push(session);
      groupEnd = Math.max(groupEnd, session.end.getTime());
    }

    if (group.length > 0) {
      this.assignColumns(group);
    }
  }

  private assignColumns(group: CalendarSession[]): void {
    const columnEnds: number[] = [];
    const columnOf = new Map<CalendarSession, number>();

    for (const session of group) {
      let column = columnEnds.findIndex(columnEnd => columnEnd <= session.start.getTime());
      if (column === -1) {
        column = columnEnds.length;
        columnEnds.push(0);
      }
      columnEnds[column] = session.end.getTime();
      columnOf.set(session, column);
    }

    const width = 100 / columnEnds.length;
    for (const session of group) {
      session.width = width;
      session.left = columnOf.get(session)! * width;
    }
  }

  // Nearest standard length: under 45 min = 30 min colour, 45-74 = 60 min colour, 75+ = 90 min colour
  private getDurationColours(durationMinutes: number): DurationColours {
    if (durationMinutes < 45) {
      return { block: 'bg-blue-50 border-blue-200 border-l-action-royal', name: 'text-blue-950', time: 'text-action-royal' };
    }
    if (durationMinutes < 75) {
      return { block: 'bg-emerald-50 border-emerald-200 border-l-success-emerald', name: 'text-emerald-950', time: 'text-emerald-700' };
    }
    return { block: 'bg-slate-100 border-slate-300 border-l-slate-navy', name: 'text-slate-900', time: 'text-slate-600' };
  }

  // ---- Date helpers ----

  private buildDays(): CalendarDay[] {
    const today = new Date();
    const days: CalendarDay[] = [];

    for (let i = 0; i < 7; i++) {
      const date = this.addDays(this.weekStart, i);
      days.push({
        date: date,
        isToday: this.isSameDay(date, today),
        isWeekend: i >= 5
      });
    }
    return days;
  }

  // Which column (0 = Monday) a session starts in, or outside 0-6 if it isn't in this week
  private getDayIndex(start: Date): number {
    const startDay = new Date(start.getFullYear(), start.getMonth(), start.getDate());
    return Math.round((startDay.getTime() - this.weekStart.getTime()) / (24 * 60 * 60 * 1000));
  }

  private getMondayOf(date: Date): Date {
    const daysSinceMonday = (date.getDay() + 6) % 7; // getDay() is 0 for Sunday
    return new Date(date.getFullYear(), date.getMonth(), date.getDate() - daysSinceMonday);
  }

  // Built from year/month/day so clock changes never shift the result off midnight
  private addDays(date: Date, days: number): Date {
    return new Date(date.getFullYear(), date.getMonth(), date.getDate() + days);
  }

  private isSameDay(a: Date, b: Date): boolean {
    return a.getFullYear() === b.getFullYear() && a.getMonth() === b.getMonth() && a.getDate() === b.getDate();
  }
}
