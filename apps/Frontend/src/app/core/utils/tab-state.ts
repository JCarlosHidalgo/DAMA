import { Signal, signal } from '@angular/core';
import { Observable, firstValueFrom } from 'rxjs';

import { Page } from '../models/page.model';

export interface TabState<T> {
  loading: boolean;
  page: Page<T> | null;
  pageIndex: number;
}

export function createEmptyTabState<T>(): TabState<T> {
  return { loading: false, page: null, pageIndex: 0 };
}

export class PaginatedTabState<T> {
  private readonly stateSignal = signal<TabState<T>>(createEmptyTabState<T>());

  readonly state: Signal<TabState<T>> = this.stateSignal.asReadonly();

  async loadFrom(
    pageRequest: (pageIndex: number) => Observable<Page<T>>,
    pageIndex: number,
  ): Promise<void> {
    this.stateSignal.set({ ...this.stateSignal(), loading: true });
    try {
      const page = await firstValueFrom(pageRequest(pageIndex));
      this.stateSignal.set({ loading: false, page, pageIndex });
    } catch (error) {
      this.stateSignal.set({ ...this.stateSignal(), loading: false });
      throw error;
    }
  }

  hasItems(): boolean {
    const current = this.stateSignal();
    return !!current.page && current.page.items.length > 0;
  }
}
