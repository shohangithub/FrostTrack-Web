using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddBookingLedgerTables : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "AccrualEntries",
                schema: "product",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    BookingId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    BookingDetailId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    AccrualRunId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    Source = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: false),
                    BillPeriodFrom = table.Column<DateTime>(type: "datetime2", nullable: false),
                    BillPeriodTo = table.Column<DateTime>(type: "datetime2", nullable: false),
                    BillType = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: false),
                    Cycles = table.Column<int>(type: "int", nullable: false),
                    Quantity = table.Column<float>(type: "real", nullable: false),
                    Rate = table.Column<decimal>(type: "decimal(10,2)", nullable: false),
                    Amount = table.Column<decimal>(type: "decimal(10,2)", nullable: false),
                    Note = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    TenantId = table.Column<Guid>(type: "uniqueidentifier", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_AccrualEntries", x => x.Id);
                    table.ForeignKey(
                        name: "FK_AccrualEntries_AccrualRuns_AccrualRunId",
                        column: x => x.AccrualRunId,
                        principalSchema: "product",
                        principalTable: "AccrualRuns",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_AccrualEntries_BookingDetail_BookingDetailId",
                        column: x => x.BookingDetailId,
                        principalSchema: "product",
                        principalTable: "BookingDetail",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_AccrualEntries_Booking_BookingId",
                        column: x => x.BookingId,
                        principalSchema: "product",
                        principalTable: "Booking",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "BookingCharges",
                schema: "product",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    BookingId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    BookingDetailId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    DeliveryId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    DeliveryNumber = table.Column<string>(type: "nvarchar(30)", maxLength: 30, nullable: false),
                    DeliveryDate = table.Column<DateTime>(type: "datetime2", nullable: false),
                    Quantity = table.Column<float>(type: "real", nullable: false),
                    Rate = table.Column<decimal>(type: "decimal(10,2)", nullable: false),
                    ChargeAmount = table.Column<decimal>(type: "decimal(10,2)", nullable: false),
                    LabourCharge = table.Column<decimal>(type: "decimal(10,2)", nullable: false),
                    AdjustmentValue = table.Column<decimal>(type: "decimal(10,2)", nullable: false),
                    NetAmount = table.Column<decimal>(type: "decimal(10,2)", nullable: false),
                    Note = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    TenantId = table.Column<Guid>(type: "uniqueidentifier", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_BookingCharges", x => x.Id);
                    table.ForeignKey(
                        name: "FK_BookingCharges_BookingDetail_BookingDetailId",
                        column: x => x.BookingDetailId,
                        principalSchema: "product",
                        principalTable: "BookingDetail",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_BookingCharges_Booking_BookingId",
                        column: x => x.BookingId,
                        principalSchema: "product",
                        principalTable: "Booking",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_BookingCharges_Delivery_DeliveryId",
                        column: x => x.DeliveryId,
                        principalSchema: "product",
                        principalTable: "Delivery",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "BookingPayments",
                schema: "product",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    BookingId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    CustomerId = table.Column<int>(type: "int", nullable: false),
                    TransactionId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    DeliveryId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    TransactionCode = table.Column<string>(type: "nvarchar(30)", maxLength: 30, nullable: false),
                    TransactionDate = table.Column<DateTime>(type: "datetime2", nullable: false),
                    Amount = table.Column<decimal>(type: "decimal(10,2)", nullable: false),
                    DiscountAmount = table.Column<decimal>(type: "decimal(10,2)", nullable: false),
                    AdjustmentValue = table.Column<decimal>(type: "decimal(10,2)", nullable: false),
                    NetAmount = table.Column<decimal>(type: "decimal(10,2)", nullable: false),
                    PaymentMethod = table.Column<string>(type: "nvarchar(30)", maxLength: 30, nullable: false),
                    Reference = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: true),
                    Note = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    TenantId = table.Column<Guid>(type: "uniqueidentifier", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_BookingPayments", x => x.Id);
                    table.ForeignKey(
                        name: "FK_BookingPayments_Booking_BookingId",
                        column: x => x.BookingId,
                        principalSchema: "product",
                        principalTable: "Booking",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_BookingPayments_Customers_CustomerId",
                        column: x => x.CustomerId,
                        principalTable: "Customers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_BookingPayments_Delivery_DeliveryId",
                        column: x => x.DeliveryId,
                        principalSchema: "product",
                        principalTable: "Delivery",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_BookingPayments_Transactions_TransactionId",
                        column: x => x.TransactionId,
                        principalSchema: "finance",
                        principalTable: "Transactions",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

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
                name: "IX_BookingCharges_BookingDetailId",
                schema: "product",
                table: "BookingCharges",
                column: "BookingDetailId");

            migrationBuilder.CreateIndex(
                name: "IX_BookingCharges_BookingId",
                schema: "product",
                table: "BookingCharges",
                column: "BookingId");

            migrationBuilder.CreateIndex(
                name: "IX_BookingCharges_DeliveryId",
                schema: "product",
                table: "BookingCharges",
                column: "DeliveryId");

            migrationBuilder.CreateIndex(
                name: "IX_BookingPayments_BookingId",
                schema: "product",
                table: "BookingPayments",
                column: "BookingId");

            migrationBuilder.CreateIndex(
                name: "IX_BookingPayments_CustomerId",
                schema: "product",
                table: "BookingPayments",
                column: "CustomerId");

            migrationBuilder.CreateIndex(
                name: "IX_BookingPayments_DeliveryId",
                schema: "product",
                table: "BookingPayments",
                column: "DeliveryId");

            migrationBuilder.CreateIndex(
                name: "IX_BookingPayments_TransactionId",
                schema: "product",
                table: "BookingPayments",
                column: "TransactionId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "AccrualEntries",
                schema: "product");

            migrationBuilder.DropTable(
                name: "BookingCharges",
                schema: "product");

            migrationBuilder.DropTable(
                name: "BookingPayments",
                schema: "product");
        }
    }
}
