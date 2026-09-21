using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddBankToTransactionAndSourceTypeToBankTransaction : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "BankId",
                schema: "finance",
                table: "Transactions",
                type: "int",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "SourceType",
                table: "BankTransactions",
                type: "nvarchar(max)",
                nullable: true);

            migrationBuilder.AddColumn<Guid>(
                name: "TransactionId",
                table: "BankTransactions",
                type: "uniqueidentifier",
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_Transactions_BankId",
                schema: "finance",
                table: "Transactions",
                column: "BankId");

            migrationBuilder.AddForeignKey(
                name: "FK_Transactions_Banks_BankId",
                schema: "finance",
                table: "Transactions",
                column: "BankId",
                principalTable: "Banks",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Transactions_Banks_BankId",
                schema: "finance",
                table: "Transactions");

            migrationBuilder.DropIndex(
                name: "IX_Transactions_BankId",
                schema: "finance",
                table: "Transactions");

            migrationBuilder.DropColumn(
                name: "BankId",
                schema: "finance",
                table: "Transactions");

            migrationBuilder.DropColumn(
                name: "SourceType",
                table: "BankTransactions");

            migrationBuilder.DropColumn(
                name: "TransactionId",
                table: "BankTransactions");
        }
    }
}
