using System.ComponentModel.DataAnnotations.Schema;

namespace Application.ReponseDTO;

public record ProductResponse(
    int Id,
    string ProductName,
    string ProductCode,
    string? CustomBarcode,
    int CategoryId,
    int? DefaultUnitId,
    string? ImageUrl,
    decimal? BookingRate,
    bool IsActive,
    string Status
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
    string? ImageUrl,
    decimal? BookingRate,
    string Status
    );


public record ProductLisWithStockResponse(
    int Id,
    string ProductName,
    string ProductCode,
    int CategoryId,
    string CategoryName,
    int? DefaultUnitId,
    string? UnitName,
    string? ImageUrl,
    decimal? BookingRate,
    string Status,
    double? CurrentStock,
    decimal? LastPurchaseRate,
    UnitConversion StockUnit
    );