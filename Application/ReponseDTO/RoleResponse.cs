using Domain.Enums;
using System.ComponentModel.DataAnnotations.Schema;
using System.ComponentModel.DataAnnotations;

namespace Application.ReponseDTO;

public record class RoleResponse(int Id, string Name);
public record class RoleListResponse(int Id, string Name);
