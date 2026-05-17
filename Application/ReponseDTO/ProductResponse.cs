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
    string Status,
    bool IsDeleted,
    bool IsArchived,
    DateTime? DeletedAt,
    DateTime? ArchivedAt
    );

