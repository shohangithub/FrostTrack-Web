using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Persistence.Migrations
{
    /// <inheritdoc />
    public partial class RenameAccrualToRecurringCharge : Migration
    {
        // -- Up ------------------------------------------------------------------
        // Renames tables/columns in-place so no data is lost.
        //   AccrualRuns         ? RecurringChargeRuns
        //   AccrualEntries      ? RecurringChargeEntries
        //   AccrualEntries.AccrualRunId          ? RecurringChargeRunId
        //   AccrualRuns.TotalAccrualAmount        ? TotalRecurringChargeAmount
        //   BookingDetail.LastAccrualDate         ? LastRecurringChargeDate
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            // 1. Drop FKs on AccrualEntries (must drop before rename)
            migrationBuilder.DropForeignKey(
                name: "FK_AccrualEntries_AccrualRuns_AccrualRunId",
                schema: "product",
                table: "AccrualEntries");

            migrationBuilder.DropForeignKey(
                name: "FK_AccrualEntries_BookingDetail_BookingDetailId",
                schema: "product",
                table: "AccrualEntries");

            migrationBuilder.DropForeignKey(
                name: "FK_AccrualEntries_Booking_BookingId",
                schema: "product",
                table: "AccrualEntries");

            // 2. Drop indexes (must drop before column/table rename on SQL Server)
            migrationBuilder.DropIndex(
                name: "IX_AccrualEntries_AccrualRunId",
                schema: "product",
                table: "AccrualEntries");

            migrationBuilder.DropIndex(
                name: "IX_AccrualEntries_BookingDetailId",
                schema: "product",
                table: "AccrualEntries");

            migrationBuilder.DropIndex(
                name: "IX_AccrualEntries_BookingId",
                schema: "product",
                table: "AccrualEntries");

            migrationBuilder.DropIndex(
                name: "IX_AccrualRuns_StartedAt",
                schema: "product",
                table: "AccrualRuns");

            migrationBuilder.DropIndex(
                name: "IX_AccrualRuns_TenantId",
                schema: "product",
                table: "AccrualRuns");

            // 3. Rename FK column on AccrualEntries
            migrationBuilder.RenameColumn(
                name: "AccrualRunId",
                schema: "product",
                table: "AccrualEntries",
                newName: "RecurringChargeRunId");

            // 4. Rename amount column on AccrualRuns
            migrationBuilder.RenameColumn(
                name: "TotalAccrualAmount",
                schema: "product",
                table: "AccrualRuns",
                newName: "TotalRecurringChargeAmount");

            // 5. Rename tables
            migrationBuilder.RenameTable(
                name: "AccrualEntries",
                schema: "product",
                newName: "RecurringChargeEntries",
                newSchema: "product");

            migrationBuilder.RenameTable(
                name: "AccrualRuns",
                schema: "product",
                newName: "RecurringChargeRuns",
                newSchema: "product");

            // 6. Rename BookingDetail column
            migrationBuilder.RenameColumn(
                name: "LastAccrualDate",
                schema: "product",
                table: "BookingDetail",
                newName: "LastRecurringChargeDate");

            // 7. Recreate indexes with new names
            migrationBuilder.CreateIndex(
                name: "IX_RecurringChargeEntries_RecurringChargeRunId",
                schema: "product",
                table: "RecurringChargeEntries",
                column: "RecurringChargeRunId");

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

            migrationBuilder.CreateIndex(
                name: "IX_RecurringChargeRuns_StartedAt",
                schema: "product",
                table: "RecurringChargeRuns",
                column: "StartedAt");

            migrationBuilder.CreateIndex(
                name: "IX_RecurringChargeRuns_TenantId",
                schema: "product",
                table: "RecurringChargeRuns",
                column: "TenantId");

            // 8. Recreate FKs with new names
            migrationBuilder.AddForeignKey(
                name: "FK_RecurringChargeEntries_RecurringChargeRuns_RecurringChargeRunId",
                schema: "product",
                table: "RecurringChargeEntries",
                column: "RecurringChargeRunId",
                principalSchema: "product",
                principalTable: "RecurringChargeRuns",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_RecurringChargeEntries_BookingDetail_BookingDetailId",
                schema: "product",
                table: "RecurringChargeEntries",
                column: "BookingDetailId",
                principalSchema: "product",
                principalTable: "BookingDetail",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_RecurringChargeEntries_Booking_BookingId",
                schema: "product",
                table: "RecurringChargeEntries",
                column: "BookingId",
                principalSchema: "product",
                principalTable: "Booking",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            // Reverse: drop new FKs, indexes, rename back
            migrationBuilder.DropForeignKey(
                name: "FK_RecurringChargeEntries_RecurringChargeRuns_RecurringChargeRunId",
                schema: "product",
                table: "RecurringChargeEntries");

            migrationBuilder.DropForeignKey(
                name: "FK_RecurringChargeEntries_BookingDetail_BookingDetailId",
                schema: "product",
                table: "RecurringChargeEntries");

            migrationBuilder.DropForeignKey(
                name: "FK_RecurringChargeEntries_Booking_BookingId",
                schema: "product",
                table: "RecurringChargeEntries");

            migrationBuilder.DropIndex(
                name: "IX_RecurringChargeEntries_RecurringChargeRunId",
                schema: "product",
                table: "RecurringChargeEntries");

            migrationBuilder.DropIndex(
                name: "IX_RecurringChargeEntries_BookingDetailId",
                schema: "product",
                table: "RecurringChargeEntries");

            migrationBuilder.DropIndex(
                name: "IX_RecurringChargeEntries_BookingId",
                schema: "product",
                table: "RecurringChargeEntries");

            migrationBuilder.DropIndex(
                name: "IX_RecurringChargeRuns_StartedAt",
                schema: "product",
                table: "RecurringChargeRuns");

            migrationBuilder.DropIndex(
                name: "IX_RecurringChargeRuns_TenantId",
                schema: "product",
                table: "RecurringChargeRuns");

            migrationBuilder.RenameColumn(
                name: "RecurringChargeRunId",
                schema: "product",
                table: "RecurringChargeEntries",
                newName: "AccrualRunId");

            migrationBuilder.RenameColumn(
                name: "TotalRecurringChargeAmount",
                schema: "product",
                table: "RecurringChargeRuns",
                newName: "TotalAccrualAmount");

            migrationBuilder.RenameTable(
                name: "RecurringChargeEntries",
                schema: "product",
                newName: "AccrualEntries",
                newSchema: "product");

            migrationBuilder.RenameTable(
                name: "RecurringChargeRuns",
                schema: "product",
                newName: "AccrualRuns",
                newSchema: "product");

            migrationBuilder.RenameColumn(
                name: "LastRecurringChargeDate",
                schema: "product",
                table: "BookingDetail",
                newName: "LastAccrualDate");

            migrationBuilder.CreateIndex(
                name: "IX_AccrualEntries_AccrualRunId",
                schema: "product",
                table: "AccrualEntries",
                column: "AccrualRunId");

            migrationBuilder.CreateIndex(
                name: "IX_AccrualEntries_BookingDetailId",
                schema: "product",
                table: "AccrualEntries",
                column: "BookingDetailId");

            migrationBuilder.CreateIndex(
                name: "IX_AccrualEntries_BookingId",
                schema: "product",
                table: "AccrualEntries",
                column: "BookingId");

            migrationBuilder.CreateIndex(
                name: "IX_AccrualRuns_StartedAt",
                schema: "product",
                table: "AccrualRuns",
                column: "StartedAt");

            migrationBuilder.CreateIndex(
                name: "IX_AccrualRuns_TenantId",
                schema: "product",
                table: "AccrualRuns",
                column: "TenantId");

            migrationBuilder.AddForeignKey(
                name: "FK_AccrualEntries_AccrualRuns_AccrualRunId",
                schema: "product",
                table: "AccrualEntries",
                column: "AccrualRunId",
                principalSchema: "product",
                principalTable: "AccrualRuns",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_AccrualEntries_BookingDetail_BookingDetailId",
                schema: "product",
                table: "AccrualEntries",
                column: "BookingDetailId",
                principalSchema: "product",
                principalTable: "BookingDetail",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_AccrualEntries_Booking_BookingId",
                schema: "product",
                table: "AccrualEntries",
                column: "BookingId",
                principalSchema: "product",
                principalTable: "Booking",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);
        }
    }
}
