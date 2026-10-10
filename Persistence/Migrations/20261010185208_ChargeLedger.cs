using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Persistence.Migrations
{
    /// <inheritdoc />
    public partial class ChargeLedger : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_RecurringChargeEntries_BookingDetail_BookingDetailId",
                schema: "product",
                table: "RecurringChargeEntries");

            // The table was created as AccrualEntries; this index may not exist in every database
            migrationBuilder.Sql(
                "IF EXISTS (SELECT 1 FROM sys.indexes WHERE name = 'IX_RecurringChargeEntries_BookingDetailId' AND object_id = OBJECT_ID('[product].[RecurringChargeEntries]')) " +
                "DROP INDEX [IX_RecurringChargeEntries_BookingDetailId] ON [product].[RecurringChargeEntries]");

            // The table was created as AccrualEntries; this index may not exist in every database
            migrationBuilder.Sql(
                "IF EXISTS (SELECT 1 FROM sys.indexes WHERE name = 'IX_RecurringChargeEntries_BookingId' AND object_id = OBJECT_ID('[product].[RecurringChargeEntries]')) " +
                "DROP INDEX [IX_RecurringChargeEntries_BookingId] ON [product].[RecurringChargeEntries]");

            migrationBuilder.AlterColumn<Guid>(
                name: "BookingDetailId",
                schema: "product",
                table: "RecurringChargeEntries",
                type: "uniqueidentifier",
                nullable: true,
                oldClrType: typeof(Guid),
                oldType: "uniqueidentifier");

            migrationBuilder.AddColumn<string>(
                name: "ChargeType",
                schema: "product",
                table: "RecurringChargeEntries",
                type: "nvarchar(30)",
                maxLength: 30,
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<int>(
                name: "CreatedById",
                schema: "product",
                table: "RecurringChargeEntries",
                type: "int",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "CycleNo",
                schema: "product",
                table: "RecurringChargeEntries",
                type: "int",
                nullable: true);

            migrationBuilder.AddColumn<Guid>(
                name: "DeliveryId",
                schema: "product",
                table: "RecurringChargeEntries",
                type: "uniqueidentifier",
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "EntryDate",
                schema: "product",
                table: "RecurringChargeEntries",
                type: "datetime2",
                nullable: false,
                defaultValue: new DateTime(1, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified));

            // Entries created before the charge ledger are kept for history but never counted
            migrationBuilder.Sql(
                "UPDATE [product].[RecurringChargeEntries] SET [ChargeType] = 'LEGACY', [EntryDate] = [BillPeriodFrom]");

            migrationBuilder.CreateIndex(
                name: "IX_RecurringChargeEntries_BookingDetailId_CycleNo",
                schema: "product",
                table: "RecurringChargeEntries",
                columns: new[] { "BookingDetailId", "CycleNo" },
                unique: true,
                filter: "[Source] = 'POSTED' AND [ChargeType] = 'RENT_CYCLE' AND [BookingDetailId] IS NOT NULL");

            migrationBuilder.CreateIndex(
                name: "IX_RecurringChargeEntries_BookingId_EntryDate",
                schema: "product",
                table: "RecurringChargeEntries",
                columns: new[] { "BookingId", "EntryDate" });

            migrationBuilder.AddForeignKey(
                name: "FK_RecurringChargeEntries_BookingDetail_BookingDetailId",
                schema: "product",
                table: "RecurringChargeEntries",
                column: "BookingDetailId",
                principalSchema: "product",
                principalTable: "BookingDetail",
                principalColumn: "Id",
                onDelete: ReferentialAction.SetNull);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_RecurringChargeEntries_BookingDetail_BookingDetailId",
                schema: "product",
                table: "RecurringChargeEntries");

            migrationBuilder.DropIndex(
                name: "IX_RecurringChargeEntries_BookingDetailId_CycleNo",
                schema: "product",
                table: "RecurringChargeEntries");

            migrationBuilder.DropIndex(
                name: "IX_RecurringChargeEntries_BookingId_EntryDate",
                schema: "product",
                table: "RecurringChargeEntries");

            migrationBuilder.DropColumn(
                name: "ChargeType",
                schema: "product",
                table: "RecurringChargeEntries");

            migrationBuilder.DropColumn(
                name: "CreatedById",
                schema: "product",
                table: "RecurringChargeEntries");

            migrationBuilder.DropColumn(
                name: "CycleNo",
                schema: "product",
                table: "RecurringChargeEntries");

            migrationBuilder.DropColumn(
                name: "DeliveryId",
                schema: "product",
                table: "RecurringChargeEntries");

            migrationBuilder.DropColumn(
                name: "EntryDate",
                schema: "product",
                table: "RecurringChargeEntries");

            migrationBuilder.AlterColumn<Guid>(
                name: "BookingDetailId",
                schema: "product",
                table: "RecurringChargeEntries",
                type: "uniqueidentifier",
                nullable: false,
                defaultValue: new Guid("00000000-0000-0000-0000-000000000000"),
                oldClrType: typeof(Guid),
                oldType: "uniqueidentifier",
                oldNullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_RecurringChargeEntries_BookingDetailId",
                schema: "product",
                table: "RecurringChargeEntries",
                column: "BookingDetailId");

            migrationBuilder.CreateIndex(
                name: "IX_RecurringChargeEntries_BookingId",
                schema: "product",
                table: "RecurringChargeEntries",
                column: "BookingId");

            migrationBuilder.AddForeignKey(
                name: "FK_RecurringChargeEntries_BookingDetail_BookingDetailId",
                schema: "product",
                table: "RecurringChargeEntries",
                column: "BookingDetailId",
                principalSchema: "product",
                principalTable: "BookingDetail",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);
        }
    }
}
