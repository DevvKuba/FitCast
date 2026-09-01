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

  describe('markAsRead', () => {
    it('marks the given ids as read, then refetches notifications and refreshes the unread count', () => {
      notificationServiceSpy.markUserNotificationsAsRead.and.returnValue(of({ success: true, message: 'Marked as read' }));

      component.markAsRead([1, 2]);

      expect(notificationServiceSpy.markUserNotificationsAsRead).toHaveBeenCalledWith({ userId: 3, notificationIds: [1, 2] });
      expect(notificationServiceSpy.refreshUnreadCount).toHaveBeenCalled();
      expect(notificationServiceSpy.gatherAllUserNotifications).toHaveBeenCalledTimes(1);
      expect(toastServiceSpy.showSuccess).toHaveBeenCalledWith('Success', 'Marked as read');
    });

    it('does nothing when given an empty id list', () => {
      component.markAsRead([]);

      expect(notificationServiceSpy.markUserNotificationsAsRead).not.toHaveBeenCalled();
    });
  });

  describe('markAllTodayAsRead', () => {
    it('marks only the unread notifications from today', () => {
      const readToday = makeNotification({ id: 1, isRead: true, sentAt: new Date().toISOString() });
      const unreadToday = makeNotification({ id: 2, isRead: false, sentAt: new Date().toISOString() });
      component.allNotifications = [readToday, unreadToday];
      notificationServiceSpy.markUserNotificationsAsRead.and.returnValue(of({ success: true, message: 'Marked as read' }));

      component.markAllTodayAsRead();

      expect(notificationServiceSpy.markUserNotificationsAsRead).toHaveBeenCalledWith({ userId: 3, notificationIds: [2] });
    });
  });

  describe('hasUnread', () => {
    it('returns true when at least one notification is unread', () => {
      const notifications = [makeNotification({ id: 1, isRead: true }), makeNotification({ id: 2, isRead: false })];

      expect(component.hasUnread(notifications)).toBeTrue();
    });

    it('returns false when every notification is read', () => {
      const notifications = [makeNotification({ id: 1, isRead: true }), makeNotification({ id: 2, isRead: true })];

      expect(component.hasUnread(notifications)).toBeFalse();
    });

    it('returns false for an empty list', () => {
      expect(component.hasUnread([])).toBeFalse();
    });
  });

  describe('readRows', () => {
    it('returns only the notifications already marked as read', () => {
      const read = makeNotification({ id: 1, isRead: true });
      const unread = makeNotification({ id: 2, isRead: false });

      expect(component.readRows([read, unread])).toEqual([read]);
    });
  });

  describe('onPastWeekSelectionChange', () => {
    const threeDaysAgo = new Date();
    threeDaysAgo.setDate(threeDaysAgo.getDate() - 3);

    it('marks newly-checked rows as read, ignoring rows that were already read', () => {
      const alreadyRead = makeNotification({ id: 1, isRead: true, sentAt: threeDaysAgo.toISOString() });
      const newlyChecked = makeNotification({ id: 2, isRead: false, sentAt: threeDaysAgo.toISOString() });
      component.allNotifications = [alreadyRead, newlyChecked];
      notificationServiceSpy.markUserNotificationsAsRead.and.returnValue(of({ success: true, message: 'Marked as read' }));

      component.onPastWeekSelectionChange([alreadyRead, newlyChecked]);

      expect(notificationServiceSpy.markUserNotificationsAsRead).toHaveBeenCalledWith({ userId: 3, notificationIds: [2] });
    });

    it('does nothing when unchecking an already-read row', () => {
      const alreadyRead = makeNotification({ id: 1, isRead: true, sentAt: threeDaysAgo.toISOString() });
      component.allNotifications = [alreadyRead];

      component.onPastWeekSelectionChange([]);

      expect(notificationServiceSpy.markUserNotificationsAsRead).not.toHaveBeenCalled();
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
