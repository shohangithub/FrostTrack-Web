using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddBillingStartDate : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<DateTime>(
                name: "BillingStartDate",
                table: "Companies",
                type: "datetime2",
                nullable: true);

            // The application went live on 01-Jul-2026: months that ended earlier were billed outside the system
            migrationBuilder.Sql("UPDATE [Companies] SET [BillingStartDate] = '2026-07-01' WHERE [BillingStartDate] IS NULL");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "BillingStartDate",
                table: "Companies");
        }
    }
}
