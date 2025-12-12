import { ComponentFixture, TestBed } from '@angular/core/testing';

import { ClientPersonalWorkoutsComponent } from './client-personal-workouts.component';

describe('ClientPersonalWorkoutsComponent', () => {
  let component: ClientPersonalWorkoutsComponent;
  let fixture: ComponentFixture<ClientPersonalWorkoutsComponent>;

  beforeEach(async () => {
    await TestBed.configureTestingModule({
      imports: [ClientPersonalWorkoutsComponent]
    })
    .compileComponents();

    fixture = TestBed.createComponent(ClientPersonalWorkoutsComponent);
    component = fixture.componentInstance;
    fixture.detectChanges();
  });

  it('should create', () => {
    expect(component).toBeTruthy();
  });
});
