export interface PaginationResult<T> {
  data: T[];
  paging: PagingResponse;
}

export interface PagingResponse {
  hasNextPage: boolean;
  hasPreviousPage: boolean;
  pageSize: boolean;
  currentPage: boolean;
  totalData: number;
  totalPages: number;
}
