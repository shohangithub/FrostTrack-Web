using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Persistence.Migrations
{
    /// <inheritdoc />
    public partial class UpdateExistingDeliveryPaymentStatus : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            // Update existing delivery records to have UNPAID status
            migrationBuilder.Sql(@"
                UPDATE [product].[Delivery] 
                SET [PaymentStatus] = 'UNPAID' 
                WHERE [PaymentStatus] = '' OR [PaymentStatus] IS NULL
            ");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            // No rollback needed for data update
        }
    }
}
