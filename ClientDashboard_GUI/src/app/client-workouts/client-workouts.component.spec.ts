import { ComponentFixture, TestBed } from '@angular/core/testing';
import { of } from 'rxjs';
import { AccountService } from '../services/account.service';
import { ClientService } from '../services/client.service';
import { NotificationService } from '../services/notification.service';
import { ToastService } from '../services/toast.service';
import { WorkoutService } from '../services/workout.service';
import { BookingService } from '../services/booking.service';
import { Workout } from '../models/workout';
import { toDateOnlyString } from '../helpers/date-only-helpers';

import { ClientWorkouts } from './client-workouts.component';

describe('ClientWorkouts', () => {
  let component: ClientWorkouts;
  let fixture: ComponentFixture<ClientWorkouts>;
  let workoutServiceSpy: jasmine.SpyObj<WorkoutService>;
  let toastServiceSpy: jasmine.SpyObj<ToastService>;

  const workoutRow: Workout = {
    id: 1,
    clientId: 8,
    clientName: 'Alex',
    workoutTitle: 'Push Day',
    sessionDate: '2026-04-14',
    currentBlockSession: 2,
    totalBlockSessions: 8,
    duration: 60,
    startDateTime: '2026-04-14T09:00:00.000Z',
    endDateTime: '2026-04-14T10:00:00.000Z'
  };

  beforeEach(async () => {
    workoutServiceSpy = jasmine.createSpyObj<WorkoutService>('WorkoutService', [
      'retrieveTrainerClientWorkouts',
      'updateWorkout',
      'deleteWorkout',
      'addWorkout'
    ]);
    workoutServiceSpy.retrieveTrainerClientWorkouts.and.returnValue(
      of({ success: true, message: 'ok', data: [workoutRow] })
    );
    workoutServiceSpy.updateWorkout.and.returnValue(of({ success: true, message: 'updated' }));
    workoutServiceSpy.deleteWorkout.and.returnValue(of({ success: true, message: 'deleted' }));
    workoutServiceSpy.addWorkout.and.returnValue(of({ success: true, message: 'added' }));

    await TestBed.configureTestingModule({
      imports: [ClientWorkouts],
      providers: [
        { provide: WorkoutService, useValue: workoutServiceSpy },
        { provide: AccountService, useValue: { currentUser: jasmine.createSpy('currentUser').and.returnValue({ id: 13 }) } },
        { provide: ClientService, useValue: jasmine.createSpyObj<ClientService>('ClientService', ['gatherClientNames']) },
        { provide: ToastService, useValue: jasmine.createSpyObj<ToastService>('ToastService', ['showSuccess', 'showError', 'showNeutral']) },
        { provide: NotificationService, useValue: jasmine.createSpyObj<NotificationService>('NotificationService', ['refreshUnreadCount']) },
        { provide: BookingService, useValue: jasmine.createSpyObj<BookingService>('BookingService', ['getClientSessionsOnDate']) }
      ]
    })
    .compileComponents();

    const clientService = TestBed.inject(ClientService) as jasmine.SpyObj<ClientService>;
    clientService.gatherClientNames.and.returnValue(of([]));
    toastServiceSpy = TestBed.inject(ToastService) as jasmine.SpyObj<ToastService>;

    fixture = TestBed.createComponent(ClientWorkouts);
    component = fixture.componentInstance;
    fixture.detectChanges();
  });

  it('should create', () => {
    expect(component).toBeTruthy();
  });

  describe('table data behavior', () => {
    it('loads workouts on init', () => {
      expect(workoutServiceSpy.retrieveTrainerClientWorkouts).toHaveBeenCalled();
      expect(component.workouts?.length).toBe(1);
      expect(component.workouts?.[0].workoutTitle).toBe('Push Day');
    });

    it('clear() resets table filters and reloads workouts', () => {
      const tableClearSpy = jasmine.createSpy('clear');

      component.clear({ clear: tableClearSpy } as any);

      expect(tableClearSpy).toHaveBeenCalled();
      expect(workoutServiceSpy.retrieveTrainerClientWorkouts).toHaveBeenCalledTimes(2);
    });
  });

  describe('table value formatting', () => {
    it('formatDateForApi transforms Date into yyyy/MM/dd', () => {
      const formatted = component.formatDateForApi(new Date('2026-04-05T00:00:00.000Z'));

      expect(formatted).toBe('2026/04/05');
    });
  });

  describe('row edit behavior', () => {
    it('onRowEditInit clones the row before it becomes editable', () => {
      component.onRowEditInit(workoutRow);

      expect(component.clonedWorkouts[workoutRow.id]).toEqual(workoutRow);
    });

    it('onRowEditCancel restores the cloned row and discards the clone', () => {
      const original = { ...workoutRow };
      const edited = { ...workoutRow, workoutTitle: 'Changed Title' };
      component.workouts = [edited];
      component.clonedWorkouts[workoutRow.id] = original;

      component.onRowEditCancel(edited, 0);

      expect(component.workouts[0].workoutTitle).toBe('Push Day');
      expect(component.clonedWorkouts[workoutRow.id]).toBeUndefined();
    });

    it('onRowEditSave sends the editable fields only, not the read-only session counts', () => {
      const row = { ...workoutRow };
      component.onRowEditInit(row);
      component.onRowEditSave(row);

      expect(workoutServiceSpy.updateWorkout).toHaveBeenCalledWith({
        id: workoutRow.id,
        workoutTitle: workoutRow.workoutTitle,
        sessionDate: workoutRow.sessionDate,
        duration: workoutRow.duration,
        startDateTime: workoutRow.startDateTime!
      });
    });

    it('moving the start keeps the duration by moving the end', () => {
      const row = { ...workoutRow };
      component.onRowEditInit(row);
      const newStart = new Date(2026, 3, 14, 7, 30);

      component.onEditStartTimeChange(row, newStart);

      expect(component.editedSessionTimes[row.id].end).toEqual(new Date(2026, 3, 14, 8, 30));
    });

    it('moving the end updates the duration', () => {
      const row = { ...workoutRow };
      component.onRowEditInit(row);
      const start = component.editedSessionTimes[row.id].start!;
      const end = new Date(start.getTime() + 45 * 60000);

      component.onEditEndTimeChange(row, end);

      expect(row.duration).toBe(45);
    });

    it('starts workouts logged before session times with empty pickers', () => {
      const row = { ...workoutRow, startDateTime: undefined, endDateTime: undefined };

      component.onRowEditInit(row);

      expect(component.editedSessionTimes[row.id]).toEqual({ start: null, end: null });
      expect(component.formatSessionTimeRange(row)).toBe('–');
    });
  });

  describe('edit window lock', () => {
    it('locks workouts from a closed month', () => {
      const closed = new Date();
      closed.setMonth(closed.getMonth() - 3);

      expect(component.isLocked({ ...workoutRow, sessionDate: toDateOnlyString(closed) })).toBeTrue();
    });

    it('keeps today\'s workouts editable', () => {
      expect(component.isLocked({ ...workoutRow, sessionDate: toDateOnlyString(new Date()) })).toBeFalse();
    });
  });

  describe('add workout validation', () => {
    it('rejects an end time that is not after the start', () => {
      const start = new Date(2026, 3, 14, 10, 0);
      const end = new Date(2026, 3, 14, 9, 0);

      component.addNewWorkout({ id: 8, name: 'Alex' }, 'Push Day', new Date(), start, end, 60);

      expect(toastServiceSpy.showError).toHaveBeenCalledWith('Error adding workout', 'End time must be after the start time');
      expect(workoutServiceSpy.addWorkout).not.toHaveBeenCalled();
    });
  });

  describe('progress + avatar helpers', () => {
    it('getProgressPercentage rounds the current/total session ratio', () => {
      expect(component.getProgressPercentage(workoutRow)).toBe(25);
    });

    it('getProgressPercentage returns 0 when totalBlockSessions is missing', () => {
      expect(component.getProgressPercentage({ ...workoutRow, totalBlockSessions: undefined })).toBe(0);
    });

    it('getInitials uses the first letter of the client name', () => {
      expect(component.getInitials('Alex')).toBe('A');
    });

    it('getAvatarColorClass assigns the same colour to the same starting letter', () => {
      expect(component.getAvatarColorClass('Amanda')).toBe(component.getAvatarColorClass('Aaron'));
    });

    it('getAvatarColorClass spreads early vs late alphabet names across different colours', () => {
      expect(component.getAvatarColorClass('Amanda')).not.toBe(component.getAvatarColorClass('Zoe'));
    });
  });
});
