import { describe, it, expect, beforeEach } from 'vitest';
import { Subject, of, throwError } from 'rxjs';

import { PaginatedTabState, createEmptyTabState } from './tab-state';
import { Page } from '../models/page.model';

describe('createEmptyTabState', () => {
  it('returns a fresh empty state with loading=false, page=null, pageIndex=0', () => {
    expect(createEmptyTabState<string>()).toEqual({ loading: false, page: null, pageIndex: 0 });
  });
});

describe('PaginatedTabState', () => {
  let tabState: PaginatedTabState<string>;

  beforeEach(() => {
    tabState = new PaginatedTabState<string>();
  });

  it('starts with an empty state', () => {
    expect(tabState.state()).toEqual({ loading: false, page: null, pageIndex: 0 });
    expect(tabState.hasItems()).toBe(false);
  });

  it('loads a page and updates the state when the request resolves', async () => {
    const incomingPage: Page<string> = { currentIndex: 2, maxIndex: 4, items: ['a', 'b'] };

    await tabState.loadFrom(() => of(incomingPage), 2);

    expect(tabState.state()).toEqual({ loading: false, page: incomingPage, pageIndex: 2 });
    expect(tabState.hasItems()).toBe(true);
  });

  it('exposes loading=true while the request is in flight', async () => {
    const releaseRequest = new Subject<Page<string>>();
    const finalPage: Page<string> = { currentIndex: 0, maxIndex: 0, items: ['x'] };

    const loadPromise = tabState.loadFrom(() => releaseRequest.asObservable(), 0);

    expect(tabState.state().loading).toBe(true);

    releaseRequest.next(finalPage);
    releaseRequest.complete();
    await loadPromise;

    expect(tabState.state().loading).toBe(false);
    expect(tabState.state().page).toBe(finalPage);
  });

  it('hasItems returns false when the page has zero items', async () => {
    await tabState.loadFrom(
      () => of<Page<string>>({ currentIndex: 0, maxIndex: 0, items: [] }),
      0,
    );

    expect(tabState.hasItems()).toBe(false);
  });

  it('clears the loading flag and rethrows when the request errors out', async () => {
    const failure = new Error('boom');

    await expect(tabState.loadFrom(() => throwError(() => failure), 1)).rejects.toBe(failure);

    expect(tabState.state().loading).toBe(false);
    expect(tabState.state().page).toBeNull();
  });
});
