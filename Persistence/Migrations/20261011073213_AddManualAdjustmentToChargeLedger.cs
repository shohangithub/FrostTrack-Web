using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddManualAdjustmentToChargeLedger : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "ReferenceCode",
                schema: "product",
                table: "RecurringChargeEntries",
                type: "nvarchar(50)",
                maxLength: 50,
                nullable: true);

            migrationBuilder.AddColumn<Guid>(
                name: "ReversalOfId",
                schema: "product",
                table: "RecurringChargeEntries",
                type: "uniqueidentifier",
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "ReferenceCode",
                schema: "product",
                table: "RecurringChargeEntries");

            migrationBuilder.DropColumn(
                name: "ReversalOfId",
                schema: "product",
                table: "RecurringChargeEntries");
        }
    }
}
