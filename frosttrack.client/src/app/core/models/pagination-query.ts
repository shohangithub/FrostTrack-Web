export interface PaginationQuery {
    pageSize:number,
    pageIndex:number,
    orderBy?:string,
    isAscending?:boolean,
    openText?:boolean,
    dateFrom?:Date,
    dateTo?:Date
}
