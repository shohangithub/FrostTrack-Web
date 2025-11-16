export interface IUnitConversionListResponse {
  id: number;
  unitName: string;
  baseUnitId: number;
  conversionValue: number;
  baseUnitName: string;
  description?: string;
  status: string;
}

export interface IUnitConversionResponse {
  id: number;
  unitName: string;
  baseUnitId: number;
  conversionValue: number;
  description?: string;
  isActive: boolean;
  status: string;
}

export interface IUnitConversionRequest {
  unitName: string;
  conversionValue: number;
  baseUnitId: number;
  description?: string;
  isActive: boolean;
}
