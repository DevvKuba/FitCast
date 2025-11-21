import { ComponentFixture, TestBed } from '@angular/core/testing';

import { ClientPaymentsComponent } from './client-payments.component';

describe('ClientPaymentsComponent', () => {
  let component: ClientPaymentsComponent;
  let fixture: ComponentFixture<ClientPaymentsComponent>;

  beforeEach(async () => {
    await TestBed.configureTestingModule({
      imports: [ClientPaymentsComponent]
    })
    .compileComponents();

    fixture = TestBed.createComponent(ClientPaymentsComponent);
    component = fixture.componentInstance;
    fixture.detectChanges();
  });

  it('should create', () => {
    expect(component).toBeTruthy();
  });
});
