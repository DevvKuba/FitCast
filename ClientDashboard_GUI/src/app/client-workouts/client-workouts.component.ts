import { Component, inject, OnInit } from '@angular/core';
import { Table, TableModule } from 'primeng/table';
import { CommonModule } from '@angular/common';
import { ButtonModule } from 'primeng/button';
import { Workout } from '../models/workout';
import { WorkoutService } from '../services/workout.service';
import { SpinnerComponent } from "../spinner/spinner.component";
import { AccountService } from '../services/account.service';
import { Toast } from 'primeng/toast';
import { InputTextModule } from 'primeng/inputtext';
import { Dialog } from 'primeng/dialog';
import { FormsModule } from '@angular/forms';
import { AutoCompleteModule } from 'primeng/autocomplete';
import { ClientService } from '../services/client.service';
import { DatePicker } from 'primeng/datepicker';
import { InputNumberModule } from 'primeng/inputnumber';
import { MessageService } from 'primeng/api';
import { ToastService } from '../services/toast.service';
import { NotificationService } from '../services/notification.service';
import { IconFieldModule } from 'primeng/iconfield';
import { InputIconModule } from 'primeng/inputicon';
import { BookingService } from '../services/booking.service';
import { BookedSessionSlotViewDto } from '../models/dtos/booked-session-slot-view-dto';
import { WorkoutAddDto } from '../models/dtos/workout-add-dto';
import { parseDateOnly, toDateOnlyString } from '../helpers/date-only-helpers';
import { addMinutes, atClockTime, combineDateAndTime, minutesBetweenClockTimes } from '../helpers/session-time-helpers';

@Component({
  selector: 'app-client-workouts',
  imports: [TableModule, CommonModule, ButtonModule, SpinnerComponent, Toast, InputTextModule,
     Dialog, FormsModule, AutoCompleteModule, DatePicker, InputNumberModule, IconFieldModule, InputIconModule],
  templateUrl: './client-workouts.component.html',
  providers: [MessageService],
  styleUrl: './client-workouts.component.css'
})
export class ClientWorkouts implements OnInit {
    workouts: Workout[] | null = null;
    addDialogVisible: boolean = false;
    deleteDialogVisible: boolean = false;

    selectedClient: { id: number, name: string } = { id: 0, name: "" };
    workoutTitle: string = "";
    sessionDate: Date = new Date();
    sessionStartTime: Date = atClockTime(new Date(), 12, 0);
    sessionEndTime: Date = atClockTime(new Date(), 13, 0);
    workoutDuration: number = 60;
    currentUserId: number = 0;

    clients: { id: number, name: string }[] = [];
    clonedWorkouts: { [s: string]: Workout } = {};
    editedSessionTimes: { [id: number]: { start: Date | null, end: Date | null } } = {};

    deleteWorkoutId: number = 0;
    deleteWorkoutTitle: string = "";

    bookedSessionConfirmVisible: boolean = false;
    bookedSessionsOnDate: BookedSessionSlotViewDto[] = [];
    pendingWorkout: WorkoutAddDto | null = null;

    private workoutService = inject(WorkoutService);
    private accountService = inject(AccountService);
    private clientService = inject(ClientService);
    private notificationService = inject(NotificationService);
    private toastService = inject(ToastService);
    private bookingService = inject(BookingService);

    ngOnInit() {
        this.currentUserId = this.accountService.currentUser()?.id ?? 0;
        this.displayWorkouts();
    }

    clear(table: Table) {
        table.clear();
        this.displayWorkouts();
    }

    onRowEditInit(workout: Workout) {
        this.clonedWorkouts[workout.id as number] = { ...workout };
        this.editedSessionTimes[workout.id] = this.getEditableSessionTimes(workout);
    }

    onRowEditSave(newWorkout: Workout) {
        const times = this.editedSessionTimes[newWorkout.id];
        delete this.editedSessionTimes[newWorkout.id];

        if (newWorkout.workoutTitle.length !== 0 && newWorkout.sessionDate && times && times.start && times.end
            && minutesBetweenClockTimes(times.start, times.end) > 0
            && newWorkout.duration > 0 && newWorkout.id !== null) {
            delete this.clonedWorkouts[newWorkout.id as number];

            const start = combineDateAndTime(parseDateOnly(newWorkout.sessionDate), times.start);
            const updatedInfo = {
                id: newWorkout.id,
                workoutTitle: newWorkout.workoutTitle,
                sessionDate: newWorkout.sessionDate,
                duration: newWorkout.duration,
                startDateTime: start.toISOString()
            }
            this.workoutService.updateWorkout(updatedInfo).subscribe({
                next: () => {
                    newWorkout.startDateTime = start.toISOString();
                    newWorkout.endDateTime = addMinutes(start, newWorkout.duration).toISOString();
                    this.toastService.showSuccess('Updated Correctly', `Successfully updated ${newWorkout.clientName}'s workout details`);
                },
                error: () => {
                    this.toastService.showError('Unsuccessful Update', `Workout: ${newWorkout.workoutTitle} not updated`);
                }
            })
        } else {
            this.toastService.showError('Update Unsuccessful', 'Ensure all fields are filled in correctly');
        }
    }

