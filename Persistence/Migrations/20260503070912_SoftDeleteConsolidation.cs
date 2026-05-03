using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Persistence.Migrations
{
    /// <inheritdoc />
    public partial class SoftDeleteConsolidation : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "ArchivedBy",
                table: "BankTransactions");

            migrationBuilder.AddColumn<DateTime>(
                name: "ArchivedAt",
                schema: "lookup",
                table: "UnitConversions",
                type: "datetime2",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "ArchivedById",
                schema: "lookup",
                table: "UnitConversions",
                type: "int",
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "DeletedAt",
                schema: "lookup",
                table: "UnitConversions",
                type: "datetime2",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "DeletedById",
                schema: "lookup",
                table: "UnitConversions",
                type: "int",
                nullable: true);

            migrationBuilder.AddColumn<bool>(
                name: "IsArchived",
                schema: "lookup",
                table: "UnitConversions",
                type: "bit",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<bool>(
                name: "IsDeleted",
                schema: "lookup",
                table: "UnitConversions",
                type: "bit",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<DateTime>(
                name: "ArchivedAt",
                schema: "finance",
                table: "TransactionHeads",
                type: "datetime2",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "ArchivedById",
                schema: "finance",
                table: "TransactionHeads",
                type: "int",
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "DeletedAt",
                schema: "finance",
                table: "TransactionHeads",
                type: "datetime2",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "DeletedById",
                schema: "finance",
                table: "TransactionHeads",
                type: "int",
                nullable: true);

            migrationBuilder.AddColumn<bool>(
                name: "IsArchived",
                schema: "finance",
                table: "TransactionHeads",
                type: "bit",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<bool>(
                name: "IsDeleted",
                schema: "finance",
                table: "TransactionHeads",
                type: "bit",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<DateTime>(
                name: "ArchivedAt",
                table: "Suppliers",
                type: "datetime2",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "ArchivedById",
                table: "Suppliers",
                type: "int",
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "DeletedAt",
                table: "Suppliers",
                type: "datetime2",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "DeletedById",
                table: "Suppliers",
                type: "int",
                nullable: true);

            migrationBuilder.AddColumn<bool>(
                name: "IsArchived",
                table: "Suppliers",
                type: "bit",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<bool>(
                name: "IsDeleted",
                table: "Suppliers",
                type: "bit",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<DateTime>(
                name: "ArchivedAt",
                schema: "payment",
                table: "SupplierPayments",
                type: "datetime2",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "ArchivedById",
                schema: "payment",
                table: "SupplierPayments",
                type: "int",
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "DeletedAt",
                schema: "payment",
                table: "SupplierPayments",
                type: "datetime2",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "DeletedById",
                schema: "payment",
                table: "SupplierPayments",
                type: "int",
                nullable: true);

            migrationBuilder.AddColumn<bool>(
                name: "IsArchived",
                schema: "payment",
                table: "SupplierPayments",
                type: "bit",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<bool>(
                name: "IsDeleted",
                schema: "payment",
                table: "SupplierPayments",
                type: "bit",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<DateTime>(
                name: "ArchivedAt",
                schema: "payment",
                table: "SupplierPaymentDetails",
                type: "datetime2",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "ArchivedById",
                schema: "payment",
                table: "SupplierPaymentDetails",
                type: "int",
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "DeletedAt",
                schema: "payment",
                table: "SupplierPaymentDetails",
                type: "datetime2",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "DeletedById",
                schema: "payment",
                table: "SupplierPaymentDetails",
                type: "int",
                nullable: true);

            migrationBuilder.AddColumn<bool>(
                name: "IsArchived",
                schema: "payment",
                table: "SupplierPaymentDetails",
                type: "bit",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<bool>(
                name: "IsDeleted",
                schema: "payment",
                table: "SupplierPaymentDetails",
                type: "bit",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<DateTime>(
                name: "ArchivedAt",
                schema: "product",
                table: "Stocks",
                type: "datetime2",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "ArchivedById",
                schema: "product",
                table: "Stocks",
                type: "int",
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "DeletedAt",
                schema: "product",
                table: "Stocks",
                type: "datetime2",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "DeletedById",
                schema: "product",
                table: "Stocks",
                type: "int",
                nullable: true);

            migrationBuilder.AddColumn<bool>(
                name: "IsArchived",
                schema: "product",
                table: "Stocks",
                type: "bit",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<bool>(
                name: "IsDeleted",
                schema: "product",
                table: "Stocks",
                type: "bit",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<DateTime>(
                name: "ArchivedAt",
                schema: "product",
                table: "SalesDetails",
                type: "datetime2",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "ArchivedById",
                schema: "product",
                table: "SalesDetails",
                type: "int",
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "DeletedAt",
                schema: "product",
                table: "SalesDetails",
                type: "datetime2",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "DeletedById",
                schema: "product",
                table: "SalesDetails",
                type: "int",
                nullable: true);

            migrationBuilder.AddColumn<bool>(
                name: "IsArchived",
                schema: "product",
                table: "SalesDetails",
                type: "bit",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<bool>(
                name: "IsDeleted",
                schema: "product",
                table: "SalesDetails",
                type: "bit",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<DateTime>(
                name: "ArchivedAt",
                schema: "product",
                table: "Sales",
                type: "datetime2",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "ArchivedById",
                schema: "product",
                table: "Sales",
                type: "int",
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "DeletedAt",
                schema: "product",
                table: "Sales",
                type: "datetime2",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "DeletedById",
                schema: "product",
                table: "Sales",
                type: "int",
                nullable: true);

            migrationBuilder.AddColumn<bool>(
                name: "IsArchived",
                schema: "product",
                table: "Sales",
                type: "bit",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<bool>(
                name: "IsDeleted",
                schema: "product",
                table: "Sales",
                type: "bit",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<DateTime>(
                name: "ArchivedAt",
                schema: "product",
                table: "SaleReturns",
                type: "datetime2",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "ArchivedById",
                schema: "product",
                table: "SaleReturns",
                type: "int",
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "DeletedAt",
                schema: "product",
                table: "SaleReturns",
                type: "datetime2",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "DeletedById",
                schema: "product",
                table: "SaleReturns",
                type: "int",
                nullable: true);

            migrationBuilder.AddColumn<bool>(
                name: "IsArchived",
                schema: "product",
                table: "SaleReturns",
                type: "bit",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<bool>(
                name: "IsDeleted",
                schema: "product",
                table: "SaleReturns",
                type: "bit",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<DateTime>(
                name: "ArchivedAt",
                schema: "product",
                table: "SaleReturnDetails",
                type: "datetime2",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "ArchivedById",
                schema: "product",
                table: "SaleReturnDetails",
                type: "int",
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "DeletedAt",
                schema: "product",
                table: "SaleReturnDetails",
                type: "datetime2",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "DeletedById",
                schema: "product",
                table: "SaleReturnDetails",
                type: "int",
                nullable: true);

            migrationBuilder.AddColumn<bool>(
                name: "IsArchived",
                schema: "product",
                table: "SaleReturnDetails",
                type: "bit",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<bool>(
                name: "IsDeleted",
                schema: "product",
                table: "SaleReturnDetails",
                type: "bit",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<DateTime>(
                name: "ArchivedAt",
                schema: "finance",
                table: "SalaryPayments",
                type: "datetime2",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "ArchivedById",
                schema: "finance",
                table: "SalaryPayments",
                type: "int",
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "DeletedAt",
                schema: "finance",
                table: "SalaryPayments",
                type: "datetime2",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "DeletedById",
                schema: "finance",
                table: "SalaryPayments",
                type: "int",
                nullable: true);

            migrationBuilder.AddColumn<bool>(
                name: "IsArchived",
                schema: "finance",
                table: "SalaryPayments",
                type: "bit",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<bool>(
                name: "IsDeleted",
                schema: "finance",
                table: "SalaryPayments",
                type: "bit",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<DateTime>(
                name: "ArchivedAt",
                schema: "product",
                table: "Purchases",
                type: "datetime2",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "ArchivedById",
                schema: "product",
                table: "Purchases",
                type: "int",
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "DeletedAt",
                schema: "product",
                table: "Purchases",
                type: "datetime2",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "DeletedById",
                schema: "product",
                table: "Purchases",
                type: "int",
                nullable: true);

            migrationBuilder.AddColumn<bool>(
                name: "IsArchived",
                schema: "product",
                table: "Purchases",
                type: "bit",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<bool>(
                name: "IsDeleted",
                schema: "product",
                table: "Purchases",
                type: "bit",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<DateTime>(
                name: "ArchivedAt",
                schema: "product",
                table: "PurchaseDetails",
                type: "datetime2",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "ArchivedById",
                schema: "product",
                table: "PurchaseDetails",
                type: "int",
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "DeletedAt",
                schema: "product",
                table: "PurchaseDetails",
                type: "datetime2",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "DeletedById",
                schema: "product",
                table: "PurchaseDetails",
                type: "int",
                nullable: true);

            migrationBuilder.AddColumn<bool>(
                name: "IsArchived",
                schema: "product",
                table: "PurchaseDetails",
                type: "bit",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<bool>(
                name: "IsDeleted",
                schema: "product",
                table: "PurchaseDetails",
                type: "bit",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<DateTime>(
                name: "ArchivedAt",
                schema: "product",
                table: "Products",
                type: "datetime2",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "ArchivedById",
                schema: "product",
                table: "Products",
                type: "int",
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "DeletedAt",
                schema: "product",
                table: "Products",
                type: "datetime2",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "DeletedById",
                schema: "product",
                table: "Products",
                type: "int",
                nullable: true);

            migrationBuilder.AddColumn<bool>(
                name: "IsArchived",
                schema: "product",
                table: "Products",
                type: "bit",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<bool>(
                name: "IsDeleted",
                schema: "product",
                table: "Products",
                type: "bit",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<DateTime>(
                name: "ArchivedAt",
                schema: "product",
                table: "ProductCategories",
                type: "datetime2",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "ArchivedById",
                schema: "product",
                table: "ProductCategories",
                type: "int",
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "DeletedAt",
                schema: "product",
                table: "ProductCategories",
                type: "datetime2",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "DeletedById",
                schema: "product",
                table: "ProductCategories",
                type: "int",
                nullable: true);

            migrationBuilder.AddColumn<bool>(
                name: "IsArchived",
                schema: "product",
                table: "ProductCategories",
                type: "bit",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<bool>(
                name: "IsDeleted",
                schema: "product",
                table: "ProductCategories",
                type: "bit",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<DateTime>(
                name: "ArchivedAt",
                schema: "general",
                table: "PaymentMethods",
                type: "datetime2",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "ArchivedById",
                schema: "general",
                table: "PaymentMethods",
                type: "int",
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "DeletedAt",
                schema: "general",
                table: "PaymentMethods",
                type: "datetime2",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "DeletedById",
                schema: "general",
                table: "PaymentMethods",
                type: "int",
                nullable: true);

            migrationBuilder.AddColumn<bool>(
                name: "IsArchived",
                schema: "general",
                table: "PaymentMethods",
                type: "bit",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<bool>(
                name: "IsDeleted",
                schema: "general",
                table: "PaymentMethods",
                type: "bit",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<DateTime>(
                name: "ArchivedAt",
                table: "Employees",
                type: "datetime2",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "ArchivedById",
                table: "Employees",
                type: "int",
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "DeletedAt",
                table: "Employees",
                type: "datetime2",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "DeletedById",
                table: "Employees",
                type: "int",
                nullable: true);

            migrationBuilder.AddColumn<bool>(
                name: "IsArchived",
                table: "Employees",
                type: "bit",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<bool>(
                name: "IsDeleted",
                table: "Employees",
                type: "bit",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<DateTime>(
                name: "DeletedAt",
                schema: "product",
                table: "DeliveryDetail",
                type: "datetime2",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "DeletedById",
                schema: "product",
                table: "DeliveryDetail",
                type: "int",
                nullable: true);

            migrationBuilder.AddColumn<bool>(
                name: "IsDeleted",
                schema: "product",
                table: "DeliveryDetail",
                type: "bit",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<DateTime>(
                name: "ArchivedAt",
                schema: "product",
                table: "DeliveryChallan",
                type: "datetime2",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "ArchivedById",
                schema: "product",
                table: "DeliveryChallan",
                type: "int",
                nullable: true);

            migrationBuilder.AddColumn<bool>(
                name: "IsArchived",
                schema: "product",
                table: "DeliveryChallan",
                type: "bit",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<DateTime>(
                name: "ArchivedAt",
                schema: "product",
                table: "Damages",
                type: "datetime2",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "ArchivedById",
                schema: "product",
                table: "Damages",
                type: "int",
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "DeletedAt",
                schema: "product",
                table: "Damages",
                type: "datetime2",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "DeletedById",
                schema: "product",
                table: "Damages",
                type: "int",
                nullable: true);

            migrationBuilder.AddColumn<bool>(
                name: "IsArchived",
                schema: "product",
                table: "Damages",
                type: "bit",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<bool>(
                name: "IsDeleted",
                schema: "product",
                table: "Damages",
                type: "bit",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<DateTime>(
                name: "ArchivedAt",
                table: "Customers",
                type: "datetime2",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "ArchivedById",
                table: "Customers",
                type: "int",
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "DeletedAt",
                table: "Customers",
                type: "datetime2",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "DeletedById",
                table: "Customers",
                type: "int",
                nullable: true);

            migrationBuilder.AddColumn<bool>(
                name: "IsArchived",
                table: "Customers",
                type: "bit",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<bool>(
                name: "IsDeleted",
                table: "Customers",
                type: "bit",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<DateTime>(
                name: "DeletedAt",
                schema: "product",
                table: "BookingDetail",
                type: "datetime2",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "DeletedById",
                schema: "product",
                table: "BookingDetail",
                type: "int",
                nullable: true);

            migrationBuilder.AddColumn<bool>(
                name: "IsDeleted",
                schema: "product",
                table: "BookingDetail",
                type: "bit",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<DateTime>(
                name: "DeletedAt",
                schema: "product",
                table: "Booking",
                type: "datetime2",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "DeletedById",
                schema: "product",
                table: "Booking",
                type: "int",
                nullable: true);

            migrationBuilder.AddColumn<bool>(
                name: "IsDeleted",
                schema: "product",
                table: "Booking",
                type: "bit",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<DateTime>(
                name: "ArchivedAt",
                schema: "lookup",
                table: "BaseUnits",
                type: "datetime2",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "ArchivedById",
                schema: "lookup",
                table: "BaseUnits",
                type: "int",
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "DeletedAt",
                schema: "lookup",
                table: "BaseUnits",
                type: "datetime2",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "DeletedById",
                schema: "lookup",
                table: "BaseUnits",
                type: "int",
                nullable: true);

            migrationBuilder.AddColumn<bool>(
                name: "IsArchived",
                schema: "lookup",
                table: "BaseUnits",
                type: "bit",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<bool>(
                name: "IsDeleted",
                schema: "lookup",
                table: "BaseUnits",
                type: "bit",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<int>(
                name: "ArchivedById",
                table: "BankTransactions",
                type: "int",
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "DeletedAt",
                table: "BankTransactions",
                type: "datetime2",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "DeletedById",
                table: "BankTransactions",
                type: "int",
                nullable: true);

            migrationBuilder.AddColumn<bool>(
                name: "IsDeleted",
                table: "BankTransactions",
                type: "bit",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<DateTime>(
                name: "ArchivedAt",
                table: "Banks",
                type: "datetime2",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "ArchivedById",
                table: "Banks",
                type: "int",
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "DeletedAt",
                table: "Banks",
                type: "datetime2",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "DeletedById",
                table: "Banks",
                type: "int",
                nullable: true);

            migrationBuilder.AddColumn<bool>(
                name: "IsArchived",
                table: "Banks",
                type: "bit",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<bool>(
                name: "IsDeleted",
                table: "Banks",
                type: "bit",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<DateTime>(
                name: "ArchivedAt",
                table: "Assets",
                type: "datetime2",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "ArchivedById",
                table: "Assets",
                type: "int",
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "DeletedAt",
                table: "Assets",
                type: "datetime2",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "DeletedById",
                table: "Assets",
                type: "int",
                nullable: true);

            migrationBuilder.AddColumn<bool>(
                name: "IsArchived",
                table: "Assets",
                type: "bit",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<bool>(
                name: "IsDeleted",
                table: "Assets",
                type: "bit",
                nullable: false,
                defaultValue: false);

            migrationBuilder.CreateIndex(
                name: "IX_Delivery_TenantId",
                schema: "product",
                table: "Delivery",
                column: "TenantId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_Delivery_TenantId",
                schema: "product",
                table: "Delivery");

            migrationBuilder.DropColumn(
                name: "ArchivedAt",
                schema: "lookup",
                table: "UnitConversions");

            migrationBuilder.DropColumn(
                name: "ArchivedById",
                schema: "lookup",
                table: "UnitConversions");

            migrationBuilder.DropColumn(
                name: "DeletedAt",
                schema: "lookup",
                table: "UnitConversions");

            migrationBuilder.DropColumn(
                name: "DeletedById",
                schema: "lookup",
                table: "UnitConversions");

            migrationBuilder.DropColumn(
                name: "IsArchived",
                schema: "lookup",
                table: "UnitConversions");

            migrationBuilder.DropColumn(
                name: "IsDeleted",
                schema: "lookup",
                table: "UnitConversions");

            migrationBuilder.DropColumn(
                name: "ArchivedAt",
                schema: "finance",
                table: "TransactionHeads");

            migrationBuilder.DropColumn(
                name: "ArchivedById",
                schema: "finance",
                table: "TransactionHeads");

            migrationBuilder.DropColumn(
                name: "DeletedAt",
                schema: "finance",
                table: "TransactionHeads");

            migrationBuilder.DropColumn(
                name: "DeletedById",
                schema: "finance",
                table: "TransactionHeads");

            migrationBuilder.DropColumn(
                name: "IsArchived",
                schema: "finance",
                table: "TransactionHeads");

            migrationBuilder.DropColumn(
                name: "IsDeleted",
                schema: "finance",
                table: "TransactionHeads");

            migrationBuilder.DropColumn(
                name: "ArchivedAt",
                table: "Suppliers");

            migrationBuilder.DropColumn(
                name: "ArchivedById",
                table: "Suppliers");

            migrationBuilder.DropColumn(
                name: "DeletedAt",
                table: "Suppliers");

            migrationBuilder.DropColumn(
                name: "DeletedById",
                table: "Suppliers");

            migrationBuilder.DropColumn(
                name: "IsArchived",
                table: "Suppliers");

            migrationBuilder.DropColumn(
                name: "IsDeleted",
                table: "Suppliers");

            migrationBuilder.DropColumn(
                name: "ArchivedAt",
                schema: "payment",
                table: "SupplierPayments");

            migrationBuilder.DropColumn(
                name: "ArchivedById",
                schema: "payment",
                table: "SupplierPayments");

            migrationBuilder.DropColumn(
                name: "DeletedAt",
                schema: "payment",
                table: "SupplierPayments");

            migrationBuilder.DropColumn(
                name: "DeletedById",
                schema: "payment",
                table: "SupplierPayments");

            migrationBuilder.DropColumn(
                name: "IsArchived",
                schema: "payment",
                table: "SupplierPayments");

            migrationBuilder.DropColumn(
                name: "IsDeleted",
                schema: "payment",
                table: "SupplierPayments");

            migrationBuilder.DropColumn(
                name: "ArchivedAt",
                schema: "payment",
                table: "SupplierPaymentDetails");

            migrationBuilder.DropColumn(
                name: "ArchivedById",
                schema: "payment",
                table: "SupplierPaymentDetails");

            migrationBuilder.DropColumn(
                name: "DeletedAt",
                schema: "payment",
                table: "SupplierPaymentDetails");

            migrationBuilder.DropColumn(
                name: "DeletedById",
                schema: "payment",
                table: "SupplierPaymentDetails");

            migrationBuilder.DropColumn(
                name: "IsArchived",
                schema: "payment",
                table: "SupplierPaymentDetails");

            migrationBuilder.DropColumn(
                name: "IsDeleted",
                schema: "payment",
                table: "SupplierPaymentDetails");

            migrationBuilder.DropColumn(
                name: "ArchivedAt",
                schema: "product",
                table: "Stocks");

            migrationBuilder.DropColumn(
                name: "ArchivedById",
                schema: "product",
                table: "Stocks");

            migrationBuilder.DropColumn(
                name: "DeletedAt",
                schema: "product",
                table: "Stocks");

            migrationBuilder.DropColumn(
                name: "DeletedById",
                schema: "product",
                table: "Stocks");

            migrationBuilder.DropColumn(
                name: "IsArchived",
                schema: "product",
                table: "Stocks");

            migrationBuilder.DropColumn(
                name: "IsDeleted",
                schema: "product",
                table: "Stocks");

            migrationBuilder.DropColumn(
                name: "ArchivedAt",
                schema: "product",
                table: "SalesDetails");

            migrationBuilder.DropColumn(
                name: "ArchivedById",
                schema: "product",
                table: "SalesDetails");

            migrationBuilder.DropColumn(
                name: "DeletedAt",
                schema: "product",
                table: "SalesDetails");

            migrationBuilder.DropColumn(
                name: "DeletedById",
                schema: "product",
                table: "SalesDetails");

            migrationBuilder.DropColumn(
                name: "IsArchived",
                schema: "product",
                table: "SalesDetails");

            migrationBuilder.DropColumn(
                name: "IsDeleted",
                schema: "product",
                table: "SalesDetails");

            migrationBuilder.DropColumn(
                name: "ArchivedAt",
                schema: "product",
                table: "Sales");

            migrationBuilder.DropColumn(
                name: "ArchivedById",
                schema: "product",
                table: "Sales");

            migrationBuilder.DropColumn(
                name: "DeletedAt",
                schema: "product",
                table: "Sales");

            migrationBuilder.DropColumn(
                name: "DeletedById",
                schema: "product",
                table: "Sales");

            migrationBuilder.DropColumn(
                name: "IsArchived",
                schema: "product",
                table: "Sales");

            migrationBuilder.DropColumn(
                name: "IsDeleted",
                schema: "product",
                table: "Sales");

            migrationBuilder.DropColumn(
                name: "ArchivedAt",
                schema: "product",
                table: "SaleReturns");

            migrationBuilder.DropColumn(
                name: "ArchivedById",
                schema: "product",
                table: "SaleReturns");

            migrationBuilder.DropColumn(
                name: "DeletedAt",
                schema: "product",
                table: "SaleReturns");

            migrationBuilder.DropColumn(
                name: "DeletedById",
                schema: "product",
                table: "SaleReturns");

            migrationBuilder.DropColumn(
                name: "IsArchived",
                schema: "product",
                table: "SaleReturns");

            migrationBuilder.DropColumn(
                name: "IsDeleted",
                schema: "product",
                table: "SaleReturns");

            migrationBuilder.DropColumn(
                name: "ArchivedAt",
                schema: "product",
                table: "SaleReturnDetails");

            migrationBuilder.DropColumn(
                name: "ArchivedById",
                schema: "product",
                table: "SaleReturnDetails");

            migrationBuilder.DropColumn(
                name: "DeletedAt",
                schema: "product",
                table: "SaleReturnDetails");

            migrationBuilder.DropColumn(
                name: "DeletedById",
                schema: "product",
                table: "SaleReturnDetails");

            migrationBuilder.DropColumn(
                name: "IsArchived",
                schema: "product",
                table: "SaleReturnDetails");

            migrationBuilder.DropColumn(
                name: "IsDeleted",
                schema: "product",
                table: "SaleReturnDetails");

            migrationBuilder.DropColumn(
                name: "ArchivedAt",
                schema: "finance",
                table: "SalaryPayments");

            migrationBuilder.DropColumn(
                name: "ArchivedById",
                schema: "finance",
                table: "SalaryPayments");

            migrationBuilder.DropColumn(
                name: "DeletedAt",
                schema: "finance",
                table: "SalaryPayments");

            migrationBuilder.DropColumn(
                name: "DeletedById",
                schema: "finance",
                table: "SalaryPayments");

            migrationBuilder.DropColumn(
                name: "IsArchived",
                schema: "finance",
                table: "SalaryPayments");

            migrationBuilder.DropColumn(
                name: "IsDeleted",
                schema: "finance",
                table: "SalaryPayments");

            migrationBuilder.DropColumn(
                name: "ArchivedAt",
                schema: "product",
                table: "Purchases");

            migrationBuilder.DropColumn(
                name: "ArchivedById",
                schema: "product",
                table: "Purchases");

            migrationBuilder.DropColumn(
                name: "DeletedAt",
                schema: "product",
                table: "Purchases");

            migrationBuilder.DropColumn(
                name: "DeletedById",
                schema: "product",
                table: "Purchases");

            migrationBuilder.DropColumn(
                name: "IsArchived",
                schema: "product",
                table: "Purchases");

            migrationBuilder.DropColumn(
                name: "IsDeleted",
                schema: "product",
                table: "Purchases");

            migrationBuilder.DropColumn(
                name: "ArchivedAt",
                schema: "product",
                table: "PurchaseDetails");

            migrationBuilder.DropColumn(
                name: "ArchivedById",
                schema: "product",
                table: "PurchaseDetails");

            migrationBuilder.DropColumn(
                name: "DeletedAt",
                schema: "product",
                table: "PurchaseDetails");

            migrationBuilder.DropColumn(
                name: "DeletedById",
                schema: "product",
                table: "PurchaseDetails");

            migrationBuilder.DropColumn(
                name: "IsArchived",
                schema: "product",
                table: "PurchaseDetails");

            migrationBuilder.DropColumn(
                name: "IsDeleted",
                schema: "product",
                table: "PurchaseDetails");

            migrationBuilder.DropColumn(
                name: "ArchivedAt",
                schema: "product",
                table: "Products");

            migrationBuilder.DropColumn(
                name: "ArchivedById",
                schema: "product",
                table: "Products");

            migrationBuilder.DropColumn(
                name: "DeletedAt",
                schema: "product",
                table: "Products");

            migrationBuilder.DropColumn(
                name: "DeletedById",
                schema: "product",
                table: "Products");

            migrationBuilder.DropColumn(
                name: "IsArchived",
                schema: "product",
                table: "Products");

            migrationBuilder.DropColumn(
                name: "IsDeleted",
                schema: "product",
                table: "Products");

            migrationBuilder.DropColumn(
                name: "ArchivedAt",
                schema: "product",
                table: "ProductCategories");

            migrationBuilder.DropColumn(
                name: "ArchivedById",
                schema: "product",
                table: "ProductCategories");

            migrationBuilder.DropColumn(
                name: "DeletedAt",
                schema: "product",
                table: "ProductCategories");

            migrationBuilder.DropColumn(
                name: "DeletedById",
                schema: "product",
                table: "ProductCategories");

            migrationBuilder.DropColumn(
                name: "IsArchived",
                schema: "product",
                table: "ProductCategories");

            migrationBuilder.DropColumn(
                name: "IsDeleted",
                schema: "product",
                table: "ProductCategories");

            migrationBuilder.DropColumn(
                name: "ArchivedAt",
                schema: "general",
                table: "PaymentMethods");

            migrationBuilder.DropColumn(
                name: "ArchivedById",
                schema: "general",
                table: "PaymentMethods");

            migrationBuilder.DropColumn(
                name: "DeletedAt",
                schema: "general",
                table: "PaymentMethods");

            migrationBuilder.DropColumn(
                name: "DeletedById",
                schema: "general",
                table: "PaymentMethods");

            migrationBuilder.DropColumn(
                name: "IsArchived",
                schema: "general",
                table: "PaymentMethods");

            migrationBuilder.DropColumn(
                name: "IsDeleted",
                schema: "general",
                table: "PaymentMethods");

            migrationBuilder.DropColumn(
                name: "ArchivedAt",
                table: "Employees");

            migrationBuilder.DropColumn(
                name: "ArchivedById",
                table: "Employees");

            migrationBuilder.DropColumn(
                name: "DeletedAt",
                table: "Employees");

            migrationBuilder.DropColumn(
                name: "DeletedById",
                table: "Employees");

            migrationBuilder.DropColumn(
                name: "IsArchived",
                table: "Employees");

            migrationBuilder.DropColumn(
                name: "IsDeleted",
                table: "Employees");

            migrationBuilder.DropColumn(
                name: "DeletedAt",
                schema: "product",
                table: "DeliveryDetail");

            migrationBuilder.DropColumn(
                name: "DeletedById",
                schema: "product",
                table: "DeliveryDetail");

            migrationBuilder.DropColumn(
                name: "IsDeleted",
                schema: "product",
                table: "DeliveryDetail");

            migrationBuilder.DropColumn(
                name: "ArchivedAt",
                schema: "product",
                table: "DeliveryChallan");

            migrationBuilder.DropColumn(
                name: "ArchivedById",
                schema: "product",
                table: "DeliveryChallan");

            migrationBuilder.DropColumn(
                name: "IsArchived",
                schema: "product",
                table: "DeliveryChallan");

            migrationBuilder.DropColumn(
                name: "ArchivedAt",
                schema: "product",
                table: "Damages");

            migrationBuilder.DropColumn(
                name: "ArchivedById",
                schema: "product",
                table: "Damages");

            migrationBuilder.DropColumn(
                name: "DeletedAt",
                schema: "product",
                table: "Damages");

            migrationBuilder.DropColumn(
                name: "DeletedById",
                schema: "product",
                table: "Damages");

            migrationBuilder.DropColumn(
                name: "IsArchived",
                schema: "product",
                table: "Damages");

            migrationBuilder.DropColumn(
                name: "IsDeleted",
                schema: "product",
                table: "Damages");

            migrationBuilder.DropColumn(
                name: "ArchivedAt",
                table: "Customers");

            migrationBuilder.DropColumn(
                name: "ArchivedById",
                table: "Customers");

            migrationBuilder.DropColumn(
                name: "DeletedAt",
                table: "Customers");

            migrationBuilder.DropColumn(
                name: "DeletedById",
                table: "Customers");

            migrationBuilder.DropColumn(
                name: "IsArchived",
                table: "Customers");

            migrationBuilder.DropColumn(
                name: "IsDeleted",
                table: "Customers");

            migrationBuilder.DropColumn(
                name: "DeletedAt",
                schema: "product",
                table: "BookingDetail");

            migrationBuilder.DropColumn(
                name: "DeletedById",
                schema: "product",
                table: "BookingDetail");

            migrationBuilder.DropColumn(
                name: "IsDeleted",
                schema: "product",
                table: "BookingDetail");

            migrationBuilder.DropColumn(
                name: "DeletedAt",
                schema: "product",
                table: "Booking");

            migrationBuilder.DropColumn(
                name: "DeletedById",
                schema: "product",
                table: "Booking");

            migrationBuilder.DropColumn(
                name: "IsDeleted",
                schema: "product",
                table: "Booking");

            migrationBuilder.DropColumn(
                name: "ArchivedAt",
                schema: "lookup",
                table: "BaseUnits");

            migrationBuilder.DropColumn(
                name: "ArchivedById",
                schema: "lookup",
                table: "BaseUnits");

            migrationBuilder.DropColumn(
                name: "DeletedAt",
                schema: "lookup",
                table: "BaseUnits");

            migrationBuilder.DropColumn(
                name: "DeletedById",
                schema: "lookup",
                table: "BaseUnits");

            migrationBuilder.DropColumn(
                name: "IsArchived",
                schema: "lookup",
                table: "BaseUnits");

            migrationBuilder.DropColumn(
                name: "IsDeleted",
                schema: "lookup",
                table: "BaseUnits");

            migrationBuilder.DropColumn(
                name: "ArchivedById",
                table: "BankTransactions");

            migrationBuilder.DropColumn(
                name: "DeletedAt",
                table: "BankTransactions");

            migrationBuilder.DropColumn(
                name: "DeletedById",
                table: "BankTransactions");

            migrationBuilder.DropColumn(
                name: "IsDeleted",
                table: "BankTransactions");

            migrationBuilder.DropColumn(
                name: "ArchivedAt",
                table: "Banks");

            migrationBuilder.DropColumn(
                name: "ArchivedById",
                table: "Banks");

            migrationBuilder.DropColumn(
                name: "DeletedAt",
                table: "Banks");

            migrationBuilder.DropColumn(
                name: "DeletedById",
                table: "Banks");

            migrationBuilder.DropColumn(
                name: "IsArchived",
                table: "Banks");

            migrationBuilder.DropColumn(
                name: "IsDeleted",
                table: "Banks");

            migrationBuilder.DropColumn(
                name: "ArchivedAt",
                table: "Assets");

            migrationBuilder.DropColumn(
                name: "ArchivedById",
                table: "Assets");

            migrationBuilder.DropColumn(
                name: "DeletedAt",
                table: "Assets");

            migrationBuilder.DropColumn(
                name: "DeletedById",
                table: "Assets");

            migrationBuilder.DropColumn(
                name: "IsArchived",
                table: "Assets");

            migrationBuilder.DropColumn(
                name: "IsDeleted",
                table: "Assets");

            migrationBuilder.AddColumn<string>(
                name: "ArchivedBy",
                table: "BankTransactions",
                type: "nvarchar(max)",
                nullable: true);
        }
    }
}
