using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Persistence.Migrations
{
    /// <inheritdoc />
    public partial class UpdateDeliverytable : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "DiscountAmount",
                schema: "product",
                table: "Delivery");

            migrationBuilder.DropColumn(
                name: "PaidAmount",
                schema: "product",
                table: "Delivery");

            migrationBuilder.AlterColumn<int>(
                name: "UnitConversionId",
                schema: "product",
                table: "Stocks",
                type: "int",
                nullable: true,
                oldClrType: typeof(int),
                oldType: "int");

            migrationBuilder.AddColumn<int>(
                name: "UnitId",
                schema: "product",
                table: "Stocks",
                type: "int",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<DateTime>(
                name: "ArchivedAt",
                schema: "product",
                table: "Delivery",
                type: "datetime2",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "ArchivedById",
                schema: "product",
                table: "Delivery",
                type: "int",
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "DeletedAt",
                schema: "product",
                table: "Delivery",
                type: "datetime2",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "DeletedById",
                schema: "product",
                table: "Delivery",
                type: "int",
                nullable: true);

            migrationBuilder.AddColumn<bool>(
                name: "IsArchived",
                schema: "product",
                table: "Delivery",
                type: "bit",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<bool>(
                name: "IsDeleted",
                schema: "product",
                table: "Delivery",
                type: "bit",
                nullable: false,
                defaultValue: false);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "UnitId",
                schema: "product",
                table: "Stocks");

            migrationBuilder.DropColumn(
                name: "ArchivedAt",
                schema: "product",
                table: "Delivery");

            migrationBuilder.DropColumn(
                name: "ArchivedById",
                schema: "product",
                table: "Delivery");

            migrationBuilder.DropColumn(
                name: "DeletedAt",
                schema: "product",
                table: "Delivery");

            migrationBuilder.DropColumn(
                name: "DeletedById",
                schema: "product",
                table: "Delivery");

            migrationBuilder.DropColumn(
                name: "IsArchived",
                schema: "product",
                table: "Delivery");

            migrationBuilder.DropColumn(
                name: "IsDeleted",
                schema: "product",
                table: "Delivery");

            migrationBuilder.AlterColumn<int>(
                name: "UnitConversionId",
                schema: "product",
                table: "Stocks",
                type: "int",
                nullable: false,
                defaultValue: 0,
                oldClrType: typeof(int),
                oldType: "int",
                oldNullable: true);

            migrationBuilder.AddColumn<decimal>(
                name: "DiscountAmount",
                schema: "product",
                table: "Delivery",
                type: "decimal(10,2)",
                nullable: false,
                defaultValue: 0m);

            migrationBuilder.AddColumn<decimal>(
                name: "PaidAmount",
                schema: "product",
                table: "Delivery",
                type: "decimal(10,2)",
                nullable: false,
                defaultValue: 0m);
        }
    }
}
