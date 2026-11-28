using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Cenvo.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddWashSalesIndexes : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateIndex(
                name: "ix_wash_ticket_branch_id_paid_at",
                table: "wash_ticket",
                columns: new[] { "branch_id", "paid_at" });

            migrationBuilder.CreateIndex(
                name: "ix_customer_membership_sold_branch_id_paid_at",
                table: "customer_membership",
                columns: new[] { "sold_branch_id", "paid_at" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "ix_wash_ticket_branch_id_paid_at",
                table: "wash_ticket");

            migrationBuilder.DropIndex(
                name: "ix_customer_membership_sold_branch_id_paid_at",
                table: "customer_membership");
        }
    }
}
