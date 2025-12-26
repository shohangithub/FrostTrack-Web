export interface PaginationQuery {
  pageSize: number;
  pageIndex: number;
  orderBy?: string;
  isAscending?: boolean;
  openText?: string;
  dateFrom?: Date;
  dateTo?: Date;
}
