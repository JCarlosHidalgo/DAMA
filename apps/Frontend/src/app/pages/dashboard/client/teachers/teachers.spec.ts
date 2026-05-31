import { Component, input, provideZonelessChangeDetection } from '@angular/core';
import { TestBed } from '@angular/core/testing';
import { describe, it, expect, beforeEach } from 'vitest';

import { Teachers } from './teachers';
import { UserList } from '@pages/dashboard/client/users/user-list';

@Component({
  selector: 'app-user-list',
  standalone: true,
  template: '<div data-testid="user-list" [attr.data-kind]="kind()"></div>',
})
class StubUserList {
  readonly kind = input<'student' | 'teacher'>('teacher');
}

describe('Teachers', () => {
  let fixture: ReturnType<typeof TestBed.createComponent<Teachers>>;

  beforeEach(async () => {
    TestBed.resetTestingModule();
    await TestBed.configureTestingModule({
      imports: [Teachers],
      providers: [provideZonelessChangeDetection()],
    })
      .overrideComponent(Teachers, {
        remove: { imports: [UserList] },
        add: { imports: [StubUserList] },
      })
      .compileComponents();
    fixture = TestBed.createComponent(Teachers);
  });

  it('renders UserList with kind="teacher"', () => {
    fixture.detectChanges();
    const userList = fixture.nativeElement.querySelector('[data-testid="user-list"]');
    expect(userList).not.toBeNull();
    expect(userList.getAttribute('data-kind')).toBe('teacher');
  });
});
