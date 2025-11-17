export interface PurchaseInvoiceReportResponse {
  id: number;
  invoiceNumber: string;
  invoiceDate: string; // ISO string (DateTime in C#)
  subtotal: number;
  vatPercent: number;
  vatAmount: number;
  discountPercent: number;
  discountAmount: number;
  otherCost: number;
  invoiceAmount: number;
  paidAmount: number;
  branchId: number;
  supplier: SupplierReportResponse;
  purchaseDetails: PurchaseDetailReportResponse[];
}

export interface SupplierReportResponse {
  supplierName: string;
  supplierCode: string;
  supplierBarcode: string;
  supplierMobile?: string | null;
  address?: string | null;
  creditLimit: number;
  openingBalance: number;
  previousDue: number;
}

export interface PurchaseDetailReportResponse {
  id: number;
  purchaseRate: number;
  purchaseQuantity: number;
  purchaseAmount: number;
  product?: ProductReportResponse | null;
  purchaseUnit?: UnitConversionReportResponse | null;
}

export interface ProductReportResponse {
  id: number;
  productName: string;
  productCode: string;
  categoryId: number;
  defaultUnitId?: number | null;
  imageUrl?: string | null;
  purchaseRate?: number | null;
  bookingRate?: number | null;
  wholesalePrice?: number | null;
  vatPercent?: number | null;
  isActive: boolean;
  status: string;
  isProductAsService: boolean;
  productAs: string;
}

export interface UnitConversionReportResponse {
  id: number;
  unitName: string;
  baseUnitId: number;
  conversionValue: number;
  description: string;
  isActive: boolean;
  status: string;
}
