using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddBalanceSheetSetup : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "AccountCategory",
                schema: "finance",
                table: "TransactionHeads",
                type: "nvarchar(50)",
                maxLength: 50,
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "BalanceSheetStartDate",
                table: "Companies",
                type: "datetime2",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "FiscalYearStartMonth",
                table: "Companies",
                type: "int",
                nullable: false,
                defaultValue: 7); // July–June

            migrationBuilder.AddColumn<decimal>(
                name: "OpeningWdv",
                table: "Assets",
                type: "decimal(18,2)",
                nullable: true);

            migrationBuilder.CreateTable(
                name: "BalanceSheetEntries",
                schema: "finance",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Code = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    EntryDate = table.Column<DateTime>(type: "datetime2", nullable: false),
                    EntryType = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: false),
                    LineItem = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    AccountName = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: false),
                    Amount = table.Column<decimal>(type: "decimal(18,2)", nullable: false),
                    ProfitCategory = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: true),
                    Note = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true),
                    TenantId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    CreatedById = table.Column<int>(type: "int", nullable: false),
                    CreatedTime = table.Column<DateTime>(type: "datetime2", nullable: false),
                    LastUpdatedById = table.Column<int>(type: "int", nullable: true),
                    LastUpdatedTime = table.Column<DateTime>(type: "datetime2", nullable: true),
                    IsDeleted = table.Column<bool>(type: "bit", nullable: false),
                    DeletedAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    DeletedById = table.Column<int>(type: "int", nullable: true),
                    IsArchived = table.Column<bool>(type: "bit", nullable: false),
                    ArchivedAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    ArchivedById = table.Column<int>(type: "int", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_BalanceSheetEntries", x => x.Id);
                });

            migrationBuilder.CreateIndex(
                name: "IX_BalanceSheetEntries_TenantId",
                schema: "finance",
                table: "BalanceSheetEntries",
                column: "TenantId");

            migrationBuilder.CreateIndex(
                name: "IX_BalanceSheetEntries_TenantId_EntryDate",
                schema: "finance",
                table: "BalanceSheetEntries",
                columns: new[] { "TenantId", "EntryDate" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "BalanceSheetEntries",
                schema: "finance");

            migrationBuilder.DropColumn(
                name: "AccountCategory",
                schema: "finance",
                table: "TransactionHeads");

            migrationBuilder.DropColumn(
                name: "BalanceSheetStartDate",
                table: "Companies");

            migrationBuilder.DropColumn(
                name: "FiscalYearStartMonth",
                table: "Companies");

            migrationBuilder.DropColumn(
                name: "OpeningWdv",
                table: "Assets");
        }
    }
}
