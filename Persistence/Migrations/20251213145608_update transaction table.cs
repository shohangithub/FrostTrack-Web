using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Persistence.Migrations
{
    /// <inheritdoc />
    public partial class updatetransactiontable : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AlterColumn<string>(
                name: "TransactionType",
                schema: "finance",
                table: "Transactions",
                type: "nvarchar(max)",
                nullable: true,
                oldClrType: typeof(string),
                oldType: "nvarchar(max)");

            migrationBuilder.AlterColumn<string>(
                name: "TransactionFlow",
                schema: "finance",
                table: "Transactions",
                type: "nvarchar(max)",
                nullable: true,
                oldClrType: typeof(string),
                oldType: "nvarchar(max)");

            migrationBuilder.AddColumn<Guid>(
                name: "TransactionHeadId",
                schema: "finance",
                table: "Transactions",
                type: "uniqueidentifier",
                nullable: false,
                defaultValue: new Guid("00000000-0000-0000-0000-000000000000"));

            migrationBuilder.CreateIndex(
                name: "IX_Transactions_TransactionHeadId",
                schema: "finance",
                table: "Transactions",
                column: "TransactionHeadId");

            migrationBuilder.AddForeignKey(
                name: "FK_Transactions_TransactionHeads_TransactionHeadId",
                schema: "finance",
                table: "Transactions",
                column: "TransactionHeadId",
                principalSchema: "finance",
                principalTable: "TransactionHeads",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Transactions_TransactionHeads_TransactionHeadId",
                schema: "finance",
                table: "Transactions");

            migrationBuilder.DropIndex(
                name: "IX_Transactions_TransactionHeadId",
                schema: "finance",
                table: "Transactions");

            migrationBuilder.DropColumn(
                name: "TransactionHeadId",
                schema: "finance",
                table: "Transactions");

            migrationBuilder.AlterColumn<string>(
                name: "TransactionType",
                schema: "finance",
                table: "Transactions",
                type: "nvarchar(max)",
                nullable: false,
                defaultValue: "",
                oldClrType: typeof(string),
                oldType: "nvarchar(max)",
                oldNullable: true);

            migrationBuilder.AlterColumn<string>(
                name: "TransactionFlow",
                schema: "finance",
                table: "Transactions",
                type: "nvarchar(max)",
                nullable: false,
                defaultValue: "",
                oldClrType: typeof(string),
                oldType: "nvarchar(max)",
                oldNullable: true);
        }
    }
}
