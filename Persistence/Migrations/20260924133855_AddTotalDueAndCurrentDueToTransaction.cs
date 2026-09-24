using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddTotalDueAndCurrentDueToTransaction : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<decimal>(
                name: "CurrentDue",
                schema: "finance",
                table: "Transactions",
                type: "decimal(10,2)",
                nullable: true);

            migrationBuilder.AddColumn<decimal>(
                name: "TotalDue",
                schema: "finance",
                table: "Transactions",
                type: "decimal(10,2)",
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "CurrentDue",
                schema: "finance",
                table: "Transactions");

            migrationBuilder.DropColumn(
                name: "TotalDue",
                schema: "finance",
                table: "Transactions");
        }
    }
}
