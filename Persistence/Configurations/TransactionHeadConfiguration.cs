using Domain.Entitites;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Persistence.Configurations;

public class TransactionHeadConfiguration : IEntityTypeConfiguration<TransactionHead>
{
    public void Configure(EntityTypeBuilder<TransactionHead> builder)
    {
        builder.HasIndex(t => t.Code).IsUnique();

        // Seed default transaction heads
        var seedDate = new DateTime(2025, 1, 1, 0, 0, 0, DateTimeKind.Utc);
        builder.HasData(
            new TransactionHead
            {
                Id = Guid.NewGuid(),
                Code = "BILL_COLLECTION",
                Name = "Bill Collection",
                Type = TransactionHeadTypes.CREDIT,
                DisplayType = "",
                Description = "Money received from customers for cold storage services",
                UsageFor = UsageFor.BILL_COLLECTION,
                IsSystem = true,
                IsActive = true,
                SortOrder = 1,
                ColorCode = "#28a745",
                IconClass = "fa-money-bill-wave",
                CreatedTime = seedDate,
                CreatedById = 1
            },
            new TransactionHead
            {
                Id = Guid.NewGuid(),
                Code = "BOOKING_EXTRA_CHARGE",
                Name = "Booking Extra Charge",
                Type = TransactionHeadTypes.CREDIT,
                DisplayType = "",
                UsageFor = UsageFor.BOOKING,
                Description = "Additional charges for booking services",
                IsSystem = true,
                IsActive = true,
                SortOrder = 2,
                ColorCode = "#17a2b8",
                IconClass = "fa-plus-circle",
                CreatedTime = seedDate,
                CreatedById = 1
            },
            new TransactionHead
            {
                Id = Guid.NewGuid(),
                Code = "OFFICE_COST",
                Name = "Office Cost",
                Type = TransactionHeadTypes.DEBIT,
                DisplayType = "",
                UsageFor = UsageFor.TRANSACTION,
                Description = "General office and administrative expenses",
                IsSystem = true,
                IsActive = true,
                SortOrder = 3,
                ColorCode = "#dc3545",
                IconClass = "fa-building",
                CreatedTime = seedDate,
                CreatedById = 1
            },
            new TransactionHead
            {
                Id = Guid.NewGuid(),
                Code = "ADJUSTMENT",
                Name = "Adjustment",
                Type = TransactionHeadTypes.DEBIT,
                DisplayType = "",
                UsageFor = UsageFor.TRANSACTION,
                Description = "Financial adjustments and corrections",
                IsSystem = true,
                IsActive = true,
                SortOrder = 5,
                ColorCode = "#6c757d",
                IconClass = "fa-exchange-alt",
                CreatedTime = seedDate,
                CreatedById = 1
            },
            new TransactionHead
            {
                Id = Guid.NewGuid(),
                Code = "SALARY",
                Name = "Salary Payment",
                Type = TransactionHeadTypes.DEBIT,
                DisplayType = "",
                UsageFor = UsageFor.SALARY,
                Description = "Employee salary and wage payments",
                IsSystem = true,
                IsActive = true,
                SortOrder = 7,
                ColorCode = "#fd7e14",
                IconClass = "fa-wallet",
                CreatedTime = seedDate,
                CreatedById = 1
            },
            new TransactionHead
            {
                Id = Guid.NewGuid(),
                Code = "STORAGE_CHARGE",
                Name = "Storage Charge",
                Type = TransactionHeadTypes.CREDIT,
                DisplayType = "RECEIVABLE",
                UsageFor = UsageFor.BOOKING,
                Description = "Accounts receivable created when a customer books cold storage space",
                IsSystem = true,
                IsActive = true,
                SortOrder = 8,
                ColorCode = "#0F172A",
                IconClass = "fa-snowflake",
                CreatedTime = seedDate,
                CreatedById = 1
            }
        );
    }
}
