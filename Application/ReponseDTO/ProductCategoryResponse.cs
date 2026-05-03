namespace Application.ReponseDTO;

public record ProductCategoryResponse(int Id, string CategoryName, string Description, bool IsActive, string Status);
public record ProductCategoryListResponse(int Id, string CategoryName, string? Description, string Status, bool IsDeleted, bool IsArchived, DateTime? DeletedAt, DateTime? ArchivedAt);
