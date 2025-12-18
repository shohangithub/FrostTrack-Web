using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddDeliveryPaymentTracking : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<DateTime>(
                name: "PaymentDate",
                schema: "product",
                table: "Delivery",
                type: "datetime2",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "PaymentStatus",
                schema: "product",
                table: "Delivery",
                type: "nvarchar(20)",
                maxLength: 20,
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<Guid>(
                name: "TransactionId",
                schema: "product",
                table: "Delivery",
                type: "uniqueidentifier",
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_Delivery_TransactionId",
                schema: "product",
                table: "Delivery",
                column: "TransactionId");

            migrationBuilder.AddForeignKey(
                name: "FK_Delivery_Transactions_TransactionId",
                schema: "product",
                table: "Delivery",
                column: "TransactionId",
                principalSchema: "finance",
                principalTable: "Transactions",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Delivery_Transactions_TransactionId",
                schema: "product",
                table: "Delivery");

            migrationBuilder.DropIndex(
                name: "IX_Delivery_TransactionId",
                schema: "product",
                table: "Delivery");

            migrationBuilder.DropColumn(
                name: "PaymentDate",
                schema: "product",
                table: "Delivery");

            migrationBuilder.DropColumn(
                name: "PaymentStatus",
                schema: "product",
                table: "Delivery");

            migrationBuilder.DropColumn(
                name: "TransactionId",
                schema: "product",
                table: "Delivery");
        }
    }
}
