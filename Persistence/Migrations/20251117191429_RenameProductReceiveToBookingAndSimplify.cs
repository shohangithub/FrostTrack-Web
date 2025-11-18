using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Persistence.Migrations
{
    /// <inheritdoc />
    public partial class RenameProductReceiveToBookingAndSimplify : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_ProductDeliveryDetails_BaseUnits_DeliveryUnitId",
                schema: "product",
                table: "ProductDeliveryDetails");

            migrationBuilder.DropTable(
                name: "ProductReceiveDetails",
                schema: "product");

            migrationBuilder.DropTable(
                name: "ProductReceives",
                schema: "product");

            migrationBuilder.DropColumn(
                name: "DiscountAmount",
                schema: "product",
                table: "ProductDeliveries");

            migrationBuilder.DropColumn(
                name: "DiscountPercent",
                schema: "product",
                table: "ProductDeliveries");

            migrationBuilder.DropColumn(
                name: "OtherCost",
                schema: "product",
                table: "ProductDeliveries");

            migrationBuilder.DropColumn(
                name: "PaidAmount",
                schema: "product",
                table: "ProductDeliveries");

            migrationBuilder.DropColumn(
                name: "Subtotal",
                schema: "product",
                table: "ProductDeliveries");

            migrationBuilder.DropColumn(
                name: "TotalAmount",
                schema: "product",
                table: "ProductDeliveries");

            migrationBuilder.DropColumn(
                name: "VatAmount",
                schema: "product",
                table: "ProductDeliveries");

            migrationBuilder.RenameColumn(
                name: "DeliveryAmount",
                schema: "product",
                table: "ProductDeliveryDetails",
                newName: "DeliveryRate");

            migrationBuilder.RenameColumn(
                name: "BookingRate",
                schema: "product",
                table: "ProductDeliveryDetails",
                newName: "BaseRate");

            migrationBuilder.AlterColumn<float>(
                name: "DeliveryQuantity",
                schema: "product",
                table: "ProductDeliveryDetails",
                type: "real",
                nullable: false,
                oldClrType: typeof(decimal),
                oldType: "decimal(18,3)");

            migrationBuilder.AlterColumn<long>(
                name: "Id",
                schema: "product",
                table: "ProductDeliveryDetails",
                type: "bigint",
                nullable: false,
                oldClrType: typeof(long),
                oldType: "bigint")
                .Annotation("Relational:ColumnOrder", 0)
                .Annotation("SqlServer:Identity", "1, 1")
                .OldAnnotation("SqlServer:Identity", "1, 1");

            migrationBuilder.AddColumn<decimal>(
                name: "BaseQuantity",
                schema: "product",
                table: "ProductDeliveryDetails",
                type: "decimal(18,2)",
                nullable: false,
                defaultValue: 0m);

            migrationBuilder.AddColumn<int>(
                name: "CreatedById",
                schema: "product",
                table: "ProductDeliveryDetails",
                type: "int",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<int>(
                name: "LastUpdatedById",
                schema: "product",
                table: "ProductDeliveryDetails",
                type: "int",
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "LastUpdatedTime",
                schema: "product",
                table: "ProductDeliveryDetails",
                type: "datetime2",
                nullable: true);

            migrationBuilder.AddColumn<Guid>(
                name: "TenantId",
                schema: "product",
                table: "ProductDeliveryDetails",
                type: "uniqueidentifier",
                nullable: false,
                defaultValue: new Guid("00000000-0000-0000-0000-000000000000"));

            migrationBuilder.CreateTable(
                name: "Bookings",
                schema: "product",
                columns: table => new
                {
                    Id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    BookingNumber = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    BookingDate = table.Column<DateTime>(type: "datetime2", nullable: false),
                    CustomerId = table.Column<int>(type: "int", nullable: false),
                    BranchId = table.Column<int>(type: "int", nullable: false),
                    Notes = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    TenantId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    CreatedById = table.Column<int>(type: "int", nullable: false),
                    CreatedTime = table.Column<DateTime>(type: "datetime", nullable: false),
                    LastUpdatedById = table.Column<int>(type: "int", nullable: true),
                    LastUpdatedTime = table.Column<DateTime>(type: "datetime2", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Bookings", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Bookings_Branches_BranchId",
                        column: x => x.BranchId,
                        principalTable: "Branches",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_Bookings_Customers_CustomerId",
                        column: x => x.CustomerId,
                        principalTable: "Customers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "BookingDetails",
                schema: "product",
                columns: table => new
                {
                    Id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    BookingDetailId = table.Column<long>(type: "bigint", nullable: false),
                    BookingId = table.Column<long>(type: "bigint", nullable: false),
                    ProductId = table.Column<int>(type: "int", nullable: false),
                    BookingUnitId = table.Column<int>(type: "int", nullable: false),
                    BookingQuantity = table.Column<float>(type: "real", nullable: false),
                    BookingRate = table.Column<decimal>(type: "decimal(10,2)", nullable: false),
                    BaseQuantity = table.Column<decimal>(type: "decimal(18,2)", nullable: false),
                    BaseRate = table.Column<decimal>(type: "decimal(10,2)", nullable: false),
                    TenantId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    CreatedById = table.Column<int>(type: "int", nullable: false),
                    CreatedTime = table.Column<DateTime>(type: "datetime", nullable: false),
                    LastUpdatedById = table.Column<int>(type: "int", nullable: true),
                    LastUpdatedTime = table.Column<DateTime>(type: "datetime2", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_BookingDetails", x => x.Id);
                    table.ForeignKey(
                        name: "FK_BookingDetails_Bookings_BookingId",
                        column: x => x.BookingId,
                        principalSchema: "product",
                        principalTable: "Bookings",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_BookingDetails_Products_ProductId",
                        column: x => x.ProductId,
                        principalSchema: "product",
                        principalTable: "Products",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_BookingDetails_UnitConversions_BookingUnitId",
                        column: x => x.BookingUnitId,
                        principalSchema: "lookup",
                        principalTable: "UnitConversions",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "IX_BookingDetails_BookingId",
                schema: "product",
                table: "BookingDetails",
                column: "BookingId");

            migrationBuilder.CreateIndex(
                name: "IX_BookingDetails_BookingUnitId",
                schema: "product",
                table: "BookingDetails",
                column: "BookingUnitId");

            migrationBuilder.CreateIndex(
                name: "IX_BookingDetails_ProductId",
                schema: "product",
                table: "BookingDetails",
                column: "ProductId");

            migrationBuilder.CreateIndex(
                name: "IX_Bookings_BranchId",
                schema: "product",
                table: "Bookings",
                column: "BranchId");

            migrationBuilder.CreateIndex(
                name: "IX_Bookings_CustomerId",
                schema: "product",
                table: "Bookings",
                column: "CustomerId");

            migrationBuilder.AddForeignKey(
                name: "FK_ProductDeliveryDetails_UnitConversions_DeliveryUnitId",
                schema: "product",
                table: "ProductDeliveryDetails",
                column: "DeliveryUnitId",
                principalSchema: "lookup",
                principalTable: "UnitConversions",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_ProductDeliveryDetails_UnitConversions_DeliveryUnitId",
                schema: "product",
                table: "ProductDeliveryDetails");

            migrationBuilder.DropTable(
                name: "BookingDetails",
                schema: "product");

            migrationBuilder.DropTable(
                name: "Bookings",
                schema: "product");

            migrationBuilder.DropColumn(
                name: "BaseQuantity",
                schema: "product",
                table: "ProductDeliveryDetails");

            migrationBuilder.DropColumn(
                name: "CreatedById",
                schema: "product",
                table: "ProductDeliveryDetails");

            migrationBuilder.DropColumn(
                name: "LastUpdatedById",
                schema: "product",
                table: "ProductDeliveryDetails");

            migrationBuilder.DropColumn(
                name: "LastUpdatedTime",
                schema: "product",
                table: "ProductDeliveryDetails");

            migrationBuilder.DropColumn(
                name: "TenantId",
                schema: "product",
                table: "ProductDeliveryDetails");

            migrationBuilder.RenameColumn(
                name: "DeliveryRate",
                schema: "product",
                table: "ProductDeliveryDetails",
                newName: "DeliveryAmount");

            migrationBuilder.RenameColumn(
                name: "BaseRate",
                schema: "product",
                table: "ProductDeliveryDetails",
                newName: "BookingRate");

            migrationBuilder.AlterColumn<decimal>(
                name: "DeliveryQuantity",
                schema: "product",
                table: "ProductDeliveryDetails",
                type: "decimal(18,3)",
                nullable: false,
                oldClrType: typeof(float),
                oldType: "real");

            migrationBuilder.AlterColumn<long>(
                name: "Id",
                schema: "product",
                table: "ProductDeliveryDetails",
                type: "bigint",
                nullable: false,
                oldClrType: typeof(long),
                oldType: "bigint")
                .Annotation("SqlServer:Identity", "1, 1")
                .OldAnnotation("Relational:ColumnOrder", 0)
                .OldAnnotation("SqlServer:Identity", "1, 1");

            migrationBuilder.AddColumn<decimal>(
                name: "DiscountAmount",
                schema: "product",
                table: "ProductDeliveries",
                type: "decimal(10,2)",
                nullable: false,
                defaultValue: 0m);

            migrationBuilder.AddColumn<float>(
                name: "DiscountPercent",
                schema: "product",
                table: "ProductDeliveries",
                type: "real",
                nullable: false,
                defaultValue: 0f);

            migrationBuilder.AddColumn<decimal>(
                name: "OtherCost",
                schema: "product",
                table: "ProductDeliveries",
                type: "decimal(10,2)",
                nullable: false,
                defaultValue: 0m);

            migrationBuilder.AddColumn<decimal>(
                name: "PaidAmount",
                schema: "product",
                table: "ProductDeliveries",
                type: "decimal(10,2)",
                nullable: false,
                defaultValue: 0m);

            migrationBuilder.AddColumn<decimal>(
                name: "Subtotal",
                schema: "product",
                table: "ProductDeliveries",
                type: "decimal(10,2)",
                nullable: false,
                defaultValue: 0m);

            migrationBuilder.AddColumn<decimal>(
                name: "TotalAmount",
                schema: "product",
                table: "ProductDeliveries",
                type: "decimal(10,2)",
                nullable: false,
                defaultValue: 0m);

            migrationBuilder.AddColumn<decimal>(
                name: "VatAmount",
                schema: "product",
                table: "ProductDeliveries",
                type: "decimal(10,2)",
                nullable: false,
                defaultValue: 0m);

            migrationBuilder.CreateTable(
                name: "ProductReceives",
                schema: "product",
                columns: table => new
                {
                    Id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    BranchId = table.Column<int>(type: "int", nullable: false),
                    CustomerId = table.Column<int>(type: "int", nullable: false),
                    CreatedById = table.Column<int>(type: "int", nullable: false),
                    CreatedTime = table.Column<DateTime>(type: "datetime", nullable: false),
                    DiscountAmount = table.Column<decimal>(type: "decimal(10,2)", nullable: false),
                    DiscountPercent = table.Column<float>(type: "real", nullable: false),
                    LastUpdatedById = table.Column<int>(type: "int", nullable: true),
                    LastUpdatedTime = table.Column<DateTime>(type: "datetime2", nullable: true),
                    Notes = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    OtherCost = table.Column<decimal>(type: "decimal(10,2)", nullable: false),
                    PaidAmount = table.Column<decimal>(type: "decimal(10,2)", nullable: false),
                    ReceiveDate = table.Column<DateTime>(type: "datetime2", nullable: false),
                    ReceiveNumber = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    Subtotal = table.Column<decimal>(type: "decimal(10,2)", nullable: false),
                    TenantId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    TotalAmount = table.Column<decimal>(type: "decimal(10,2)", nullable: false),
                    VatAmount = table.Column<decimal>(type: "decimal(10,2)", nullable: false),
                    VatPercent = table.Column<float>(type: "real", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ProductReceives", x => x.Id);
                    table.ForeignKey(
                        name: "FK_ProductReceives_Branches_BranchId",
                        column: x => x.BranchId,
                        principalTable: "Branches",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_ProductReceives_Customers_CustomerId",
                        column: x => x.CustomerId,
                        principalTable: "Customers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "ProductReceiveDetails",
                schema: "product",
                columns: table => new
                {
                    Id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    ProductId = table.Column<int>(type: "int", nullable: false),
                    ProductReceiveId = table.Column<long>(type: "bigint", nullable: false),
                    ReceiveUnitId = table.Column<int>(type: "int", nullable: false),
                    BookingRate = table.Column<decimal>(type: "decimal(10,2)", nullable: false),
                    CreatedById = table.Column<int>(type: "int", nullable: false),
                    CreatedTime = table.Column<DateTime>(type: "datetime", nullable: false),
                    LastUpdatedById = table.Column<int>(type: "int", nullable: true),
                    LastUpdatedTime = table.Column<DateTime>(type: "datetime2", nullable: true),
                    ReceiveAmount = table.Column<decimal>(type: "decimal(10,2)", nullable: false),
                    ReceiveQuantity = table.Column<float>(type: "real", nullable: false),
                    TenantId = table.Column<Guid>(type: "uniqueidentifier", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ProductReceiveDetails", x => x.Id);
                    table.ForeignKey(
                        name: "FK_ProductReceiveDetails_ProductReceives_ProductReceiveId",
                        column: x => x.ProductReceiveId,
                        principalSchema: "product",
                        principalTable: "ProductReceives",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_ProductReceiveDetails_Products_ProductId",
                        column: x => x.ProductId,
                        principalSchema: "product",
                        principalTable: "Products",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_ProductReceiveDetails_UnitConversions_ReceiveUnitId",
                        column: x => x.ReceiveUnitId,
                        principalSchema: "lookup",
                        principalTable: "UnitConversions",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "IX_ProductReceiveDetails_ProductId",
                schema: "product",
                table: "ProductReceiveDetails",
                column: "ProductId");

            migrationBuilder.CreateIndex(
                name: "IX_ProductReceiveDetails_ProductReceiveId",
                schema: "product",
                table: "ProductReceiveDetails",
                column: "ProductReceiveId");

            migrationBuilder.CreateIndex(
                name: "IX_ProductReceiveDetails_ReceiveUnitId",
                schema: "product",
                table: "ProductReceiveDetails",
                column: "ReceiveUnitId");

            migrationBuilder.CreateIndex(
                name: "IX_ProductReceives_BranchId",
                schema: "product",
                table: "ProductReceives",
                column: "BranchId");

            migrationBuilder.CreateIndex(
                name: "IX_ProductReceives_CustomerId",
                schema: "product",
                table: "ProductReceives",
                column: "CustomerId");

            migrationBuilder.AddForeignKey(
                name: "FK_ProductDeliveryDetails_BaseUnits_DeliveryUnitId",
                schema: "product",
                table: "ProductDeliveryDetails",
                column: "DeliveryUnitId",
                principalSchema: "lookup",
                principalTable: "BaseUnits",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);
        }
    }
}
