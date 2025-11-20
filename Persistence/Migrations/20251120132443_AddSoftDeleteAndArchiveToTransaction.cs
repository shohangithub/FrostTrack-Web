using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddSoftDeleteAndArchiveToTransaction : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<DateTime>(
                name: "ArchivedAt",
                schema: "finance",
                table: "Transactions",
                type: "datetime2",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "ArchivedById",
                schema: "finance",
                table: "Transactions",
                type: "int",
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "DeletedAt",
                schema: "finance",
                table: "Transactions",
                type: "datetime2",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "DeletedById",
                schema: "finance",
                table: "Transactions",
                type: "int",
                nullable: true);

            migrationBuilder.AddColumn<bool>(
                name: "IsArchived",
                schema: "finance",
                table: "Transactions",
                type: "bit",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<bool>(
                name: "IsDeleted",
                schema: "finance",
                table: "Transactions",
                type: "bit",
                nullable: false,
                defaultValue: false);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "ArchivedAt",
                schema: "finance",
                table: "Transactions");

            migrationBuilder.DropColumn(
                name: "ArchivedById",
                schema: "finance",
                table: "Transactions");

            migrationBuilder.DropColumn(
                name: "DeletedAt",
                schema: "finance",
                table: "Transactions");

            migrationBuilder.DropColumn(
                name: "DeletedById",
                schema: "finance",
                table: "Transactions");

            migrationBuilder.DropColumn(
                name: "IsArchived",
                schema: "finance",
                table: "Transactions");

            migrationBuilder.DropColumn(
                name: "IsDeleted",
                schema: "finance",
                table: "Transactions");
        }
    }
}
