import { ComponentFixture, TestBed } from '@angular/core/testing';
import { of } from 'rxjs';
import { AccountService } from '../services/account.service';
import { ClientService } from '../services/client.service';
import { NotificationService } from '../services/notification.service';
import { ToastService } from '../services/toast.service';
import { WorkoutService } from '../services/workout.service';
import { Client } from '../models/client';

import { ClientInfoComponent } from './client-info.component';

describe('ClientInfoComponent', () => {
  let component: ClientInfoComponent;
  let fixture: ComponentFixture<ClientInfoComponent>;
  let accountServiceMock: { currentUser: jasmine.Spy };
  let clientServiceSpy: jasmine.SpyObj<ClientService>;

  const createClient = (id: number, firstName: string): Client => ({
    id,
    firstName,
    createdAt: '2026-04-14T00:00:00.000Z',
    isActive: true,
    currentBlockSession: 1,
    totalBlockSessions: 8,
    workouts: []
  });

  beforeEach(async () => {
    accountServiceMock = {
      currentUser: jasmine.createSpy('currentUser').and.returnValue({ id: 11 })
    };

    clientServiceSpy = jasmine.createSpyObj<ClientService>('ClientService', [
      'getAllTrainerClients',
      'getClientPhoneNumber',
      'updateClient',
      'deleteClient',
      'addClient'
    ]);
    clientServiceSpy.getAllTrainerClients.and.returnValue(
      of({ success: true, message: 'ok', data: [createClient(1, 'Alex')] })
    );
    clientServiceSpy.getClientPhoneNumber.and.returnValue(of({ success: true, message: 'ok', data: '+44 1234 123456' }));
    clientServiceSpy.updateClient.and.returnValue(of({ success: true, message: 'updated' }));
    clientServiceSpy.deleteClient.and.returnValue(of({ success: true, message: 'deleted' }));
    clientServiceSpy.addClient.and.returnValue(of({ success: true, message: 'added' }));

    await TestBed.configureTestingModule({
      imports: [ClientInfoComponent],
      providers: [
        { provide: AccountService, useValue: accountServiceMock },
        { provide: ClientService, useValue: clientServiceSpy },
        { provide: WorkoutService, useValue: jasmine.createSpyObj<WorkoutService>('WorkoutService', ['quickAddWorkout']) },
        { provide: ToastService, useValue: jasmine.createSpyObj<ToastService>('ToastService', ['showSuccess', 'showError']) },
        { provide: NotificationService, useValue: jasmine.createSpyObj<NotificationService>('NotificationService', ['refreshUnreadCount']) }
      ]
    })
    .compileComponents();

    fixture = TestBed.createComponent(ClientInfoComponent);
    component = fixture.componentInstance;
    fixture.detectChanges();
  });

  it('should create', () => {
    expect(component).toBeTruthy();
  });

  describe('table data behavior', () => {
    it('loads trainer clients on init', () => {
      expect(clientServiceSpy.getAllTrainerClients).toHaveBeenCalledWith(11);
      expect(component.clients?.length).toBe(1);
      expect(component.clients?.[0].firstName).toBe('Alex');
    });

    it('clear() resets table filters and refreshes clients', () => {
      const clearSpy = jasmine.createSpy('clear');

      component.clear({ clear: clearSpy } as any);

      expect(clearSpy).toHaveBeenCalled();
      expect(clientServiceSpy.getAllTrainerClients).toHaveBeenCalledTimes(2);
    });

    it('onRowEditCancel restores original row from cloned data', () => {
      const original = createClient(2, 'Sam');
      const edited = { ...original, firstName: 'Changed Name' };
      component.clients = [edited];
      component.clonedClients[2] = original;

      component.onRowEditCancel(edited, 0);

      expect(component.clients[0].firstName).toBe('Sam');
      expect(component.clonedClients[2]).toBeUndefined();
    });
  });

  describe('table status labels', () => {
    it('returns active styling + label for active clients', () => {
      expect(component.getActivities(true)).toBe('success');
      expect(component.getActivityLabel(true)).toBe('Active');
    });

    it('returns inactive styling + label for inactive clients', () => {
      expect(component.getActivities(false)).toBe('danger');
      expect(component.getActivityLabel(false)).toBe('Inactive');
    });
  });

  describe('documentation: how these simple table tests work', () => {
    it('checks class-level table behavior without relying on PrimeNG DOM internals', () => {
      component.clients = [createClient(3, 'Taylor')];

      expect(component.clients[0].firstName).toBe('Taylor');
      expect(component.getActivityLabel(component.clients[0].isActive)).toBe('Active');
    });
  });
});
