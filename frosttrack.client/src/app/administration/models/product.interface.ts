import { IUnitConversionResponse } from '../../common/models/unit-conversion.interface';

export interface IProductListResponse {
  id: number;
  productName: string;
  productCode: string;
  customBarcode: string;
  categoryId: number;
  categoryName: string;
  defaultUnitId: number | null;
  unitName: string | null;
  imageUrl: string;
  bookingRate: number | null;
  isActive: boolean;
  status: string;
  branchId: number | null;
}

export interface IProductListWithStockResponse extends IProductListResponse {
  currentStock: number | null;
  lastPurchaseRate: number | null;
  stockUnit: IUnitConversionResponse | null;
}

export interface IProductResponse {
  id: number;
  productName: string;
  productCode: string;
  customBarcode: string;
  categoryId: number;
  categoryName: string | null;
  defaultUnitId: number;
  unitName: string | null;
  imageUrl: string;
  bookingRate: number | null;
  isActive: boolean;
  status: string;
  branchId: number | null;
}

export interface IProductRequest {
  productName: string;
  productCode: string;
  customBarcode: string;
  categoryId: number;
  defaultUnitId: number | null;
  imageUrl: string;
  bookingRate: number | null;
  isActive: boolean;
  status: string;
  branchId: number | null;
}
