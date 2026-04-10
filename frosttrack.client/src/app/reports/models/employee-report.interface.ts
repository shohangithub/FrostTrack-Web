export interface IEmployeeReportItem {
  id: number;
  employeeCode: string;
  employeeName: string;
  department: string | null;
  designation: string | null;
  employmentType: string | null;
  email: string | null;
  phone: string | null;
  address: string | null;
  dateOfBirth: string | null;
  joiningDate: string | null;
  salary: number;
  bloodGroup: string | null;
  nationalId: string | null;
  emergencyContact: string | null;
  bankAccount: string | null;
  status: string;
}
