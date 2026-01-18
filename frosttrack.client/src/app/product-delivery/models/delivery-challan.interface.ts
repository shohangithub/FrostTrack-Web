export interface IDeliveryChallanRequest {
  id: string;
  challanNumber: string;
  challanDate: Date;
  vehicleNumber: string;
  driverName?: string;
  driverContact?: string;
  vehicleType?: string;
  transportCompany?: string;
  destination?: string;
  branchId: number;
  remarks?: string;
  status: string;
  dispatchTime?: Date;
  deliveryTime?: Date;
  deliveryIds: string[];
}

export interface IDeliveryChallanResponse {
  id: string;
  challanNumber: string;
  challanDate: Date;
  vehicleNumber: string;
  driverName?: string;
  driverContact?: string;
  vehicleType?: string;
  transportCompany?: string;
  destination?: string;
  branchId: number;
  remarks?: string;
  status: string;
  dispatchTime?: Date;
  deliveryTime?: Date;
  challanItems: IDeliveryChallanItemResponse[];
}

export interface IDeliveryChallanItemResponse {
  id: string;
  deliveryChallanId: string;
  deliveryId: string;
  deliveryNumber: string;
  deliveryDate: Date;
  bookingNumber: string;
  customerName: string;
  chargeAmount: number;
  notes?: string;
}

export interface IDeliveryChallanListResponse {
  id: string;
  challanNumber: string;
  challanDate: Date;
  vehicleNumber: string;
  driverName?: string;
  destination?: string;
  status: string;
  totalDeliveries: number;
  totalAmount: number;
  dispatchTime?: Date;
  deliveryTime?: Date;
}
