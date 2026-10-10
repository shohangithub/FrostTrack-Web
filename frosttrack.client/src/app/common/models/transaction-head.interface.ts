export interface ITransactionHeadListResponse {
  id: number;
  name: string;
  type: string;
  displayType: string;
  isSystem: boolean;
  status: string;
  transactionGroupName?: string | null;
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
  transactionGroupId?: string | null;
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
  transactionGroupId?: string | null;
}

export interface ITransactionHeadLookup {
  id: string;
  name: string;
  type: string;
  displayType: string;
}
