export interface IBaseUnitListResponse {
    id:number;
    unitName:string;
    description?:string;
    status:string;
}

export interface IBaseUnitResponse {
    id:number;
    unitName:string;
    description?:string;
    isActive:boolean;
    status:string;
}

export interface IBaseUnitRequest {
    unitName:string;
    description?:string;
    isActive:boolean;
}
