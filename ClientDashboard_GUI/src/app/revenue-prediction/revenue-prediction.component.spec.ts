import { ComponentFixture, TestBed } from '@angular/core/testing';
import { provideHttpClient } from '@angular/common/http';
import { provideHttpClientTesting } from '@angular/common/http/testing';
import { provideRouter } from '@angular/router';
import { MessageService } from 'primeng/api';

import { RevenuePredictionComponent } from './revenue-prediction.component';

describe('RevenuePredictionComponent', () => {
  let component: RevenuePredictionComponent;
  let fixture: ComponentFixture<RevenuePredictionComponent>;

  beforeEach(async () => {
    await TestBed.configureTestingModule({
      imports: [RevenuePredictionComponent],
      providers: [provideHttpClient(), provideHttpClientTesting(), provideRouter([]), MessageService]
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
