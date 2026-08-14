import { ComponentFixture, TestBed } from '@angular/core/testing';
import { of } from 'rxjs';
import { AccountService } from '../services/account.service';
import { ClientService } from '../services/client.service';
import { NotificationService } from '../services/notification.service';
import { ToastService } from '../services/toast.service';
import { WorkoutService } from '../services/workout.service';
import { Workout } from '../models/workout';

import { ClientWorkouts } from './client-workouts.component';

describe('ClientWorkouts', () => {
  let component: ClientWorkouts;
  let fixture: ComponentFixture<ClientWorkouts>;
  let workoutServiceSpy: jasmine.SpyObj<WorkoutService>;

  const workoutRow: Workout = {
    id: 1,
    clientId: 8,
    clientName: 'Alex',
    workoutTitle: 'Push Day',
    sessionDate: '2026-04-14',
    currentBlockSession: 2,
    totalBlockSessions: 8,
    exerciseCount: 7,
    duration: 60
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
        { provide: NotificationService, useValue: jasmine.createSpyObj<NotificationService>('NotificationService', ['refreshUnreadCount']) }
      ]
    })
    .compileComponents();

    const clientService = TestBed.inject(ClientService) as jasmine.SpyObj<ClientService>;
    clientService.gatherClientNames.and.returnValue(of([]));

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
      component.onRowEditSave({ ...workoutRow });

      expect(workoutServiceSpy.updateWorkout).toHaveBeenCalledWith({
        id: workoutRow.id,
        workoutTitle: workoutRow.workoutTitle,
        sessionDate: workoutRow.sessionDate,
        exerciseCount: workoutRow.exerciseCount,
        duration: workoutRow.duration
      });
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
