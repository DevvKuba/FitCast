import { TestBed } from '@angular/core/testing';
import { of } from 'rxjs';
import { NotificationService } from '../services/notification.service';
import { ClientService } from '../services/client.service';
import { AccountService } from '../services/account.service';
import { ToastService } from '../services/toast.service';
import { NotificationResponseDto } from '../models/dtos/notification-response-dto';
import { NotificationType } from '../enums/notification-type';
import { CommunicationType } from '../enums/communication-type';
import { NotificationAudience } from '../enums/notification-audience';
import { UserRole } from '../enums/user-role';

import { NotificationsComponent } from './notifications.component';

describe('NotificationsComponent', () => {
  let component: NotificationsComponent;
  let notificationServiceSpy: jasmine.SpyObj<NotificationService>;
  let clientServiceSpy: jasmine.SpyObj<ClientService>;
  let toastServiceSpy: jasmine.SpyObj<ToastService>;

  const makeNotification = (overrides: Partial<NotificationResponseDto>): NotificationResponseDto => ({
    id: 1,
    trainerId: 3,
    clientId: 7,
    message: 'A notification',
    reminderType: NotificationType.NewClientConfigurationReminder,
    sentThrough: CommunicationType.Sms,
    audience: NotificationAudience.Trainer,
    sentAt: new Date().toISOString(),
    isRead: false,
    ...overrides
  });

  beforeEach(() => {
    notificationServiceSpy = jasmine.createSpyObj<NotificationService>('NotificationService', [
      'gatherUserNotificationStatus',
      'toggleUserSMSNotificationStatus',
      'gatherLatestUserNotifications',
      'gatherAllUserNotifications',
      'markUserNotificationsAsRead',
      'gatherUnreadUserNotificationCount',
      'refreshUnreadCount'
    ]);
    notificationServiceSpy.gatherAllUserNotifications.and.returnValue(of({ success: true, message: 'ok', data: [] }));
    notificationServiceSpy.gatherUserNotificationStatus.and.returnValue(of({ success: true, message: 'ok', data: true }));

    clientServiceSpy = jasmine.createSpyObj<ClientService>('ClientService', ['gatherClientNames']);
    clientServiceSpy.gatherClientNames.and.returnValue(of([{ id: 7, name: 'alex' }]));

    toastServiceSpy = jasmine.createSpyObj<ToastService>('ToastService', ['showSuccess', 'showError', 'showNeutral']);

    TestBed.configureTestingModule({
      providers: [
        { provide: NotificationService, useValue: notificationServiceSpy },
        { provide: ClientService, useValue: clientServiceSpy },
        { provide: AccountService, useValue: { currentUser: jasmine.createSpy('currentUser').and.returnValue({ id: 3, role: UserRole.Trainer }) } },
        { provide: ToastService, useValue: toastServiceSpy }
      ]
    });

    component = TestBed.runInInjectionContext(() => new NotificationsComponent());
  });

  it('should create', () => {
    expect(component).toBeTruthy();
  });

  it('loads all notifications and the SMS status on init', () => {
    component.ngOnInit();

    expect(notificationServiceSpy.gatherAllUserNotifications).toHaveBeenCalled();
    expect(notificationServiceSpy.gatherUserNotificationStatus).toHaveBeenCalled();
    expect(component.smsNotificationsToggled).toBeTrue();
  });

  describe('Today/Past-Week split', () => {
    it('puts a notification sent today under todayNotifications', () => {
      const todayNotification = makeNotification({ id: 1, sentAt: new Date().toISOString() });
      component.allNotifications = [todayNotification];

      expect(component.todayNotifications()).toEqual([todayNotification]);
      expect(component.pastWeekNotifications()).toEqual([]);
    });

    it('puts a notification from 6 days ago under pastWeekNotifications', () => {
      const sixDaysAgo = new Date();
      sixDaysAgo.setDate(sixDaysAgo.getDate() - 6);
      const notification = makeNotification({ id: 2, sentAt: sixDaysAgo.toISOString() });
      component.allNotifications = [notification];

      expect(component.pastWeekNotifications()).toEqual([notification]);
      expect(component.todayNotifications()).toEqual([]);
    });

    it('excludes a notification from 8 days ago from both sections', () => {
      const eightDaysAgo = new Date();
      eightDaysAgo.setDate(eightDaysAgo.getDate() - 8);
      const notification = makeNotification({ id: 3, sentAt: eightDaysAgo.toISOString() });
      component.allNotifications = [notification];

      expect(component.todayNotifications()).toEqual([]);
      expect(component.pastWeekNotifications()).toEqual([]);
    });
  });

  describe('filteredNotifications', () => {
    const clientA = makeNotification({ id: 1, clientId: 7, message: 'Client A workout logged' });
    const clientB = makeNotification({ id: 2, clientId: 9, message: 'Client B payment pending' });

    beforeEach(() => {
      component.allNotifications = [clientA, clientB];
    });

    it('returns everything when no filters are set', () => {
      expect(component.filteredNotifications()).toEqual([clientA, clientB]);
    });

    it('filters by selected client', () => {
      component.selectedClient = { id: 7, name: 'alex' };

      expect(component.filteredNotifications()).toEqual([clientA]);
    });

    it('filters by search text against the message, case-insensitively', () => {
      component.searchText = 'PAYMENT';

      expect(component.filteredNotifications()).toEqual([clientB]);
    });

    it('composes client and search filters together', () => {
      component.selectedClient = { id: 7, name: 'alex' };
      component.searchText = 'payment';

      expect(component.filteredNotifications()).toEqual([]);
    });
  });

  describe('markTodayNotificationsAsRead', () => {
    it('marks only the unread notifications from today, updating them locally without refetching', () => {
      const readToday = makeNotification({ id: 1, isRead: true, sentAt: new Date().toISOString() });
      const unreadToday = makeNotification({ id: 2, isRead: false, sentAt: new Date().toISOString() });
      component.allNotifications = [readToday, unreadToday];
      notificationServiceSpy.markUserNotificationsAsRead.and.returnValue(of({ success: true, message: 'Marked as read' }));

      component.markTodayNotificationsAsRead();

      expect(notificationServiceSpy.markUserNotificationsAsRead).toHaveBeenCalledWith({ userId: 3, notificationIds: [2] });
      expect(notificationServiceSpy.refreshUnreadCount).toHaveBeenCalled();
      expect(notificationServiceSpy.gatherAllUserNotifications).not.toHaveBeenCalled();
      expect(component.allNotifications.find((n) => n.id === 2)?.isRead).toBeTrue();
    });

    it('does nothing when every notification from today is already read', () => {
      const readToday = makeNotification({ id: 1, isRead: true, sentAt: new Date().toISOString() });
      component.allNotifications = [readToday];

      component.markTodayNotificationsAsRead();

      expect(notificationServiceSpy.markUserNotificationsAsRead).not.toHaveBeenCalled();
    });

    it('is called automatically after notifications load, marking today\'s unread items as seen', () => {
      const unreadToday = makeNotification({ id: 5, isRead: false, sentAt: new Date().toISOString() });
      notificationServiceSpy.gatherAllUserNotifications.and.returnValue(of({ success: true, message: 'ok', data: [unreadToday] }));
      notificationServiceSpy.markUserNotificationsAsRead.and.returnValue(of({ success: true, message: 'Marked as read' }));

      component.ngOnInit();

      expect(notificationServiceSpy.markUserNotificationsAsRead).toHaveBeenCalledWith({ userId: 3, notificationIds: [5] });
    });
  });

  describe('tableNotifications', () => {
    it('returns the past-week window when showAllNotifications is off', () => {
      const todayNotification = makeNotification({ id: 1, sentAt: new Date().toISOString() });
      component.allNotifications = [todayNotification];
      component.showAllNotifications = false;

      expect(component.tableNotifications()).toEqual([]);
    });

    it('returns everything, including today, when showAllNotifications is on', () => {
      const todayNotification = makeNotification({ id: 1, sentAt: new Date().toISOString() });
      component.allNotifications = [todayNotification];
      component.showAllNotifications = true;

      expect(component.tableNotifications()).toEqual([todayNotification]);
    });

    it('still applies the client and search filters when showAllNotifications is on', () => {
      const clientA = makeNotification({ id: 1, clientId: 7 });
      const clientB = makeNotification({ id: 2, clientId: 9 });
      component.allNotifications = [clientA, clientB];
      component.showAllNotifications = true;
      component.selectedClient = { id: 7, name: 'alex' };

      expect(component.tableNotifications()).toEqual([clientA]);
    });
  });

  describe('getRelativeTime', () => {
    it('reports notifications sent under a minute ago as "Just now"', () => {
      expect(component.getRelativeTime(new Date().toISOString())).toBe('Just now');
    });

    it('reports minutes ago, pluralised correctly', () => {
      const fiveMinutesAgo = new Date(Date.now() - 5 * 60 * 1000);
      const oneMinuteAgo = new Date(Date.now() - 1 * 60 * 1000);

      expect(component.getRelativeTime(fiveMinutesAgo.toISOString())).toBe('5 minutes ago');
      expect(component.getRelativeTime(oneMinuteAgo.toISOString())).toBe('1 minute ago');
    });

    it('reports hours ago, pluralised correctly', () => {
      const threeHoursAgo = new Date(Date.now() - 3 * 60 * 60 * 1000);
      const oneHourAgo = new Date(Date.now() - 1 * 60 * 60 * 1000);

      expect(component.getRelativeTime(threeHoursAgo.toISOString())).toBe('3 hours ago');
      expect(component.getRelativeTime(oneHourAgo.toISOString())).toBe('1 hour ago');
    });
  });

  describe('gatherClientNames', () => {
    it('loads client names for the current trainer', () => {
      component.gatherClientNames();

      expect(clientServiceSpy.gatherClientNames).toHaveBeenCalledWith(3);
      expect(component.clients).toEqual([{ id: 7, name: 'alex' }]);
    });
  });

  describe('getNotificationTypeIcon', () => {
    it('maps each notification type to an icon', () => {
      expect(component.getNotificationTypeIcon(NotificationType.NewClientConfigurationReminder)).toBe('pi-user-plus');
      expect(component.getNotificationTypeIcon(NotificationType.PendingPaymentCreatedAlert)).toBe('pi-credit-card');
      expect(component.getNotificationTypeIcon(NotificationType.QuickAddWorkoutReminder)).toBe('pi-bolt');
    });
  });
});
