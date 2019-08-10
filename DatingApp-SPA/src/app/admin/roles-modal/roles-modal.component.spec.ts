import { async, ComponentFixture, TestBed } from '@angular/core/testing';

import { RolesModalComponent } from './roles-modal.component';

describe('RolesModalComponent', () => {
  let component: RolesModalComponent;
  let fixture: ComponentFixture<RolesModalComponent>;

  beforeEach(async(() => {
    TestBed.configureTestingModule({
      declarations: [ RolesModalComponent ]
    })
    .compileComponents();
  }));

  beforeEach(() => {
    fixture = TestBed.createComponent(RolesModalComponent);
    component = fixture.componentInstance;
    fixture.detectChanges();
  });

  it('should create', () => {
    expect(component).toBeTruthy();
  });
});
