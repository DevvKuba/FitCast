import { ComponentFixture, TestBed } from '@angular/core/testing';

import { RevenuePredictionComponent } from './revenue-prediction.component';

describe('RevenuePredictionComponent', () => {
  let component: RevenuePredictionComponent;
  let fixture: ComponentFixture<RevenuePredictionComponent>;

  beforeEach(async () => {
    await TestBed.configureTestingModule({
      imports: [RevenuePredictionComponent]
    })
    .compileComponents();

    fixture = TestBed.createComponent(RevenuePredictionComponent);
    component = fixture.componentInstance;
    fixture.detectChanges();
  });

  it('should create', () => {
    expect(component).toBeTruthy();
  });
});
