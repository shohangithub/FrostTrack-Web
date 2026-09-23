using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddCollectedAmountToDeliveryAndDeliveryDetail : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<decimal>(
                name: "CollectedAmount",
                schema: "product",
                table: "DeliveryDetail",
                type: "decimal(10,2)",
                nullable: false,
                defaultValue: 0m);

            migrationBuilder.AddColumn<decimal>(
                name: "CollectedAmount",
                schema: "product",
                table: "Delivery",
                type: "decimal(10,2)",
                nullable: false,
                defaultValue: 0m);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "CollectedAmount",
                schema: "product",
                table: "DeliveryDetail");

            migrationBuilder.DropColumn(
                name: "CollectedAmount",
                schema: "product",
                table: "Delivery");
        }
    }
}
