export interface ITransactionGroupListResponse {
  id: string;
  code: string;
  name: string;
  sortOrder: number;
  headCount: number;
  status: string;
  accountType?: string | null;
}

export interface ITransactionGroupResponse {
  id: string;
  code: string;
  name: string;
  sortOrder: number;
  description?: string;
  isActive: boolean;
  status: string;
  accountType?: string | null;
}

export interface ITransactionGroupRequest {
  name: string;
  sortOrder: number;
  description?: string;
  isActive: boolean;
  accountType?: string | null;
}
