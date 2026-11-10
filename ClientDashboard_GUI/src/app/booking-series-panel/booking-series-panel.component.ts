import { Component, EventEmitter, Input, OnInit, Output, inject } from '@angular/core';
import { FormsModule } from '@angular/forms';
import { ButtonModule } from 'primeng/button';
import { DatePicker } from 'primeng/datepicker';
import { Dialog } from 'primeng/dialog';
import { InputTextModule } from 'primeng/inputtext';
import { TextareaModule } from 'primeng/textarea';
import { IconFieldModule } from 'primeng/iconfield';
import { InputIconModule } from 'primeng/inputicon';
import { BookingService } from '../services/booking.service';
import { ToastService } from '../services/toast.service';
import { BookingSeriesDto } from '../models/dtos/booking-series-dto';
import { BookingSeriesUpdateDto } from '../models/dtos/booking-series-update-dto';
import { SessionRecurrence } from '../enums/session-recurrence';
import { parseDateOnly, parseTimeOnly, toDateOnlyString, toTimeOnlyString } from '../helpers/date-only-helpers';

interface ClientSeriesGroup {
  clientName: string;
  series: BookingSeriesDto[];
}

// Lives inside the calendar's Booking Series drawer: lists the trainer's series grouped by client,
// with edit and delete dialogs on top. Series times are the trainer's local schedule times.
@Component({
  selector: 'app-booking-series-panel',
  imports: [FormsModule, ButtonModule, DatePicker, Dialog, InputTextModule, TextareaModule, IconFieldModule, InputIconModule],
  templateUrl: './booking-series-panel.component.html',
  styleUrl: './booking-series-panel.component.css'
})
export class BookingSeriesPanelComponent implements OnInit {
  // set by the calendar's "Manage series" link to open that series' edit dialog straight away
  @Input() initialEditSeriesId: number | null = null;

  // tells the calendar to reload the week after an edit or delete
  @Output() seriesChanged = new EventEmitter<void>();
  @Output() newSeriesRequested = new EventEmitter<void>();

  bookingService = inject(BookingService);
  toastService = inject(ToastService);

  allSeries: BookingSeriesDto[] = [];
  searchText = '';
  isLoading = false;
  isSaving = false;

  selectedSeries: BookingSeriesDto | null = null;
  editDialogVisible = false;
  deleteDialogVisible = false;

  // edit form - client and recurrence are read-only on an existing series
  formTitle = '';
  formDescription = '';
  formStartDate: Date | null = null;
  formStartTime: Date | null = null;
  formEndTime: Date | null = null;

  ngOnInit(): void {
    this.loadSeries();
  }

  loadSeries(): void {
    this.isLoading = true;
    this.bookingService.getTrainerBookingSeries().subscribe({
      next: (response) => {
        this.allSeries = response.data ?? [];
        this.isLoading = false;
        this.openInitialEditSeries();
      },
      error: (response) => {
        this.toastService.showError('Error Loading Booking Series', response.error?.message ?? 'Booking series could not be loaded');
        this.isLoading = false;
      }
    });
  }

  // ---- List ----

  // Series grouped under each client, filtered by the search box (client name or title).
  // The API already sorts by client then title, so groups come out in order.
  get groupedSeries(): ClientSeriesGroup[] {
    const search = this.searchText.trim().toLowerCase();
    const groups: ClientSeriesGroup[] = [];

    for (const series of this.allSeries) {
      const matchesSearch = search === ''
        || series.clientName.toLowerCase().includes(search)
        || series.title.toLowerCase().includes(search);
      if (!matchesSearch) {
        continue;
      }

      let group = groups.find(g => g.clientName === series.clientName);
      if (!group) {
        group = { clientName: series.clientName, series: [] };
        groups.push(group);
      }
      group.series.push(series);
    }
    return groups;
  }

