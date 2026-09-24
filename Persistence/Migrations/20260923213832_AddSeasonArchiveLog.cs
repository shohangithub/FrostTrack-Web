using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddSeasonArchiveLog : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.EnsureSchema(
                name: "operations");

            migrationBuilder.CreateTable(
                name: "SeasonArchiveLogs",
                schema: "operations",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    BatchCode = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    SeasonTitle = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: false),
                    CutoffDate = table.Column<DateTime>(type: "datetime2", nullable: false),
                    TotalArchivedBookings = table.Column<int>(type: "int", nullable: false),
                    TotalCarriedForwardBookings = table.Column<int>(type: "int", nullable: false),
                    TotalCarriedForwardStock = table.Column<decimal>(type: "decimal(18,2)", nullable: false),
                    TotalCarriedForwardCustomerDue = table.Column<decimal>(type: "decimal(18,2)", nullable: false),
                    TotalCarriedForwardCashBalance = table.Column<decimal>(type: "decimal(18,2)", nullable: false),
                    TotalCarriedForwardBankBalance = table.Column<decimal>(type: "decimal(18,2)", nullable: false),
                    TotalArchivedDeliveries = table.Column<int>(type: "int", nullable: false),
                    TotalArchivedTransactions = table.Column<int>(type: "int", nullable: false),
                    TotalArchivedChallans = table.Column<int>(type: "int", nullable: false),
                    ExecutedById = table.Column<int>(type: "int", nullable: false),
                    ExecutedByName = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: false),
                    ExecutedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    Notes = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    TenantId = table.Column<Guid>(type: "uniqueidentifier", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_SeasonArchiveLogs", x => x.Id);
                });

            migrationBuilder.CreateIndex(
                name: "IX_SeasonArchiveLogs_ExecutedAt",
                schema: "operations",
                table: "SeasonArchiveLogs",
                column: "ExecutedAt");

            migrationBuilder.CreateIndex(
                name: "IX_SeasonArchiveLogs_TenantId",
                schema: "operations",
                table: "SeasonArchiveLogs",
                column: "TenantId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "SeasonArchiveLogs",
                schema: "operations");
        }
    }
}
