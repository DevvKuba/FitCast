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
    workoutDuration: number = 60;
    exerciseCount: number = 8;
    currentUserId: number = 0;

    clients: { id: number, name: string }[] = [];
    clonedWorkouts: { [s: string]: Workout } = {};

    deleteWorkoutId: number = 0;
    deleteWorkoutTitle: string = "";

    private workoutService = inject(WorkoutService);
    private accountService = inject(AccountService);
    private clientService = inject(ClientService);
    private notificationService = inject(NotificationService);
    private toastService = inject(ToastService);

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
    }

    onRowEditSave(newWorkout: Workout) {
        if (newWorkout.workoutTitle.length !== 0 && newWorkout.sessionDate !== null
            && newWorkout.exerciseCount > 0 && newWorkout.id !== null) {
            delete this.clonedWorkouts[newWorkout.id as number];

            const updatedInfo = {
                id: newWorkout.id,
                workoutTitle: newWorkout.workoutTitle,
                sessionDate: newWorkout.sessionDate,
                exerciseCount: newWorkout.exerciseCount,
                duration: newWorkout.duration
            }
            this.workoutService.updateWorkout(updatedInfo).subscribe({
                next: () => {
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

    addNewWorkout(selectedClient: { id: number, name: string }, workoutTitle: string, sessionDate: Date | undefined, exerciseCount: number | null, duration: number | null) {
        if (!this.validateWorkoutAddFields(selectedClient, workoutTitle, sessionDate, exerciseCount, duration)) {
            return;
        }

        const newWorkout = {
            workoutTitle: workoutTitle,
            clientName: selectedClient.name,
            clientId: selectedClient.id,
            sessionDate: this.formatDateForApi(sessionDate),
            exerciseCount: exerciseCount ?? 0,
            duration: duration ?? 0
        }

        this.workoutService.addWorkout(newWorkout).subscribe({
            next: (response) => {
                this.addDialogVisible = false;
                this.toastService.showSuccess('Successfully added workout', response.message);
                this.displayWorkouts();
                this.notificationService.refreshUnreadCount(this.currentUserId);
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

    validateWorkoutAddFields(selectedClient: { id: number, name: string }, workoutTitle: string, sessionDate: Date | undefined, exerciseCount: number | null, duration: number | null): boolean {
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

        if (!exerciseCount || exerciseCount === null || exerciseCount <= 0) {
            this.toastService.showError('Error adding workout', 'Must provide a valid exercise count');
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
}
