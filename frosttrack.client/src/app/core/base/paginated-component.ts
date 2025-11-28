import { Observable } from 'rxjs';
import { BaseComponent } from './base-component';
import { DefaultPagination } from '@config/pagination';
import { PaginationQuery } from '@core/models/pagination-query';

/**
 * Pagination functionality that can be mixed into components
 */
export abstract class PaginatedComponent extends BaseComponent {
  // Pagination state
  currentPage = DefaultPagination.PAGEINDEX;
  pageSize = DefaultPagination.PAGESIZE;
  totalItems = 0;
  loadingIndicator = false;

  // Pagination with sorting
  pagination: PaginationQuery = {
    pageSize: DefaultPagination.PAGESIZE,
    pageIndex: DefaultPagination.PAGEINDEX,
    orderBy: DefaultPagination.ORDERBY,
    isAscending: DefaultPagination.ASCENDING,
  };

  // Search and filters
  searchQuery = '';

  /**
   * Load paginated data with automatic loading state management
   */
  protected loadPaginatedData<T>(
    serviceCall: (
      query: any
    ) => Observable<{ data: T[]; paging: { totalData: number } }>,
    options: {
      additionalFilters?: any;
      onDataLoaded?: (data: T[]) => void;
      errorMessage?: string;
    } = {}
  ): void {
    const {
      additionalFilters = {},
      onDataLoaded,
      errorMessage = 'Error loading data',
    } = options;

    // Update pagination object
    this.pagination.pageIndex = this.currentPage - 1;
    this.pagination.pageSize = this.pageSize;

    const paginationQuery = {
      ...this.pagination,
      openText: this.searchQuery,
      ...additionalFilters,
    };

    super.loadData(serviceCall(paginationQuery), {
      loadingState: this,
      onSuccess: (response: any) => {
        if (response) {
          onDataLoaded?.(response.data);
          this.totalItems = response.paging.totalData;
        }
      },
      onError: (error) => {
        console.error(errorMessage, error);
      },
    });
  }

  /**
   * Handle search input changes
   */
  onSearch(): void {
    this.currentPage = 1;
    this.loadData();
  }

  /**
   * Handle page changes from datatable
   */
  onPageChange(event: any): void {
    this.currentPage = event.offset + 1;
    this.loadData();
  }

  /**
   * Clear all filters and reload
   */
  clearFilters(): void {
    this.searchQuery = '';
    this.currentPage = 1;
    this.loadData();
  }

  /**
   * Abstract method that implementing components must provide
   */
  abstract override loadData(): void;
}
