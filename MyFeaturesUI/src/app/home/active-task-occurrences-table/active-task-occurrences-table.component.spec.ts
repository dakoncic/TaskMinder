import { ComponentFixture, TestBed } from '@angular/core/testing';

import { ActiveTaskOccurrencesTableComponent } from './active-task-occurrences-table.component';

describe('ActiveTaskOccurrencesTableComponent', () => {
  let component: ActiveTaskOccurrencesTableComponent;
  let fixture: ComponentFixture<ActiveTaskOccurrencesTableComponent>;

  beforeEach(async () => {
    await TestBed.configureTestingModule({
      imports: [ActiveTaskOccurrencesTableComponent]
    })
    .compileComponents();
    
    fixture = TestBed.createComponent(ActiveTaskOccurrencesTableComponent);
    component = fixture.componentInstance;
    fixture.detectChanges();
  });

  it('should create', () => {
    expect(component).toBeTruthy();
  });
});
