export interface Page<T> {
  currentIndex: number;
  maxIndex: number;
  items: T[];
}

export interface PaginationParams {
  pageIndex?: number;
}

export interface PagedUsersResponse {
  items: UserListItem[];
  pageIndex: number;
  maxPageIndex: number;
}

export interface UserListItem {
  id: string;
  username: string;
}
