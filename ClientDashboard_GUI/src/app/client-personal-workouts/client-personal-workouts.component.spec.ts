import { ComponentFixture, TestBed } from '@angular/core/testing';
import { provideHttpClient } from '@angular/common/http';
import { provideHttpClientTesting } from '@angular/common/http/testing';
import { provideRouter } from '@angular/router';

import { ClientPersonalWorkoutsComponent } from './client-personal-workouts.component';

describe('ClientPersonalWorkoutsComponent', () => {
  let component: ClientPersonalWorkoutsComponent;
  let fixture: ComponentFixture<ClientPersonalWorkoutsComponent>;

  beforeEach(async () => {
    await TestBed.configureTestingModule({
      imports: [ClientPersonalWorkoutsComponent],
      providers: [provideHttpClient(), provideHttpClientTesting(), provideRouter([])]
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
