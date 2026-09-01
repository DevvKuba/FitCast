import { ComponentFixture, TestBed } from '@angular/core/testing';
import { ActivatedRoute, Router } from '@angular/router';
import { MessageService } from 'primeng/api';
import { of } from 'rxjs';
import { AccountService } from '../services/account.service';
import { NotificationService } from '../services/notification.service';

import { UserNavbar } from './user-navbar';

describe('Navbar', () => {
  let component: UserNavbar;
  let fixture: ComponentFixture<UserNavbar>;

  beforeEach(async () => {
    const notificationServiceSpy = jasmine.createSpyObj<NotificationService>('NotificationService', [
      'gatherUserNotificationStatus',
      'toggleUserSMSNotificationStatus',
      'gatherLatestUserNotifications',
      'gatherAllUserNotifications',
      'gatherUnreadUserNotificationCount',
      'refreshUnreadCount'
    ]);
    // unreadNotificationCount is a signal on the real service, not a spied method.
    (notificationServiceSpy as any).unreadNotificationCount = () => 0;

    // RouterLink (used by the notification pane's "View All Notifications" button) subscribes to
    // router.events and calls createUrlTree/serializeUrl internally, same as the password-reset page's mock.
    const routerSpy = jasmine.createSpyObj<Router>('Router', ['navigateByUrl', 'createUrlTree', 'serializeUrl'], { events: of() });
    routerSpy.createUrlTree.and.returnValue({} as any);
    routerSpy.serializeUrl.and.returnValue('/notifications');

    await TestBed.configureTestingModule({
      imports: [UserNavbar],
      providers: [
        { provide: AccountService, useValue: { currentUser: jasmine.createSpy('currentUser').and.returnValue(null) } },
        { provide: NotificationService, useValue: notificationServiceSpy },
        { provide: MessageService, useValue: jasmine.createSpyObj('MessageService', ['add']) },
        { provide: Router, useValue: routerSpy },
        { provide: ActivatedRoute, useValue: {} }
      ]
    })
    .compileComponents();

    fixture = TestBed.createComponent(UserNavbar);
    component = fixture.componentInstance;
    fixture.detectChanges();
  });

  it('should create', () => {
    expect(component).toBeTruthy();
  });
});
