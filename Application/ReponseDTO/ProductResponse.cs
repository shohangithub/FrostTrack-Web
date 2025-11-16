using System.ComponentModel.DataAnnotations.Schema;

namespace Application.ReponseDTO;

public record ProductResponse(
    int Id,
    string ProductName,
    string ProductCode,
    string? CustomBarcode,
    int CategoryId,
    int? DefaultUnitId,
    bool IsRawMaterial,
    bool IsFinishedGoods,
    string? ImageUrl,
    int? ReOrederLevel,
    decimal? PurchaseRate,
    decimal? SellingRate,
    decimal? WholesalePrice,
    decimal? VatPercent,
    bool IsActive,
    string Status,
    bool IsProductAsService,
    string ProductAs
    );
public record ProductListResponse(
    int Id,
    string ProductName,
    string ProductCode,
    string? CustomBarcode,
    int CategoryId,
    string CategoryName,
    int? DefaultUnitId,
    string? UnitName,
    bool IsRawMaterial,
    bool IsFinishedGoods,
    string? ImageUrl,
    int? ReOrederLevel,
    decimal? PurchaseRate,
    decimal? SellingRate,
    decimal? WholesalePrice,
    decimal? VatPercent,
    string Status,
    bool IsProductAsService,
    string ProductAs
    );


public record ProductLisWithStockResponse(
    int Id,
    string ProductName,
    string ProductCode,
    int CategoryId,
    string CategoryName,
    int? DefaultUnitId,
    string? UnitName,
    bool IsRawMaterial,
    bool IsFinishedGoods,
    string? ImageUrl,
    int? ReOrederLevel,
    decimal? PurchaseRate,
    decimal? SellingRate,
    decimal? WholesalePrice,
    decimal? VatPercent,
    string Status,
    bool IsProductAsService,
    string ProductAs,
    double? CurrentStock,
    decimal? LastPurchaseRate,
    UnitConversion StockUnit
    );