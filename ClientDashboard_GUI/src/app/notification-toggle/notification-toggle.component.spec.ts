import { ComponentFixture, TestBed } from '@angular/core/testing';
import { ActivatedRoute, Router } from '@angular/router';
import { of } from 'rxjs';
import { NotificationService } from '../services/notification.service';
import { AccountService } from '../services/account.service';
import { ToastService } from '../services/toast.service';
import { NotificationResponseDto } from '../models/dtos/notification-response-dto';
import { NotificationType } from '../enums/notification-type';
import { CommunicationType } from '../enums/communication-type';
import { NotificationAudience } from '../enums/notification-audience';

import { NotificationToggleComponent } from './notification-toggle.component';

describe('NotificationToggleComponent', () => {
  let component: NotificationToggleComponent;
  let fixture: ComponentFixture<NotificationToggleComponent>;
  let notificationServiceSpy: jasmine.SpyObj<NotificationService>;
  let toastServiceSpy: jasmine.SpyObj<ToastService>;

  const notifications: NotificationResponseDto[] = [
    {
      id: 1,
      trainerId: 3,
      message: 'New client John Smith joined',
      reminderType: NotificationType.NewClientConfigurationReminder,
      sentThrough: CommunicationType.Sms,
      audience: NotificationAudience.Trainer,
      sentAt: '2026-08-28T09:00:00Z',
      isRead: false
    }
  ];

  beforeEach(async () => {
    notificationServiceSpy = jasmine.createSpyObj<NotificationService>('NotificationService', [
      'gatherUserNotificationStatus',
      'toggleUserSMSNotificationStatus',
      'gatherLatestUserNotifications',
      'gatherAllUserNotifications',
      'gatherUnreadUserNotificationCount',
      'refreshUnreadCount'
    ]);
    notificationServiceSpy.gatherUserNotificationStatus.and.returnValue(of({ success: true, message: 'ok', data: true }));

    toastServiceSpy = jasmine.createSpyObj<ToastService>('ToastService', ['showSuccess', 'showError', 'showNeutral']);

    // RouterLink (used by the "View All Notifications" button) subscribes to router.events
    // and calls createUrlTree/serializeUrl internally, same as the password-reset page's mock.
    const routerSpy = jasmine.createSpyObj<Router>('Router', ['navigateByUrl', 'createUrlTree', 'serializeUrl'], { events: of() });
    routerSpy.createUrlTree.and.returnValue({} as any);
    routerSpy.serializeUrl.and.returnValue('/notifications');

    await TestBed.configureTestingModule({
      imports: [NotificationToggleComponent],
      providers: [
        { provide: NotificationService, useValue: notificationServiceSpy },
        { provide: AccountService, useValue: { currentUser: jasmine.createSpy('currentUser').and.returnValue({ id: 3 }) } },
        { provide: ToastService, useValue: toastServiceSpy },
        { provide: Router, useValue: routerSpy },
        { provide: ActivatedRoute, useValue: {} }
      ]
    })
    .compileComponents();

    fixture = TestBed.createComponent(NotificationToggleComponent);
    component = fixture.componentInstance;
    component.latestNotifications = notifications;
    fixture.detectChanges();
  });

  it('should create', () => {
    expect(component).toBeTruthy();
  });

  it('renders the notifications passed via @Input() without re-fetching them', () => {
    expect(notificationServiceSpy.gatherLatestUserNotifications).not.toHaveBeenCalled();
    expect(notificationServiceSpy.gatherAllUserNotifications).not.toHaveBeenCalled();
    expect(component.latestNotifications).toEqual(notifications);

    const rendered = (fixture.nativeElement as HTMLElement).textContent ?? '';
    expect(rendered).toContain('New client John Smith joined');
  });

  it('loads the SMS notification status on init', () => {
    expect(notificationServiceSpy.gatherUserNotificationStatus).toHaveBeenCalled();
    expect(component.smsNotificationsToggled).toBeTrue();
  });

  it('toggles SMS notification status and shows a success toast', () => {
    notificationServiceSpy.toggleUserSMSNotificationStatus.and.returnValue(of({ success: true, message: 'Updated' }));

    component.onNotificationToggle({ checked: false });

    expect(notificationServiceSpy.toggleUserSMSNotificationStatus).toHaveBeenCalledWith({ notificationStatus: false });
    expect(toastServiceSpy.showSuccess).toHaveBeenCalledWith('Success', 'Updated');
  });

  it('emits viewAllClicked when the "View All Notifications" button is clicked', () => {
    const emitSpy = jasmine.createSpy('viewAllClicked');
    component.viewAllClicked.subscribe(emitSpy);

    const button = (fixture.nativeElement as HTMLElement).querySelector('p-button') as HTMLElement;
    button.dispatchEvent(new MouseEvent('click', { bubbles: true }));

    expect(emitSpy).toHaveBeenCalled();
  });

  it('maps each notification type to an icon', () => {
    expect(component.getNotificationTypeIcon(NotificationType.NewClientConfigurationReminder)).toBe('pi-user-plus');
    expect(component.getNotificationTypeIcon(NotificationType.PendingPaymentCreatedAlert)).toBe('pi-credit-card');
    expect(component.getNotificationTypeIcon(NotificationType.QuickAddWorkoutReminder)).toBe('pi-bolt');
  });
});
