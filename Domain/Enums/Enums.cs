using System.ComponentModel;

namespace Domain.Enums;

public enum ERoles
{
    MasterAdmin = 1,
    Admin,
    Standard
}

public static class RoleNames
{
    public const string SuperAdmin = "SUPERADMIN";
    public const string Admin = "ADMIN";
    public const string Manager = "MANAGER";
    public const string Seller = "SELLER";
    public const string Standard = "STANDARD";
}




public enum ECustomerType
{
    Retail = 1,
    Wholesale
}

[DefaultValue(ECodeGeneration.Company)]
public enum ECodeGeneration
{
    Company = 1,
    Branch
}

public static class SalesType
{
    public const string
       RETAIL = "RETAIL",
       WHOLESALE = "WHOLESALE";
}