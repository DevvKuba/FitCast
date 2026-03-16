import { ComponentFixture, TestBed } from '@angular/core/testing';

import { TrainerAnalyticsComponent } from './trainer-analytics.component';

describe('TrainerAnalyticsComponent', () => {
  let component: TrainerAnalyticsComponent;
  let fixture: ComponentFixture<TrainerAnalyticsComponent>;

  beforeEach(async () => {
    await TestBed.configureTestingModule({
      imports: [TrainerAnalyticsComponent]
    })
    .compileComponents();

    fixture = TestBed.createComponent(TrainerAnalyticsComponent);
    component = fixture.componentInstance;
    fixture.detectChanges();
  });

  it('should create', () => {
    expect(component).toBeTruthy();
  });
});
