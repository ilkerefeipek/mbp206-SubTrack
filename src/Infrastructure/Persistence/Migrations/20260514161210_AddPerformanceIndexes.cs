using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace SubTrack.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddPerformanceIndexes : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_Subscriptions_NextBilling",
                table: "Subscriptions");

            migrationBuilder.DropIndex(
                name: "IX_Payments_SubscriptionId_PaymentDate",
                table: "Payments");

            migrationBuilder.DropIndex(
                name: "IX_Notifications_UserId_IsRead",
                table: "Notifications");

            migrationBuilder.CreateIndex(
                name: "IX_Subscriptions_Active_NextBilling",
                table: "Subscriptions",
                column: "NextBilling",
                filter: "[Status] = 'Active'");

            migrationBuilder.CreateIndex(
                name: "IX_Payments_Sub_Date_Amount",
                table: "Payments",
                columns: new[] { "SubscriptionId", "PaymentDate" },
                descending: new[] { false, true })
                .Annotation("SqlServer:Include", new[] { "Amount" });

            migrationBuilder.CreateIndex(
                name: "IX_Notifications_Unread",
                table: "Notifications",
                columns: new[] { "UserId", "IsRead" },
                filter: "[IsRead] = 0");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_Subscriptions_Active_NextBilling",
                table: "Subscriptions");

            migrationBuilder.DropIndex(
                name: "IX_Payments_Sub_Date_Amount",
                table: "Payments");

            migrationBuilder.DropIndex(
                name: "IX_Notifications_Unread",
                table: "Notifications");

            migrationBuilder.CreateIndex(
                name: "IX_Subscriptions_NextBilling",
                table: "Subscriptions",
                column: "NextBilling");

            migrationBuilder.CreateIndex(
                name: "IX_Payments_SubscriptionId_PaymentDate",
                table: "Payments",
                columns: new[] { "SubscriptionId", "PaymentDate" });

            migrationBuilder.CreateIndex(
                name: "IX_Notifications_UserId_IsRead",
                table: "Notifications",
                columns: new[] { "UserId", "IsRead" });
        }
    }
}
