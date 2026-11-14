import { AfterViewInit, Component, ElementRef, OnDestroy, OnInit, ViewChild, inject } from '@angular/core';
import { FormsModule } from '@angular/forms';
import { ButtonModule } from 'primeng/button';
import { DatePicker } from 'primeng/datepicker';
import { Dialog } from 'primeng/dialog';
import { InputTextModule } from 'primeng/inputtext';
import { SelectModule } from 'primeng/select';
import { TextareaModule } from 'primeng/textarea';
import { SelectButtonModule } from 'primeng/selectbutton';
import { DrawerModule } from 'primeng/drawer';
import { BookingSeriesPanelComponent } from '../booking-series-panel/booking-series-panel.component';
import { SessionRecurrence } from '../enums/session-recurrence';
import { BookingSeriesCreateDto } from '../models/dtos/booking-series-create-dto';
import { toDateOnlyString, toTimeOnlyString } from '../helpers/date-only-helpers';
import { BookingService } from '../services/booking.service';
import { TrainerService } from '../services/trainer.service';
import { ToastService } from '../services/toast.service';
import { ClientService } from '../services/client.service';
import { AccountService } from '../services/account.service';
import { BookedSessionSlotViewDto } from '../models/dtos/booked-session-slot-view-dto';
import { BookedSessionSlotDto } from '../models/dtos/booked-session-slot-dto';

// Grid measurements - one hour row is 60px tall: a 30 min session still fits a readable line of text,
// and the 600px minimum grid shows roughly 06:00 to 15:00 before scrolling
const HOUR_HEIGHT = 60;
const HOURS = Array.from({ length: 24 }, (_, hour) => hour);
const FIRST_VISIBLE_HOUR = 6; // the grid opens scrolled to 06:00
const MIN_BLOCK_HEIGHT = 18;

// Clicking an empty slot snaps to the half hour and prefills a one hour session
const CLICK_SNAP_MINUTES = 30;
const DEFAULT_SESSION_MINUTES = 60;

type SessionDialogMode = 'create' | 'edit';
type SessionKind = 'one-off' | 'recurring';

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
  imports: [FormsModule, ButtonModule, DatePicker, Dialog, InputTextModule, SelectModule, TextareaModule,
    SelectButtonModule, DrawerModule, BookingSeriesPanelComponent],
  templateUrl: './calendar.component.html',
  styleUrl: './calendar.component.css'
})
export class CalendarComponent implements OnInit, AfterViewInit, OnDestroy {
  @ViewChild('gridScroll') gridScroll!: ElementRef<HTMLDivElement>;

  bookingService = inject(BookingService);
  trainerService = inject(TrainerService);
  toastService = inject(ToastService);
  clientService = inject(ClientService);
  accountService = inject(AccountService);

  hours = HOURS;
  hourHeight = HOUR_HEIGHT;

  // Times are shown in the device's time zone (browser), not the trainer's profile zone
  browserTimeZoneId = Intl.DateTimeFormat().resolvedOptions().timeZone;
  trainerTimeZoneId = '';

  weekStart: Date = this.getMondayOf(new Date());
  weekPickerVisible = false;
  days: CalendarDay[] = [];
  sessionsByDay: CalendarSession[][] = [];

  bookingsCount = 0;
  coachedHours = 0;

  now = new Date();
  private clockTimer?: ReturnType<typeof setInterval>;

  clients: { id: number, name: string }[] = [];

  // ---- Dialog state ----
  selectedSession: CalendarSession | null = null;
  detailsDialogVisible = false;
  deleteDialogVisible = false;
  sessionDialogVisible = false;
  sessionDialogMode: SessionDialogMode = 'create';
  isSaving = false;

  // booking series drawer - manageSeriesId opens a series' edit dialog as soon as the drawer loads
  seriesDrawerVisible = false;
  manageSeriesId: number | null = null;

  // one-off sessions are sent as a single slot, recurring ones create a booking series
  sessionKind: SessionKind = 'one-off';
  sessionKindOptions = [
    { label: 'One-off', value: 'one-off' },
    { label: 'Recurring', value: 'recurring' }
  ];
  formRecurrence: SessionRecurrence = SessionRecurrence.Weekly;
  recurrenceOptions = [
    { label: 'Weekly', value: SessionRecurrence.Weekly },
    { label: 'Every 2 weeks', value: SessionRecurrence.BiWeekly }
  ];

  // new / edit session form - date and times are in the browser's zone
  formClientId: number | null = null;
  formTitle = '';
  formDescription = '';
  formDate: Date | null = null;
  formStartTime: Date | null = null;
  formEndTime: Date | null = null;

  ngOnInit(): void {
    this.loadWeek();
    this.retrieveTrainerTimeZone();
    this.retrieveClients();

    // keeps the current-time line moving
    this.clockTimer = setInterval(() => this.now = new Date(), 60 * 1000);
  }

