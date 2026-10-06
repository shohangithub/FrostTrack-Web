using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddEditDeleteWindowToCompany : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "EditDeleteAllowedDays",
                table: "Companies",
                type: "int",
                nullable: false,
                defaultValue: 90);

            migrationBuilder.AddColumn<bool>(
                name: "RestrictEditDelete",
                table: "Companies",
                type: "bit",
                nullable: false,
                defaultValue: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "EditDeleteAllowedDays",
                table: "Companies");

            migrationBuilder.DropColumn(
                name: "RestrictEditDelete",
                table: "Companies");
        }
    }
}
