using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Persistence.Migrations
{
    /// <inheritdoc />
    public partial class archiveproperties : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<DateTime>(
                name: "ArchivedAt",
                schema: "product",
                table: "DeliveryDetail",
                type: "datetime2",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "ArchivedById",
                schema: "product",
                table: "DeliveryDetail",
                type: "int",
                nullable: true);

            migrationBuilder.AddColumn<bool>(
                name: "IsArchived",
                schema: "product",
                table: "DeliveryDetail",
                type: "bit",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<DateTime>(
                name: "ArchivedAt",
                schema: "product",
                table: "BookingDetail",
                type: "datetime2",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "ArchivedById",
                schema: "product",
                table: "BookingDetail",
                type: "int",
                nullable: true);

            migrationBuilder.AddColumn<bool>(
                name: "IsArchived",
                schema: "product",
                table: "BookingDetail",
                type: "bit",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<DateTime>(
                name: "ArchivedAt",
                schema: "product",
                table: "Booking",
                type: "datetime2",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "ArchivedById",
                schema: "product",
                table: "Booking",
                type: "int",
                nullable: true);

            migrationBuilder.AddColumn<bool>(
                name: "IsArchived",
                schema: "product",
                table: "Booking",
                type: "bit",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<DateTime>(
                name: "ArchivedAt",
                table: "BankTransactions",
                type: "datetime2",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "ArchivedBy",
                table: "BankTransactions",
                type: "nvarchar(max)",
                nullable: true);

            migrationBuilder.AddColumn<bool>(
                name: "IsArchived",
                table: "BankTransactions",
                type: "bit",
                nullable: false,
                defaultValue: false);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "ArchivedAt",
                schema: "product",
                table: "DeliveryDetail");

            migrationBuilder.DropColumn(
                name: "ArchivedById",
                schema: "product",
                table: "DeliveryDetail");

            migrationBuilder.DropColumn(
                name: "IsArchived",
                schema: "product",
                table: "DeliveryDetail");

            migrationBuilder.DropColumn(
                name: "ArchivedAt",
                schema: "product",
                table: "BookingDetail");

            migrationBuilder.DropColumn(
                name: "ArchivedById",
                schema: "product",
                table: "BookingDetail");

            migrationBuilder.DropColumn(
                name: "IsArchived",
                schema: "product",
                table: "BookingDetail");

            migrationBuilder.DropColumn(
                name: "ArchivedAt",
                schema: "product",
                table: "Booking");

            migrationBuilder.DropColumn(
                name: "ArchivedById",
                schema: "product",
                table: "Booking");

            migrationBuilder.DropColumn(
                name: "IsArchived",
                schema: "product",
                table: "Booking");

            migrationBuilder.DropColumn(
                name: "ArchivedAt",
                table: "BankTransactions");

            migrationBuilder.DropColumn(
                name: "ArchivedBy",
                table: "BankTransactions");

            migrationBuilder.DropColumn(
                name: "IsArchived",
                table: "BankTransactions");
        }
    }
}
