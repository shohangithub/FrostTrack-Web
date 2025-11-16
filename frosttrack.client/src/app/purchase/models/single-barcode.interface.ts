export interface ISingleBarcode {
  id: number;
  productName: string;
  productCode: string;
  customBarcode: string;
  sellingRate: number | null;
  quantity: number | null;
  article: string|null;
  isSingle: boolean;
  width: number | null;
  height: number | null;
}