  ngAfterViewInit(): void {
    // run after the first layout pass, otherwise the scroll area has no height yet and the scroll is ignored.
    // a little above 06:00 so its hour label isn't hidden under the day headers
    setTimeout(() => {
      this.gridScroll.nativeElement.scrollTop = FIRST_VISIBLE_HOUR * HOUR_HEIGHT - 12;
    });
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

  // Week picker: any day picked loads the Monday-Sunday week it falls in
  goToWeekOf(date: Date): void {
    this.weekPickerVisible = false;
    this.weekStart = this.getMondayOf(date);
    this.loadWeek();
  }

  // The week picker's day template passes { day, month (0-based), year }
  isInVisibleWeek(date: { day: number, month: number, year: number }): boolean {
    const day = new Date(date.year, date.month, date.day);
    return day >= this.weekStart && day < this.addDays(this.weekStart, 7);
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

  retrieveClients(): void {
    const trainerId = this.accountService.currentUser()?.id;
    if (!trainerId) {
      return;
    }

    this.clientService.gatherClientNames(trainerId).subscribe({
      next: (clients) => {
        this.clients = clients;
      }
    });
  }

  // ---- Session details & delete ----

  openSessionDetails(session: CalendarSession, event: MouseEvent): void {
    event.stopPropagation(); // don't also trigger the empty-slot click on the day column
    this.selectedSession = session;
    this.detailsDialogVisible = true;
  }

  openDeleteConfirmation(): void {
    this.detailsDialogVisible = false;
    this.deleteDialogVisible = true;
  }

  deleteSession(): void {
    if (!this.selectedSession) {
      return;
    }

    this.isSaving = true;
    this.bookingService.deleteBookedSessionSlot(this.selectedSession.slot.id).subscribe({
      next: (response) => {
        this.toastService.showSuccess('Session Deleted', response.message);
        this.deleteDialogVisible = false;
        this.isSaving = false;
        this.loadWeek();
      },
      error: (response) => {
        this.toastService.showError('Error Deleting Session', response.error?.message ?? 'The session could not be deleted');
        this.isSaving = false;
      }
    });
  }

  // ---- New & edit session ----

  // Header button: today, starting at the next half hour
  openNewSession(): void {
    const now = new Date();
    const minutesFromMidnight = Math.ceil((now.getHours() * 60 + now.getMinutes()) / CLICK_SNAP_MINUTES) * CLICK_SNAP_MINUTES;
    this.openCreateDialog(this.startOfDay(now), minutesFromMidnight);
  }

  // Empty-slot click: prefills the clicked half hour. Nothing is sent until the trainer presses Create.
  openNewSessionAt(day: CalendarDay, event: MouseEvent): void {
    const column = event.currentTarget as HTMLElement;
    const clickY = event.clientY - column.getBoundingClientRect().top;
    const clickedMinutes = clickY / HOUR_HEIGHT * 60;
    const minutesFromMidnight = Math.floor(clickedMinutes / CLICK_SNAP_MINUTES) * CLICK_SNAP_MINUTES;

    this.openCreateDialog(day.date, minutesFromMidnight);
  }

  openEditSession(): void {
    if (!this.selectedSession) {
      return;
    }

    const session = this.selectedSession;
    this.sessionDialogMode = 'edit';
    this.formClientId = session.slot.clientId;
    this.formTitle = session.slot.title;
    this.formDescription = session.slot.description ?? '';
    this.formDate = this.startOfDay(session.start);
    this.formStartTime = new Date(session.start);
    this.formEndTime = new Date(session.end);

    this.detailsDialogVisible = false;
    this.sessionDialogVisible = true;
  }

  // From the drawer's New button: the same dialog, switched to Recurring
  openNewSeries(): void {
    this.seriesDrawerVisible = false;
    this.openNewSession();
    this.sessionKind = 'recurring';
  }

  saveSession(): void {
    if (this.sessionDialogMode === 'create' && this.sessionKind === 'recurring') {
      this.saveSeries();
      return;
    }

    const sessionSlot = this.buildSessionSlotFromForm();
    if (!sessionSlot) {
      return;
    }

    this.isSaving = true;
    const request = this.sessionDialogMode === 'create'
      ? this.bookingService.addBookedSessionSlot(sessionSlot)
      : this.bookingService.updateBookedSessionSlot(this.selectedSession!.slot.id, sessionSlot);

    request.subscribe({
      next: (response) => {
        this.toastService.showSuccess(this.sessionDialogMode === 'create' ? 'Session Created' : 'Session Updated', response.message);
        this.sessionDialogVisible = false;
        this.isSaving = false;
        this.loadWeek();
      },
      error: (response) => {
        this.toastService.showError('Error Saving Session', response.error?.message ?? 'The session could not be saved');
        this.isSaving = false;
      }
    });
  }

  saveSeries(): void {
    const bookingSeries = this.buildSeriesFromForm();
    if (!bookingSeries) {
      return;
    }

    this.isSaving = true;
    this.bookingService.createBookingSeries(bookingSeries).subscribe({
      next: (response) => {
        this.toastService.showSuccess('Recurring Session Created', response.message);
        this.sessionDialogVisible = false;
        this.isSaving = false;
        this.loadWeek();
      },
      error: (response) => {
        this.toastService.showError('Error Creating Recurring Session', response.error?.message ?? 'The recurring session could not be created');
        this.isSaving = false;
      }
    });
  }

  // ---- Booking series drawer ----

  openSeriesDrawer(): void {
    this.manageSeriesId = null;
    this.seriesDrawerVisible = true;
  }

  // "Manage series" link in the session details: opens the drawer straight on that series' edit dialog
  openManageSeries(): void {
    if (!this.selectedSession?.slot.bookingSeriesId) {
      return;
    }

    this.manageSeriesId = this.selectedSession.slot.bookingSeriesId;
    this.detailsDialogVisible = false;
    this.seriesDrawerVisible = true;
  }

  // A series was edited or deleted in the drawer, so its sessions on the grid have changed
  onSeriesChanged(): void {
    this.loadWeek();
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

  // e.g. "Tuesday 10 November 2026"
  formatLongDate(date: Date): string {
    return date.toLocaleDateString('en-GB', { weekday: 'long', day: 'numeric', month: 'long', year: 'numeric' });
  }

  // ---- Session form helpers ----

  private openCreateDialog(date: Date, startMinutes: number): void {
    // keep the prefilled end on the same day - a session can't run past midnight from the form
    const endMinutes = Math.min(startMinutes + DEFAULT_SESSION_MINUTES, 24 * 60 - 1);

    this.sessionDialogMode = 'create';
    this.sessionKind = 'one-off';
    this.formRecurrence = SessionRecurrence.Weekly;
    this.selectedSession = null;
    this.formClientId = null;
    this.formTitle = '';
    this.formDescription = '';
    this.formDate = this.startOfDay(date);
    this.formStartTime = this.atMinutes(date, startMinutes);
    this.formEndTime = this.atMinutes(date, endMinutes);

    this.sessionDialogVisible = true;
  }

  // One-off session: the local date and times become the UTC body the API expects
  private buildSessionSlotFromForm(): BookedSessionSlotDto | null {
    const times = this.validateForm();
    if (!times) {
      return null;
    }

    return {
      clientId: this.formClientId!,
      title: this.formTitle.trim(),
      description: this.formDescription.trim() === '' ? undefined : this.formDescription.trim(),
      startDateTime: times.start.toISOString(), // toISOString always gives UTC with a trailing "Z"
      endDateTime: times.end.toISOString()
    };
  }

  // Recurring session: the series keeps the trainer's local date and clock time, and the duration
  // comes from the start and end times
  private buildSeriesFromForm(): BookingSeriesCreateDto | null {
    const times = this.validateForm();
    if (!times) {
      return null;
    }

    return {
      clientId: this.formClientId!,
      title: this.formTitle.trim(),
      description: this.formDescription.trim() === '' ? undefined : this.formDescription.trim(),
      duration: Math.round((times.end.getTime() - times.start.getTime()) / 60000),
      startDate: toDateOnlyString(times.start),
      startTime: toTimeOnlyString(times.start),
      recurrence: this.formRecurrence
    };
  }

  // Checks the fields both session kinds share. Returns the combined start and end,
  // or null (after showing a toast) when something is missing or the times are the wrong way round.
  private validateForm(): { start: Date, end: Date } | null {
    if (!this.formClientId) {
      this.toastService.showError('Validation Error', 'A client must be selected');
      return null;
    }
    if (!this.formTitle || this.formTitle.trim() === '') {
      this.toastService.showError('Validation Error', 'A title is required');
      return null;
    }
    if (!this.formDate || !this.formStartTime || !this.formEndTime) {
      this.toastService.showError('Validation Error', 'A date, start time and end time are required');
      return null;
    }

    const start = this.combineDateAndTime(this.formDate, this.formStartTime);
    const end = this.combineDateAndTime(this.formDate, this.formEndTime);

    if (end <= start) {
      this.toastService.showError('Validation Error', 'Session end time must be after the start time');
      return null;
    }

    return { start: start, end: end };
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

  private startOfDay(date: Date): Date {
    return new Date(date.getFullYear(), date.getMonth(), date.getDate());
  }

  private atMinutes(date: Date, minutesFromMidnight: number): Date {
    return new Date(date.getFullYear(), date.getMonth(), date.getDate(), Math.floor(minutesFromMidnight / 60), minutesFromMidnight % 60);
  }

  // The date picker gives the day and the time pickers give the clock time - this joins them in the browser's zone
  private combineDateAndTime(date: Date, time: Date): Date {
    return new Date(date.getFullYear(), date.getMonth(), date.getDate(), time.getHours(), time.getMinutes());
  }

  private isSameDay(a: Date, b: Date): boolean {
    return a.getFullYear() === b.getFullYear() && a.getMonth() === b.getMonth() && a.getDate() === b.getDate();
  }
}
