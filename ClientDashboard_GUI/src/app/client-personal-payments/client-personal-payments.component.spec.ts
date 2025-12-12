import { ComponentFixture, TestBed } from '@angular/core/testing';

import { ClientPersonalPaymentsComponent } from './client-personal-payments.component';

describe('ClientPersonalPaymentsComponent', () => {
  let component: ClientPersonalPaymentsComponent;
  let fixture: ComponentFixture<ClientPersonalPaymentsComponent>;

  beforeEach(async () => {
    await TestBed.configureTestingModule({
      imports: [ClientPersonalPaymentsComponent]
    })
    .compileComponents();

    fixture = TestBed.createComponent(ClientPersonalPaymentsComponent);
    component = fixture.componentInstance;
    fixture.detectChanges();
  });

  it('should create', () => {
    expect(component).toBeTruthy();
  });
});
