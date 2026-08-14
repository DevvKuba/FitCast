import { TestBed } from '@angular/core/testing';
import { of } from 'rxjs';
import { AccountService } from '../services/account.service';
import { ClientService } from '../services/client.service';
import { PaymentService } from '../services/payment.service';
import { ToastService } from '../services/toast.service';
import { TrainerService } from '../services/trainer.service';
import { Payment } from '../models/payment';

import { ClientPaymentsComponent } from './client-payments.component';

describe('ClientPaymentsComponent', () => {
  let component: ClientPaymentsComponent;
  let paymentServiceSpy: jasmine.SpyObj<PaymentService>;
  let clientServiceSpy: jasmine.SpyObj<ClientService>;

  const paymentRow: Payment = {
    id: 1,
    trainerId: 3,
    clientId: 7,
    amount: 120,
    currency: 'GBP',
    numberOfSessions: 4,
    paymentDate: '2026-04-14',
    confirmed: true
  };

  beforeEach(() => {
    paymentServiceSpy = jasmine.createSpyObj<PaymentService>('PaymentService', [
      'getTrainerPayments',
      'updatePaymentInfo',
      'deleteTrainerPayment',
      'addTrainerPayment',
      'filterOldClientPayments'
    ]);
    paymentServiceSpy.getTrainerPayments.and.returnValue(of({ success: true, message: 'ok', data: [paymentRow] }));
    paymentServiceSpy.updatePaymentInfo.and.returnValue(of({ success: true, message: 'updated' }));
    paymentServiceSpy.deleteTrainerPayment.and.returnValue(of({ success: true, message: 'deleted' }));
    paymentServiceSpy.addTrainerPayment.and.returnValue(of({ success: true, message: 'added' }));
    paymentServiceSpy.filterOldClientPayments.and.returnValue(of({ success: true, message: 'filtered', data: 1 }));

    clientServiceSpy = jasmine.createSpyObj<ClientService>('ClientService', ['gatherClientNames']);
    clientServiceSpy.gatherClientNames.and.returnValue(of([{ id: 7, name: 'Alex' }]));

    TestBed.configureTestingModule({
      providers: [
        { provide: PaymentService, useValue: paymentServiceSpy },
        { provide: ClientService, useValue: clientServiceSpy },
        { provide: AccountService, useValue: { currentUser: jasmine.createSpy('currentUser').and.returnValue({ id: 3 }) } },
        { provide: ToastService, useValue: jasmine.createSpyObj<ToastService>('ToastService', ['showSuccess', 'showError', 'showNeutral']) },
        {
          provide: TrainerService,
          useValue: jasmine.createSpyObj<TrainerService>('TrainerService', [
            'updateTrainerPaymentSetting',
            'getAutoPaymentSettingStatus'
          ])
        }
      ]
    });

    const trainerService = TestBed.inject(TrainerService) as jasmine.SpyObj<TrainerService>;
    trainerService.updateTrainerPaymentSetting.and.returnValue(of({ success: true, message: 'updated' }));
    trainerService.getAutoPaymentSettingStatus.and.returnValue(of({ success: true, message: 'ok', data: false }));

    // Instantiate without template rendering to keep tests focused on table logic.
    component = TestBed.runInInjectionContext(() => new ClientPaymentsComponent());
    component.ngOnInit();
  });

  it('should create', () => {
    expect(component).toBeTruthy();
  });

  describe('table data behavior', () => {
    it('loads client names and maps payment rows with clientName on init', () => {
      expect(clientServiceSpy.gatherClientNames).toHaveBeenCalledWith(3);
      expect(paymentServiceSpy.getTrainerPayments).toHaveBeenCalled();
      expect(component.payments?.length).toBe(1);
      expect((component.payments as any[])?.[0]?.clientName).toBe('Alex');
    });

    it('maps missing clientId to "Removed Client" fallback name', () => {
      paymentServiceSpy.getTrainerPayments.and.returnValue(
        of({ success: true, message: 'ok', data: [{ ...paymentRow, clientId: 999 }] })
      );
      component.clients = [{ id: 7, name: 'Alex' }];

      component.gatherAllTrainerPayments();

      expect((component.payments as any[])?.[0]?.clientName).toBe('Removed Client');
    });

    it('clear() resets table filters and refreshes list', () => {
      const tableClearSpy = jasmine.createSpy('clear');

      component.clear({ clear: tableClearSpy } as any);

      expect(tableClearSpy).toHaveBeenCalled();
      expect(clientServiceSpy.gatherClientNames).toHaveBeenCalledTimes(2);
    });
  });

  describe('status helpers', () => {
    it('returns correct status label and severity values', () => {
      expect(component.getActivityLabel(true)).toBe('Confirmed');
      expect(component.getActivities(true)).toBe('success');
      expect(component.getActivityLabel(false)).toBe('Pending');
      expect(component.getActivities(false)).toBe('info');
    });
  });

  describe('last 30 days filter', () => {
    it('displayedPayments returns every payment when the toggle is off', () => {
      component.payments = [paymentRow, { ...paymentRow, id: 2, paymentDate: '2020-01-01' }];
      component.last30DaysOnly = false;

      expect(component.displayedPayments.length).toBe(2);
    });

    it('displayedPayments only keeps payments from the last 30 days when toggled on', () => {
      const recent = { ...paymentRow, id: 2, paymentDate: new Date().toISOString() };
      const old = { ...paymentRow, id: 3, paymentDate: '2020-01-01' };
      component.payments = [recent, old];
      component.last30DaysOnly = true;

      const displayed = component.displayedPayments;

      expect(displayed.length).toBe(1);
      expect(displayed[0].id).toBe(2);
    });
  });

  describe('avatar helpers', () => {
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

  describe('documentation: how these simple table tests work', () => {
    it('verifies mapped table rows and helper outputs through component state', () => {
      component.clients = [{ id: 7, name: 'Alex' }];
      component.gatherAllTrainerPayments();

      expect((component.payments as any[])?.[0]?.clientName).toBe('Alex');
      expect(component.getActivityLabel(component.payments?.[0].confirmed ?? false)).toBe('Confirmed');
    });
  });
});
