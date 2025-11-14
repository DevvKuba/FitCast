import { ComponentFixture, TestBed } from '@angular/core/testing';

import { TrainerProfilePageComponent } from './trainer-profile-page.component';

describe('TrainerProfilePageComponent', () => {
  let component: TrainerProfilePageComponent;
  let fixture: ComponentFixture<TrainerProfilePageComponent>;

  beforeEach(async () => {
    await TestBed.configureTestingModule({
      imports: [TrainerProfilePageComponent]
    })
    .compileComponents();

    fixture = TestBed.createComponent(TrainerProfilePageComponent);
    component = fixture.componentInstance;
    fixture.detectChanges();
  });

  it('should create', () => {
    expect(component).toBeTruthy();
  });
});
