using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddProductDeliveryTables : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "ProductDeliveries",
                schema: "product",
                columns: table => new
                {
                    Id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    DeliveryNumber = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    DeliveryDate = table.Column<DateTime>(type: "datetime2", nullable: false),
                    CustomerId = table.Column<int>(type: "int", nullable: false),
                    BranchId = table.Column<int>(type: "int", nullable: false),
                    Subtotal = table.Column<decimal>(type: "decimal(10,2)", nullable: false),
                    VatAmount = table.Column<decimal>(type: "decimal(10,2)", nullable: false),
                    DiscountPercent = table.Column<float>(type: "real", nullable: false),
                    DiscountAmount = table.Column<decimal>(type: "decimal(10,2)", nullable: false),
                    OtherCost = table.Column<decimal>(type: "decimal(10,2)", nullable: false),
                    TotalAmount = table.Column<decimal>(type: "decimal(10,2)", nullable: false),
                    PaidAmount = table.Column<decimal>(type: "decimal(10,2)", nullable: false),
                    Notes = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    TenantId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    CreatedById = table.Column<int>(type: "int", nullable: false),
                    CreatedTime = table.Column<DateTime>(type: "datetime", nullable: false),
                    LastUpdatedById = table.Column<int>(type: "int", nullable: true),
                    LastUpdatedTime = table.Column<DateTime>(type: "datetime2", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ProductDeliveries", x => x.Id);
                    table.ForeignKey(
                        name: "FK_ProductDeliveries_Branches_BranchId",
                        column: x => x.BranchId,
                        principalTable: "Branches",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_ProductDeliveries_Customers_CustomerId",
                        column: x => x.CustomerId,
                        principalTable: "Customers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "ProductDeliveryDetails",
                schema: "product",
                columns: table => new
                {
                    Id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    ProductDeliveryId = table.Column<long>(type: "bigint", nullable: false),
                    ProductId = table.Column<int>(type: "int", nullable: false),
                    DeliveryUnitId = table.Column<int>(type: "int", nullable: false),
                    DeliveryQuantity = table.Column<decimal>(type: "decimal(18,3)", nullable: false),
                    BookingRate = table.Column<decimal>(type: "decimal(10,2)", nullable: false),
                    DeliveryAmount = table.Column<decimal>(type: "decimal(10,2)", nullable: false),
                    CreatedTime = table.Column<DateTime>(type: "datetime", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ProductDeliveryDetails", x => x.Id);
                    table.ForeignKey(
                        name: "FK_ProductDeliveryDetails_BaseUnits_DeliveryUnitId",
                        column: x => x.DeliveryUnitId,
                        principalSchema: "lookup",
                        principalTable: "BaseUnits",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_ProductDeliveryDetails_ProductDeliveries_ProductDeliveryId",
                        column: x => x.ProductDeliveryId,
                        principalSchema: "product",
                        principalTable: "ProductDeliveries",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_ProductDeliveryDetails_Products_ProductId",
                        column: x => x.ProductId,
                        principalSchema: "product",
                        principalTable: "Products",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "IX_ProductDeliveries_BranchId",
                schema: "product",
                table: "ProductDeliveries",
                column: "BranchId");

            migrationBuilder.CreateIndex(
                name: "IX_ProductDeliveries_CustomerId",
                schema: "product",
                table: "ProductDeliveries",
                column: "CustomerId");

            migrationBuilder.CreateIndex(
                name: "IX_ProductDeliveryDetails_DeliveryUnitId",
                schema: "product",
                table: "ProductDeliveryDetails",
                column: "DeliveryUnitId");

            migrationBuilder.CreateIndex(
                name: "IX_ProductDeliveryDetails_ProductDeliveryId",
                schema: "product",
                table: "ProductDeliveryDetails",
                column: "ProductDeliveryId");

            migrationBuilder.CreateIndex(
                name: "IX_ProductDeliveryDetails_ProductId",
                schema: "product",
                table: "ProductDeliveryDetails",
                column: "ProductId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "ProductDeliveryDetails",
                schema: "product");

            migrationBuilder.DropTable(
                name: "ProductDeliveries",
                schema: "product");
        }
    }
}
