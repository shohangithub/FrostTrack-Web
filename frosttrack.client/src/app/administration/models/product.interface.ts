import { IUnitConversionResponse } from "../../common/models/unit-conversion.interface";

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
  isRawMaterial: boolean;
  isFinishedGoods: boolean;
  reOrederLevel: number | null;
  purchaseRate: number | null;
  sellingRate: number | null;
  wholesalePrice: number | null;
  vatPercent: number | null;
  isProductAsService: boolean;
  productAs: string;
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
  isRawMaterial: boolean;
  isFinishedGoods: boolean;
  reOrederLevel: number | null;
  purchaseRate: number | null;
  sellingRate: number | null;
  wholesalePrice: number | null;
  vatPercent: number | null;
  isProductAsService: boolean;
  productAs: string;
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
  isRawMaterial: boolean;
  isFinishedGoods: boolean;
  reOrederLevel: number | null;
  purchaseRate: number | null;
  sellingRate: number | null;
  wholesalePrice: number | null;
  vatPercent: number | null;
  isProductAsService: boolean;
  productAs: string;
  isActive: boolean;
  status: string;
  branchId: number | null;
}
