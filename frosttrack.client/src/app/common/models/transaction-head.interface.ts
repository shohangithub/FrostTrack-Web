export interface ITransactionHeadListResponse {
  id: number;
  code: string;
  name: string;
  type: string;
  displayType: string;
  isSystem: boolean;
  status: string;
}

export interface ITransactionHeadResponse {
  id: number;
  code: string;
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
  code: string;
  name: string;
  type: string;
  displayType: string;
  description?: string;
  sortOrder: number;
  isActive: boolean;
  colorCode?: string;
  iconClass?: string;
}
