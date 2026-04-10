export interface ISalaryPaymentRequest {
  employeeId: number;
  month: number;
  year: number;
  basicSalary: number;
  bonus: number;
  deduction: number;
  paymentMethod: string;
  note?: string;
}

export interface ISalaryPaymentResponse {
  id: number;
  employeeId: number;
  employeeName: string;
  employeeCode: string;
  month: number;
  year: number;
  basicSalary: number;
  bonus: number;
  deduction: number;
  netAmount: number;
  paymentDate: Date;
  paymentMethod: string;
  note?: string;
  transactionId: string;
  transactionCode: string;
  createdAt: Date;
}

export interface IEmployeeForSalary {
  id: number;
  name: string;
  code: string;
  designation: string;
  salary: number;
  lastPaymentDate?: Date;
  lastPaymentPeriod?: string;
}

export interface ISalaryPaymentList {
  id: string;
  employeeName: string;
  employeeCode: string;
  period: string;
  basicSalary: number;
  netAmount: number;
  paymentDate: Date;
  paymentMethod: string;
  createdAt: Date;
}

export interface IMonthlyPaymentSummary {
  month: number;
  year: number;
  totalEmployees: number;
  totalBasicSalary: number;
  totalBonus: number;
  totalDeduction: number;
  totalNetAmount: number;
  payments: ISalaryPaymentList[];
}
