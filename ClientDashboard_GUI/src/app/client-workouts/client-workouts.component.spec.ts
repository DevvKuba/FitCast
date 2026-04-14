import { ComponentFixture, TestBed } from '@angular/core/testing';
import { of } from 'rxjs';
import { AccountService } from '../services/account.service';
import { ClientService } from '../services/client.service';
import { NotificationService } from '../services/notification.service';
import { ToastService } from '../services/toast.service';
import { TrainerService } from '../services/trainer.service';
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
        {
          provide: TrainerService,
          useValue: jasmine.createSpyObj<TrainerService>('TrainerService', [
            'getWorkoutRetrievalApiKey',
            'getAutoWorkoutRetrievalStatus',
            'updateTrainerRetrievalDetails',
            'addExcludedName',
            'deleteExcludedName',
            'getAllExcludedNames',
            'gatherAndUpdateExternalWorkouts'
          ])
        },
        { provide: ToastService, useValue: jasmine.createSpyObj<ToastService>('ToastService', ['showSuccess', 'showError', 'showNeutral']) },
        { provide: NotificationService, useValue: jasmine.createSpyObj<NotificationService>('NotificationService', ['refreshUnreadCount']) }
      ]
    })
    .compileComponents();

    const trainerService = TestBed.inject(TrainerService) as jasmine.SpyObj<TrainerService>;
    trainerService.getWorkoutRetrievalApiKey.and.returnValue(of({ success: true, message: 'ok', data: '' }));
    trainerService.getAutoWorkoutRetrievalStatus.and.returnValue(of({ success: true, message: 'ok', data: false }));
    trainerService.updateTrainerRetrievalDetails.and.returnValue(of({ success: true, message: 'ok' }));
    trainerService.addExcludedName.and.returnValue(of({ success: true, message: 'ok' }));
    trainerService.deleteExcludedName.and.returnValue(of({ success: true, message: 'ok' }));
    trainerService.getAllExcludedNames.and.returnValue(of({ success: true, message: 'ok', data: [] }));
    trainerService.gatherAndUpdateExternalWorkouts.and.returnValue(of({ success: true, message: 'ok', data: 0 }));

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
      expect(workoutServiceSpy.retrieveTrainerClientWorkouts).toHaveBeenCalledWith(13);
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

  describe('table pagination behavior', () => {
    it('next/prev/reset update first row offset correctly', () => {
      component.rows = 10;
      component.first = 0;

      component.next();
      expect(component.first).toBe(10);

      component.prev();
      expect(component.first).toBe(0);

      component.first = 20;
      component.reset();
      expect(component.first).toBe(0);
    });

    it('isFirstPage and isLastPage use current pagination against row count', () => {
      component.workouts = [workoutRow];
      component.rows = 10;
      component.first = 0;

      expect(component.isFirstPage()).toBeTrue();
      expect(component.isLastPage()).toBeTrue();
    });
  });

  describe('table value formatting', () => {
    it('formatDateForApi transforms Date into yyyy/MM/dd', () => {
      const formatted = component.formatDateForApi(new Date('2026-04-05T00:00:00.000Z'));

      expect(formatted).toBe('2026/04/05');
    });
  });

  describe('documentation: how these simple table tests work', () => {
    it('focuses on component table logic (rows, pagination, formatters) rather than PrimeNG internals', () => {
      component.workouts = [workoutRow, { ...workoutRow, id: 2 }];
      component.rows = 1;
      component.first = 0;

      expect(component.isFirstPage()).toBeTrue();
      component.next();
      expect(component.first).toBe(1);
    });
  });
});
