export interface IProductCategoryListResponse {
    id:number;
    categoryName:string;
    description?:string;
    status:string;
}

export interface IProductCategoryResponse {
    id:number;
    categoryName:string;
    description?:string;
    isActive:boolean;
    status:string;
}

export interface IProductCategoryRequest {
    categoryName:string;
    description?:string;
    isActive:boolean;
}
