export interface ITransactionHeadListResponse {
  id: number;
  name: string;
  type: string;
  displayType: string;
  isSystem: boolean;
  status: string;
}

export interface ITransactionHeadResponse {
  id: number;
  name: string;
  type: string;
  displayType: string;
  sortOrder: number;
  description?: string;
  isActive: boolean;
  isSystem: boolean;
  colorCode?: string;
  iconClass?: string;
  status: string;
}

export interface ITransactionHeadRequest {
  name: string;
  type: string;
  displayType: string;
  description?: string;
  sortOrder: number;
  isActive: boolean;
  colorCode?: string;
  iconClass?: string;
}

export interface ITransactionHeadLookup {
  id: string;
  name: string;
  type: string;
  displayType: string;
}