    onRowEditCancel(workout: Workout, index: number) {
        delete this.editedSessionTimes[workout.id];
        if (this.workouts && this.clonedWorkouts[workout.id as number]) {
            this.workouts[index] = this.clonedWorkouts[workout.id as number];
            delete this.clonedWorkouts[workout.id as number];
        }
    }

    onRowDelete(workoutId: number) {
        this.workoutService.deleteWorkout(workoutId).subscribe({
            next: (response) => {
                this.toastService.showSuccess('Success Deleting Workout', response.message);
                this.deleteDialogVisible = false;
                this.displayWorkouts();
            },
            error: (response) => {
                this.toastService.showError('Unsuccessful Workout Deletion', response.error.message);
            }
        })
    }

    // Before adding, checks whether the client already has a calendar session that day, which the nightly sync will log anyway
    addNewWorkout(selectedClient: { id: number, name: string }, workoutTitle: string, sessionDate: Date | undefined,
        startTime: Date | null, endTime: Date | null, duration: number | null) {
        if (!this.validateWorkoutAddFields(selectedClient, workoutTitle, sessionDate, startTime, endTime, duration)) {
            return;
        }

        const newWorkout: WorkoutAddDto = {
            workoutTitle: workoutTitle,
            clientName: selectedClient.name,
            clientId: selectedClient.id,
            sessionDate: this.formatDateForApi(sessionDate),
            duration: duration ?? 0,
            startDateTime: combineDateAndTime(sessionDate!, startTime!).toISOString()
        }

        this.bookingService.getClientSessionsOnDate(selectedClient.id, toDateOnlyString(sessionDate!)).subscribe({
            next: (response) => {
                const bookedSessions = response.data ?? [];

                if (bookedSessions.length === 0) {
                    this.submitNewWorkout(newWorkout);
                    return;
                }

                this.pendingWorkout = newWorkout;
                this.bookedSessionsOnDate = bookedSessions;
                this.bookedSessionConfirmVisible = true;
            },
            error: (response) => {
                this.toastService.showError('Workout not added', response.error?.message ?? 'Could not check the client\'s booked sessions');
            }
        });
    }

    confirmAddWorkoutDespiteBooking() {
        this.bookedSessionConfirmVisible = false;
        if (this.pendingWorkout) {
            this.submitNewWorkout(this.pendingWorkout);
        }
    }

    submitNewWorkout(newWorkout: WorkoutAddDto) {
        this.workoutService.addWorkout(newWorkout).subscribe({
            next: (response) => {
                this.addDialogVisible = false;
                this.toastService.showSuccess('Successfully added workout', response.message);
                this.displayWorkouts();
                this.notificationService.refreshUnreadCount();
            },
            error: (response) => {
                this.toastService.showError('Workout not added', response.error.message);
            }
        })
    }

    displayWorkouts() {
        this.workoutService.retrieveTrainerClientWorkouts().subscribe({
            next: (response) => {
                this.workouts = response.data ?? [];
            },
            error: (response) => {
                this.toastService.showError('Error Loading Workouts', response.error.message);
            }
        });
    }

    showDialogForAdd() {
        this.gatherClientNames();
        this.addDialogVisible = true;
    }

    showDialogForDelete(workoutId: number, workoutTitle: string) {
        this.deleteDialogVisible = true;
        this.deleteWorkoutId = workoutId;
        this.deleteWorkoutTitle = workoutTitle;
    }

    // Start and duration move the end time; the end time sets the duration
    onAddStartTimeChange(start: Date | null) {
        if (start && this.workoutDuration > 0) {
            this.sessionEndTime = addMinutes(start, this.workoutDuration);
        }
    }

    onAddDurationChange(duration: number | null) {
        if (this.sessionStartTime && duration && duration > 0) {
            this.sessionEndTime = addMinutes(this.sessionStartTime, duration);
        }
    }

    onAddEndTimeChange(end: Date | null) {
        if (!end || !this.sessionStartTime) return;

        const minutes = minutesBetweenClockTimes(this.sessionStartTime, end);
        if (minutes > 0) {
            this.workoutDuration = minutes;
        }
    }

