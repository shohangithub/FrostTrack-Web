using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddIsContractedToBooking : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<bool>(
                name: "IsContracted",
                schema: "product",
                table: "Booking",
                type: "bit",
                nullable: false,
                defaultValue: false);

            migrationBuilder.CreateIndex(
                name: "IX_Customers_TenantId",
                table: "Customers",
                column: "TenantId");

            migrationBuilder.CreateIndex(
                name: "IX_BankTransactions_TenantId",
                table: "BankTransactions",
                column: "TenantId");

            migrationBuilder.CreateIndex(
                name: "IX_Banks_TenantId",
                table: "Banks",
                column: "TenantId");

            migrationBuilder.CreateIndex(
                name: "IX_Assets_TenantId",
                table: "Assets",
                column: "TenantId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_Customers_TenantId",
                table: "Customers");

            migrationBuilder.DropIndex(
                name: "IX_BankTransactions_TenantId",
                table: "BankTransactions");

            migrationBuilder.DropIndex(
                name: "IX_Banks_TenantId",
                table: "Banks");

            migrationBuilder.DropIndex(
                name: "IX_Assets_TenantId",
                table: "Assets");

            migrationBuilder.DropColumn(
                name: "IsContracted",
                schema: "product",
                table: "Booking");
        }
    }
}
