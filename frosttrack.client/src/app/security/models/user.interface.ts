export interface IUserListResponse {
  id: number;
  userName: string;
  email: string;
  roleNames: string[];
  status: string;
  profileImageUrl?: string;
}

export interface IUserResponse {
  id: number;
  userName: string;
  email: string;
  roleNames: string[];
  isActive: boolean;
  status: string;
  profileImageUrl?: string;
}

export interface IUserRequest {
  userName: string;
  email: string;
  role: string;
  isActive: boolean;
  profileImageUrl?: string;
}

export interface IChangePasswordRequest {
  currentPassword: string;
  newPassword: string;
  confirmPassword: string;
}

export interface ISetPasswordRequest {
  userId: number;
  newPassword: string;
  confirmPassword: string;
}
