export interface ITransactionGroupListResponse {
  id: string;
  code: string;
  name: string;
  sortOrder: number;
  headCount: number;
  status: string;
}

export interface ITransactionGroupResponse {
  id: string;
  code: string;
  name: string;
  sortOrder: number;
  description?: string;
  isActive: boolean;
  status: string;
}

export interface ITransactionGroupRequest {
  name: string;
  sortOrder: number;
  description?: string;
  isActive: boolean;
}
