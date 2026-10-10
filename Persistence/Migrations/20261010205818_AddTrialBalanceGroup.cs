using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddTrialBalanceGroup : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<Guid>(
                name: "TrialBalanceGroupId",
                schema: "finance",
                table: "TransactionHeads",
                type: "uniqueidentifier",
                nullable: true);

            migrationBuilder.CreateTable(
                name: "TrialBalanceGroups",
                schema: "finance",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Code = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    Name = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: false),
                    SortOrder = table.Column<int>(type: "int", nullable: false),
                    Description = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true),
                    IsActive = table.Column<bool>(type: "bit", nullable: false),
                    TenantId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    CreatedById = table.Column<int>(type: "int", nullable: false),
                    CreatedTime = table.Column<DateTime>(type: "datetime2", nullable: false),
                    LastUpdatedById = table.Column<int>(type: "int", nullable: true),
                    LastUpdatedTime = table.Column<DateTime>(type: "datetime2", nullable: true),
                    IsDeleted = table.Column<bool>(type: "bit", nullable: false),
                    DeletedAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    DeletedById = table.Column<int>(type: "int", nullable: true),
                    IsArchived = table.Column<bool>(type: "bit", nullable: false),
                    ArchivedAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    ArchivedById = table.Column<int>(type: "int", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_TrialBalanceGroups", x => x.Id);
                });

            migrationBuilder.CreateIndex(
                name: "IX_TransactionHeads_TrialBalanceGroupId",
                schema: "finance",
                table: "TransactionHeads",
                column: "TrialBalanceGroupId");

            migrationBuilder.CreateIndex(
                name: "IX_TrialBalanceGroups_TenantId",
                schema: "finance",
                table: "TrialBalanceGroups",
                column: "TenantId");

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

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_TransactionHeads_TrialBalanceGroups_TrialBalanceGroupId",
                schema: "finance",
                table: "TransactionHeads");

            migrationBuilder.DropTable(
                name: "TrialBalanceGroups",
                schema: "finance");

            migrationBuilder.DropIndex(
                name: "IX_TransactionHeads_TrialBalanceGroupId",
                schema: "finance",
                table: "TransactionHeads");

            migrationBuilder.DropColumn(
                name: "TrialBalanceGroupId",
                schema: "finance",
                table: "TransactionHeads");
        }
    }
}
