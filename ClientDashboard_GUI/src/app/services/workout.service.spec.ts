import { TestBed } from '@angular/core/testing';
import { HttpTestingController, provideHttpClientTesting } from '@angular/common/http/testing';
import { provideHttpClient } from '@angular/common/http';
import { environment } from '../environments/environment';
import { WorkoutAddDto } from '../models/dtos/workout-add-dto';
import { WorkoutUpdateDto } from '../models/dtos/workout-update-dto';
import { Client } from '../models/client';

import { WorkoutService } from './workout.service';

describe('WorkoutService', () => {
  let service: WorkoutService;
  let httpMock: HttpTestingController;
  const baseUrl = environment.apiUrl;

  beforeEach(() => {
    // Test HTTP backend: captures outgoing requests without hitting a real API.
    TestBed.configureTestingModule({
      providers: [provideHttpClient(), provideHttpClientTesting()]
    });

    service = TestBed.inject(WorkoutService);
    httpMock = TestBed.inject(HttpTestingController);
  });

  afterEach(() => {
    // Ensures every test flushes all expected requests.
    httpMock.verify();
  });

  it('should be created', () => {
    expect(service).toBeTruthy();
  });

  describe('GET request contracts', () => {
    it('retrieveTrainerClientWorkouts sends GET with trainerId query parameter', () => {
      const trainerId = 42;

      service.retrieveTrainerClientWorkouts(trainerId).subscribe();

      const req = httpMock.expectOne(
        `${baseUrl}workout/GetTrainerWorkouts?trainerId=${trainerId}`
      );
      expect(req.request.method).toBe('GET');
      req.flush({ success: true, message: 'ok', data: [] });
    });

    it('retrieveClientSpecificWorkouts sends GET with clientId query parameter', () => {
      const clientId = 7;

      service.retrieveClientSpecificWorkouts(clientId).subscribe();

      const req = httpMock.expectOne(
        `${baseUrl}workout/GetClientSpecificWorkouts?clientId=${clientId}`
      );
      expect(req.request.method).toBe('GET');
      req.flush({ success: true, message: 'ok', data: [] });
    });
  });

  describe('write request contracts', () => {
    it('addWorkout sends POST to manual create endpoint with dto payload', () => {
      const newWorkout: WorkoutAddDto = {
        workoutTitle: 'Push Session',
        clientName: 'Sam',
        clientId: 5,
        sessionDate: '2026-04-14',
        exerciseCount: 6,
        duration: 60
      };

      service.addWorkout(newWorkout).subscribe();

      const req = httpMock.expectOne(`${baseUrl}workout/Manual/NewWorkout`);
      expect(req.request.method).toBe('POST');
      expect(req.request.body).toEqual(newWorkout);
      req.flush({ success: true, message: 'created', data: 'id-1' });
    });

    it('quickAddWorkout sends POST to quick add endpoint with client payload', () => {
      const client: Client = {
        id: 9,
        firstName: 'Alex',
        surname: 'Mills',
        email: 'alex@fitcast.com',
        phoneNumber: '123456789',
        createdAt: '2026-04-14T00:00:00.000Z',
        isActive: true,
        currentBlockSession: 1,
        totalBlockSessions: 8,
        workouts: []
      };

      service.quickAddWorkout(client).subscribe();

      const req = httpMock.expectOne(`${baseUrl}workout/quickAddWorkout`);
      expect(req.request.method).toBe('POST');
      expect(req.request.body).toEqual(client);
      req.flush({ success: true, message: 'created', data: 'ok' });
    });

    it('updateWorkout sends PUT to update endpoint with dto payload', () => {
      const updateDto: WorkoutUpdateDto = {
        id: 3,
        workoutTitle: 'Updated Session',
        sessionDate: '2026-04-13',
        exerciseCount: 7,
        duration: 55
      };

      service.updateWorkout(updateDto).subscribe();

      const req = httpMock.expectOne(`${baseUrl}workout/updateWorkout`);
      expect(req.request.method).toBe('PUT');
      expect(req.request.body).toEqual(updateDto);
      req.flush({ success: true, message: 'updated' });
    });

    it('deleteWorkout sends DELETE with workoutId query parameter', () => {
      const workoutId = 13;

      service.deleteWorkout(workoutId).subscribe();

      const req = httpMock.expectOne(`${baseUrl}workout/DeleteWorkout?workoutId=${workoutId}`);
      expect(req.request.method).toBe('DELETE');
      req.flush({ success: true, message: 'deleted' });
    });
  });

  describe('documentation: how these service contract tests work', () => {
    it('intercepts requests and verifies URL, method, and payload before flushing mock responses', () => {
      service.retrieveTrainerClientWorkouts(1).subscribe();

      const req = httpMock.expectOne(`${baseUrl}workout/GetTrainerWorkouts?trainerId=1`);
      expect(req.request.method).toBe('GET');
      req.flush({ success: true, message: 'ok', data: [] });
    });
  });
});
