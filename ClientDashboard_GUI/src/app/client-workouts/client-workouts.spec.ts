import { ComponentFixture, TestBed } from '@angular/core/testing';

import { ClientWorkouts } from './client-workouts';

describe('ClientWorkouts', () => {
  let component: ClientWorkouts;
  let fixture: ComponentFixture<ClientWorkouts>;

  beforeEach(async () => {
    await TestBed.configureTestingModule({
      imports: [ClientWorkouts]
    })
    .compileComponents();

    fixture = TestBed.createComponent(ClientWorkouts);
    component = fixture.componentInstance;
    fixture.detectChanges();
  });

  it('should create', () => {
    expect(component).toBeTruthy();
  });
});
