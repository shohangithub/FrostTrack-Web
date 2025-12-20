using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddCascadeDeleteToDeliveryDetail : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_DeliveryDetail_Delivery_DeliveryId",
                schema: "product",
                table: "DeliveryDetail");

            migrationBuilder.AddForeignKey(
                name: "FK_DeliveryDetail_Delivery_DeliveryId",
                schema: "product",
                table: "DeliveryDetail",
                column: "DeliveryId",
                principalSchema: "product",
                principalTable: "Delivery",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_DeliveryDetail_Delivery_DeliveryId",
                schema: "product",
                table: "DeliveryDetail");

            migrationBuilder.AddForeignKey(
                name: "FK_DeliveryDetail_Delivery_DeliveryId",
                schema: "product",
                table: "DeliveryDetail",
                column: "DeliveryId",
                principalSchema: "product",
                principalTable: "Delivery",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);
        }
    }
}
