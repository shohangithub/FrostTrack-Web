export interface IEmployeeListResponse {
  id: number;
  employeeName: string;
  employeeCode: string;
  employmentType: string;
  email?: string;
  phone?: string;
  address?: string;
  dateOfBirth?: Date;
  joiningDate?: Date;
  department?: string;
  designation?: string;
  salary: number;
  emergencyContact?: string;
  bloodGroup?: string;
  nationalId?: string;
  passportNumber?: string;
  bankAccount?: string;
  notes?: string;
  photoUrl?: string;
  status: string;
}

export interface IEmployeeResponse {
  id: number;
  employeeName: string;
  employeeCode: string;
  employmentType: string;
  email?: string;
  phone?: string;
  address?: string;
  dateOfBirth?: Date;
  joiningDate?: Date;
  department?: string;
  designation?: string;
  salary: number;
  emergencyContact?: string;
  bloodGroup?: string;
  nationalId?: string;
  passportNumber?: string;
  bankAccount?: string;
  notes?: string;
  photoUrl?: string;
  isActive: boolean;
  status: string;
}

export interface IEmployeeRequest {
  id: number;
  employeeName: string;
  employeeCode: string;
  email?: string;
  phone?: string;
  address?: string;
  dateOfBirth?: Date;
  joiningDate?: Date;
  department?: string;
  designation?: string;
  salary: number;
  emergencyContact?: string;
  bloodGroup?: string;
  nationalId?: string;
  passportNumber?: string;
  bankAccount?: string;
  notes?: string;
  photoUrl?: string;
  isActive: boolean;
  branchId?: number;
}
