import { TestBed } from '@angular/core/testing';
import { provideZonelessChangeDetection } from '@angular/core';
import { describe, it, expect, beforeEach, vi } from 'vitest';

import { PageInfo, Paginator } from './paginator';

describe('Paginator', () => {
  let fixture: ReturnType<typeof TestBed.createComponent<Paginator>>;
  let emitted: number[];

  function instantiate(page: PageInfo): void {
    fixture = TestBed.createComponent(Paginator);
    fixture.componentRef.setInput('page', page);
    emitted = [];
    fixture.componentInstance.pageChange.subscribe((index: number) => emitted.push(index));
    fixture.detectChanges();
  }

  beforeEach(async () => {
    await TestBed.configureTestingModule({
      imports: [Paginator],
      providers: [provideZonelessChangeDetection()],
    }).compileComponents();
  });

  function buttons(): NodeListOf<HTMLButtonElement> {
    return fixture.nativeElement.querySelectorAll('button');
  }

  it('renders the 1-based current/total label', () => {
    instantiate({ currentIndex: 2, maxIndex: 4 });
    const label = fixture.nativeElement.querySelector('nav > span') as HTMLElement;
    expect(label.textContent?.replace(/\s+/g, ' ').trim()).toContain('3 / 5');
  });

  describe('previous button', () => {
    it('is disabled when currentIndex is 0', () => {
      instantiate({ currentIndex: 0, maxIndex: 4 });
      expect(buttons()[0].disabled).toBe(true);
    });

    it('is enabled when there are previous pages', () => {
      instantiate({ currentIndex: 2, maxIndex: 4 });
      expect(buttons()[0].disabled).toBe(false);
    });

    it('emits currentIndex-1 when clicked', () => {
      instantiate({ currentIndex: 3, maxIndex: 4 });
      buttons()[0].click();
      expect(emitted).toEqual([2]);
    });

    it('does not emit when called directly with currentIndex=0', () => {
      instantiate({ currentIndex: 0, maxIndex: 4 });
      const emitSpy = vi.spyOn(fixture.componentInstance.pageChange, 'emit');
      fixture.componentInstance.prev();
      expect(emitSpy).not.toHaveBeenCalled();
    });
  });

  describe('next button', () => {
    it('is disabled when currentIndex equals maxIndex', () => {
      instantiate({ currentIndex: 4, maxIndex: 4 });
      expect(buttons()[1].disabled).toBe(true);
    });

    it('is enabled when there are more pages', () => {
      instantiate({ currentIndex: 2, maxIndex: 4 });
      expect(buttons()[1].disabled).toBe(false);
    });

    it('emits currentIndex+1 when clicked', () => {
      instantiate({ currentIndex: 1, maxIndex: 4 });
      buttons()[1].click();
      expect(emitted).toEqual([2]);
    });

    it('does not emit when called directly at maxIndex', () => {
      instantiate({ currentIndex: 4, maxIndex: 4 });
      const emitSpy = vi.spyOn(fixture.componentInstance.pageChange, 'emit');
      fixture.componentInstance.next();
      expect(emitSpy).not.toHaveBeenCalled();
    });
  });
});
