using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Persistence.Migrations
{
    /// <inheritdoc />
    public partial class RenameTrialBalanceGroupToTransactionGroup : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            // Rename in place (keeps existing groups and head mappings)
            migrationBuilder.DropForeignKey(
                name: "FK_TransactionHeads_TrialBalanceGroups_TrialBalanceGroupId",
                schema: "finance",
                table: "TransactionHeads");

            migrationBuilder.RenameTable(
                name: "TrialBalanceGroups",
                schema: "finance",
                newName: "TransactionGroups",
                newSchema: "finance");

            migrationBuilder.Sql("EXEC sp_rename N'finance.TransactionGroups.PK_TrialBalanceGroups', N'PK_TransactionGroups', N'INDEX';");

            migrationBuilder.RenameIndex(
                name: "IX_TrialBalanceGroups_TenantId",
                schema: "finance",
                table: "TransactionGroups",
                newName: "IX_TransactionGroups_TenantId");

            migrationBuilder.RenameColumn(
                name: "TrialBalanceGroupId",
                schema: "finance",
                table: "TransactionHeads",
                newName: "TransactionGroupId");

            migrationBuilder.RenameIndex(
                name: "IX_TransactionHeads_TrialBalanceGroupId",
                schema: "finance",
                table: "TransactionHeads",
                newName: "IX_TransactionHeads_TransactionGroupId");

            migrationBuilder.AddForeignKey(
                name: "FK_TransactionHeads_TransactionGroups_TransactionGroupId",
                schema: "finance",
                table: "TransactionHeads",
                column: "TransactionGroupId",
                principalSchema: "finance",
                principalTable: "TransactionGroups",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            // Rename in place (keeps existing groups and head mappings)
            migrationBuilder.DropForeignKey(
                name: "FK_TransactionHeads_TransactionGroups_TransactionGroupId",
                schema: "finance",
                table: "TransactionHeads");

            migrationBuilder.RenameTable(
                name: "TransactionGroups",
                schema: "finance",
                newName: "TrialBalanceGroups",
                newSchema: "finance");

            migrationBuilder.Sql("EXEC sp_rename N'finance.TrialBalanceGroups.PK_TransactionGroups', N'PK_TrialBalanceGroups', N'INDEX';");

            migrationBuilder.RenameIndex(
                name: "IX_TransactionGroups_TenantId",
                schema: "finance",
                table: "TrialBalanceGroups",
                newName: "IX_TrialBalanceGroups_TenantId");

            migrationBuilder.RenameColumn(
                name: "TransactionGroupId",
                schema: "finance",
                table: "TransactionHeads",
                newName: "TrialBalanceGroupId");

            migrationBuilder.RenameIndex(
                name: "IX_TransactionHeads_TransactionGroupId",
                schema: "finance",
                table: "TransactionHeads",
                newName: "IX_TransactionHeads_TrialBalanceGroupId");

            migrationBuilder.AddForeignKey(
                name: "FK_TransactionHeads_TrialBalanceGroups_TrialBalanceGroupId",
                schema: "finance",
                table: "TransactionHeads",
                column: "TrialBalanceGroupId",
                principalSchema: "finance",
                principalTable: "TrialBalanceGroups",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);
        }
    }
}
