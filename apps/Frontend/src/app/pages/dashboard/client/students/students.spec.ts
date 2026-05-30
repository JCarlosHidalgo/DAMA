import { Component, input } from '@angular/core';
import { TestBed } from '@angular/core/testing';
import { provideZonelessChangeDetection } from '@angular/core';
import { describe, it, expect, beforeEach } from 'vitest';

import { Students } from './students';
import { UserList } from '../users/user-list';

@Component({
  selector: 'app-user-list',
  standalone: true,
  template: '<div data-testid="user-list" [attr.data-kind]="kind()"></div>',
})
class StubUserList {
  readonly kind = input<'student' | 'teacher'>('student');
}

describe('Students', () => {
  let fixture: ReturnType<typeof TestBed.createComponent<Students>>;

  beforeEach(async () => {
    TestBed.resetTestingModule();
    await TestBed.configureTestingModule({
      imports: [Students],
      providers: [provideZonelessChangeDetection()],
    })
      .overrideComponent(Students, {
        remove: { imports: [UserList] },
        add: { imports: [StubUserList] },
      })
      .compileComponents();
    fixture = TestBed.createComponent(Students);
  });

  it('renders UserList with kind="student"', () => {
    fixture.detectChanges();
    const userList = fixture.nativeElement.querySelector('[data-testid="user-list"]');
    expect(userList).not.toBeNull();
    expect(userList.getAttribute('data-kind')).toBe('student');
  });
});
