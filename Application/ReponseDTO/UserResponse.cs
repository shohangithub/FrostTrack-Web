using Domain.Enums;
using System.ComponentModel.DataAnnotations.Schema;
using System.ComponentModel.DataAnnotations;

namespace Application.ReponseDTO;

public record class UserResponseForToken(int Id, Guid TenantId, string UserName, string Email, IList<string> RoleNames, int? BranchId, bool IsActive, string Status);
public record class UserResponse(int Id, string UserName, string Email, IList<string> RoleNames, bool IsActive, string Status);
public record class UserListResponse(int Id, string UserName, string Email, IList<string> RoleNames, string Status);
