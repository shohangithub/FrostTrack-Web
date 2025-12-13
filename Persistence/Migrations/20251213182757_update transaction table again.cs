using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Persistence.Migrations
{
    /// <inheritdoc />
    public partial class updatetransactiontableagain : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "Category",
                schema: "finance",
                table: "Transactions");

            migrationBuilder.DropColumn(
                name: "SubCategory",
                schema: "finance",
                table: "Transactions");

            migrationBuilder.DropColumn(
                name: "TransactionFlow",
                schema: "finance",
                table: "Transactions");

            migrationBuilder.DropColumn(
                name: "TransactionType",
                schema: "finance",
                table: "Transactions");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "Category",
                schema: "finance",
                table: "Transactions",
                type: "nvarchar(max)",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "SubCategory",
                schema: "finance",
                table: "Transactions",
                type: "nvarchar(max)",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "TransactionFlow",
                schema: "finance",
                table: "Transactions",
                type: "nvarchar(max)",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "TransactionType",
                schema: "finance",
                table: "Transactions",
                type: "nvarchar(max)",
                nullable: true);
        }
    }
}
