using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Persistence.Migrations
{
    /// <inheritdoc />
    public partial class RemoveEntityNameEntityIdColumns : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "EntityId",
                schema: "finance",
                table: "Transactions");

            migrationBuilder.DropColumn(
                name: "EntityName",
                schema: "finance",
                table: "Transactions");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "EntityId",
                schema: "finance",
                table: "Transactions",
                type: "nvarchar(500)",
                maxLength: 500,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "EntityName",
                schema: "finance",
                table: "Transactions",
                type: "nvarchar(100)",
                maxLength: 100,
                nullable: true);
        }
    }
}