  // e.g. "Every Tuesday at 09:00 · 60 min" or "Every other Tuesday at 09:00 · 45 min"
  formatSchedule(series: BookingSeriesDto): string {
    const weekday = parseDateOnly(series.startDate).toLocaleDateString('en-GB', { weekday: 'long' });
    const every = series.recurrence === SessionRecurrence.BiWeekly ? 'Every other' : 'Every';
    return `${every} ${weekday} at ${series.startTime.substring(0, 5)} · ${series.duration} min`;
  }

  formatRecurrence(series: BookingSeriesDto): string {
    return series.recurrence === SessionRecurrence.BiWeekly ? 'Every 2 weeks' : 'Weekly';
  }

  requestNewSeries(): void {
    this.newSeriesRequested.emit();
  }

  // ---- Edit ----

  openEdit(series: BookingSeriesDto): void {
    this.selectedSeries = series;
    this.formTitle = series.title;
    this.formDescription = series.description ?? '';
    this.formStartDate = parseDateOnly(series.startDate);
    this.formStartTime = parseTimeOnly(series.startTime);
    this.formEndTime = new Date(this.formStartTime.getTime() + series.duration * 60000);
    this.editDialogVisible = true;
  }

  saveEdit(): void {
    const updatedSeries = this.buildUpdateFromForm();
    if (!updatedSeries || !this.selectedSeries) {
      return;
    }

    this.isSaving = true;
    this.bookingService.updateBookingSeries(this.selectedSeries.id, updatedSeries).subscribe({
      next: (response) => {
        this.toastService.showSuccess('Booking Series Updated', response.message);
        this.editDialogVisible = false;
        this.isSaving = false;
        this.loadSeries();
        this.seriesChanged.emit();
      },
      error: (response) => {
        this.toastService.showError('Error Updating Booking Series', response.error?.message ?? 'The booking series could not be updated');
        this.isSaving = false;
      }
    });
  }

  // ---- Delete ----

  openDelete(series: BookingSeriesDto): void {
    this.selectedSeries = series;
    this.deleteDialogVisible = true;
  }

  deleteSeries(): void {
    if (!this.selectedSeries) {
      return;
    }

    this.isSaving = true;
    this.bookingService.deleteBookingSeries(this.selectedSeries.id).subscribe({
      next: (response) => {
        this.toastService.showSuccess('Booking Series Deleted', response.message);
        this.deleteDialogVisible = false;
        this.isSaving = false;
        this.loadSeries();
        this.seriesChanged.emit();
      },
      error: (response) => {
        this.toastService.showError('Error Deleting Booking Series', response.error?.message ?? 'The booking series could not be deleted');
        this.isSaving = false;
      }
    });
  }

  // ---- Helpers ----

  private openInitialEditSeries(): void {
    if (this.initialEditSeriesId === null) {
      return;
    }

    const series = this.allSeries.find(s => s.id === this.initialEditSeriesId);
    this.initialEditSeriesId = null; // only once, not on every reload

    if (series) {
      this.openEdit(series);
    } else {
      this.toastService.showError('Booking Series Not Found', 'This session\'s series no longer exists');
    }
  }

  // Returns null (after showing a toast) when something is missing or the times are the wrong way round
  private buildUpdateFromForm(): BookingSeriesUpdateDto | null {
    if (!this.formTitle || this.formTitle.trim() === '') {
      this.toastService.showError('Validation Error', 'A title is required');
      return null;
    }
    if (!this.formStartDate || !this.formStartTime || !this.formEndTime) {
      this.toastService.showError('Validation Error', 'A start date, start time and end time are required');
      return null;
    }

    const startMinutes = this.formStartTime.getHours() * 60 + this.formStartTime.getMinutes();
    const endMinutes = this.formEndTime.getHours() * 60 + this.formEndTime.getMinutes();

    if (endMinutes <= startMinutes) {
      this.toastService.showError('Validation Error', 'Session end time must be after the start time');
      return null;
    }

    return {
      title: this.formTitle.trim(),
      description: this.formDescription.trim() === '' ? undefined : this.formDescription.trim(),
      duration: endMinutes - startMinutes,
      startDate: toDateOnlyString(this.formStartDate),
      startTime: toTimeOnlyString(this.formStartTime)
    };
  }
}
