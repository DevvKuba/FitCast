import { TestBed } from '@angular/core/testing';

import { MlPredictionService } from './ml-prediction.service';

describe('MlPredictionService', () => {
  let service: MlPredictionService;

  beforeEach(() => {
    TestBed.configureTestingModule({});
    service = TestBed.inject(MlPredictionService);
  });

  it('should be created', () => {
    expect(service).toBeTruthy();
  });
});