    onEditStartTimeChange(workout: Workout, start: Date | null) {
        if (start && workout.duration > 0) {
            this.editedSessionTimes[workout.id].end = addMinutes(start, workout.duration);
        }
    }

    onEditDurationChange(workout: Workout, duration: number | null) {
        const times = this.editedSessionTimes[workout.id];
        if (times && times.start && duration && duration > 0) {
            times.end = addMinutes(times.start, duration);
        }
    }

    onEditEndTimeChange(workout: Workout, end: Date | null) {
        const times = this.editedSessionTimes[workout.id];
        if (!end || !times || !times.start) return;

        const minutes = minutesBetweenClockTimes(times.start, end);
        if (minutes > 0) {
            workout.duration = minutes;
        }
    }

    formatSessionTime(utcDateTime: string): string {
        return new Date(utcDateTime).toLocaleTimeString('en-GB', { hour: '2-digit', minute: '2-digit' });
    }

    formatSessionTimeRange(workout: Workout): string {
        if (!workout.startDateTime || !workout.endDateTime) return '–';
        return `${this.formatSessionTime(workout.startDateTime)} – ${this.formatSessionTime(workout.endDateTime)}`;
    }

    formatDateForApi(date: Date | undefined): string {
        if (!date) return '';

        const day = date.getDate().toString().padStart(2, '0');
        const month = (date.getMonth() + 1).toString().padStart(2, '0');
        const year = date.getFullYear();

        return `${year}/${month}/${day}`;
    }


    gatherClientNames() {
        this.clientService.gatherClientNames(this.currentUserId).subscribe({
            next: (response) => {
                this.clients = response
            }
        });
    }

    validateWorkoutAddFields(selectedClient: { id: number, name: string }, workoutTitle: string, sessionDate: Date | undefined,
        startTime: Date | null, endTime: Date | null, duration: number | null): boolean {
        if (!selectedClient.id || !selectedClient.name || selectedClient.name.trim() === '') {
            this.toastService.showError('Error adding workout', 'Must select a valid client');
            return false;
        }

        if (!workoutTitle || workoutTitle.trim() === '') {
            this.toastService.showError('Error adding workout', 'Must provide a workout title');
            return false;
        }

        if (!sessionDate) {
            this.toastService.showError('Error adding workout', 'Must provide a session date');
            return false;
        }

        if (!startTime || !endTime) {
            this.toastService.showError('Error adding workout', 'Must provide a start and end time');
            return false;
        }

        if (minutesBetweenClockTimes(startTime, endTime) <= 0) {
            this.toastService.showError('Error adding workout', 'End time must be after the start time');
            return false;
        }

        if (!duration || duration === null || duration <= 0) {
            this.toastService.showError('Error adding workout', 'Must provide a valid duration');
            return false;
        }

        return true;
    }

    getProgressPercentage(workout: Workout): number {
        if (!workout.totalBlockSessions) return 0;
        return Math.round(((workout.currentBlockSession ?? 0) / workout.totalBlockSessions) * 100);
    }

    // Six evenly-spread, complementary pastel pairs - same palette/bucketing as the client
    // roster's avatars so colours stay consistent app-wide.
    private readonly avatarColorPalette: string[] = [
        'bg-primary-fixed text-primary',
        'bg-secondary-fixed text-secondary',
        'bg-violet-100 text-violet-700',
        'bg-amber-100 text-amber-700',
        'bg-rose-100 text-rose-700',
        'bg-cyan-100 text-cyan-700'
    ];

    getAvatarColorClass(clientName: string): string {
        const letter = (clientName?.charAt(0) ?? 'A').toUpperCase();
        const letterIndex = Math.max(0, letter.charCodeAt(0) - 'A'.charCodeAt(0));
        const bucketSize = 26 / this.avatarColorPalette.length;
        const index = Math.min(Math.floor(letterIndex / bucketSize), this.avatarColorPalette.length - 1);
        return this.avatarColorPalette[index];
    }

    getInitials(clientName: string): string {
        return (clientName?.charAt(0) ?? '').toUpperCase();
    }

    // Workouts logged before session times existed start with empty pickers for the trainer to fill in
    private getEditableSessionTimes(workout: Workout): { start: Date | null, end: Date | null } {
        if (!workout.startDateTime) {
            return { start: null, end: null };
        }

        const start = new Date(workout.startDateTime);
        const end = workout.endDateTime ? new Date(workout.endDateTime) : addMinutes(start, workout.duration);
        return { start: start, end: end };
    }
}
