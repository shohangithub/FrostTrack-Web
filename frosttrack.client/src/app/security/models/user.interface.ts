export interface IUserListResponse {
  id: number;
  userName: string;
  email: string;
  role: string;
  status: string;
}

export interface IUserResponse {
  id: number;
  userName: string;
  email: string;
  role: string;
  isActive: boolean;
  status: string;
}

export interface IUserRequest {
  userName: string;
  email: string;
  role: string;
  isActive: boolean;
}
