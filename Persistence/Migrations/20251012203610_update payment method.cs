using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Persistence.Migrations
{
    /// <inheritdoc />
    public partial class updatepaymentmethod : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "Color",
                schema: "general",
                table: "PaymentMethods");

            migrationBuilder.RenameColumn(
                name: "Name",
                schema: "general",
                table: "PaymentMethods",
                newName: "MethodName");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.RenameColumn(
                name: "MethodName",
                schema: "general",
                table: "PaymentMethods",
                newName: "Name");

            migrationBuilder.AddColumn<string>(
                name: "Color",
                schema: "general",
                table: "PaymentMethods",
                type: "nvarchar(max)",
                nullable: true);
        }
    }
}
