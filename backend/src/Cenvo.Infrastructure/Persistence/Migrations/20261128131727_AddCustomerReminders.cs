using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Cenvo.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddCustomerReminders : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<bool>(
                name: "accepts_reminders",
                table: "customer",
                type: "boolean",
                nullable: false,
                defaultValue: true); // existing customers receive reminders unless they opt out (business rule)

            migrationBuilder.AddColumn<Guid>(
                name: "last_ticket_id",
                table: "customer",
                type: "uuid",
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "last_wash_at",
                table: "customer",
                type: "timestamp with time zone",
                nullable: true);

            migrationBuilder.AddColumn<Guid>(
                name: "last_wash_branch_id",
                table: "customer",
                type: "uuid",
                nullable: true);

            migrationBuilder.CreateTable(
                name: "customer_reminder",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    branch_id = table.Column<Guid>(type: "uuid", nullable: false),
                    kind = table.Column<int>(type: "integer", nullable: false),
                    ref_id = table.Column<Guid>(type: "uuid", nullable: false),
                    customer_id = table.Column<Guid>(type: "uuid", nullable: false),
                    phone = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    message = table.Column<string>(type: "character varying(640)", maxLength: 640, nullable: false),
                    status = table.Column<int>(type: "integer", nullable: false),
                    attempts = table.Column<int>(type: "integer", nullable: false),
                    attempted_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    attempted_by = table.Column<Guid>(type: "uuid", nullable: true),
                    sent_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    provider_message_id = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true),
                    last_error = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true),
                    xmin = table.Column<uint>(type: "xid", rowVersion: true, nullable: false),
                    created_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    created_by = table.Column<Guid>(type: "uuid", nullable: true),
                    updated_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    updated_by = table.Column<Guid>(type: "uuid", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_customer_reminder", x => x.id);
                    table.ForeignKey(
                        name: "fk_customer_reminder_customer_customer_id",
                        column: x => x.customer_id,
                        principalTable: "customer",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "reminder_settings",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    branch_id = table.Column<Guid>(type: "uuid", nullable: false),
                    return_after_days = table.Column<int>(type: "integer", nullable: false),
                    expiring_within_days = table.Column<int>(type: "integer", nullable: false),
                    low_uses_threshold = table.Column<int>(type: "integer", nullable: false),
                    return_template = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: false),
                    expiring_template = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: false),
                    low_uses_template = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: false),
                    created_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    created_by = table.Column<Guid>(type: "uuid", nullable: true),
                    updated_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    updated_by = table.Column<Guid>(type: "uuid", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_reminder_settings", x => x.id);
                });

            migrationBuilder.CreateIndex(
                name: "ix_customer_last_wash_branch_id_last_wash_at",
                table: "customer",
                columns: new[] { "last_wash_branch_id", "last_wash_at" });

            migrationBuilder.CreateIndex(
                name: "ix_customer_reminder_branch_id_attempted_at",
                table: "customer_reminder",
                columns: new[] { "branch_id", "attempted_at" });

            migrationBuilder.CreateIndex(
                name: "ix_customer_reminder_customer_id",
                table: "customer_reminder",
                column: "customer_id");

            migrationBuilder.CreateIndex(
                name: "ix_customer_reminder_kind_ref_id",
                table: "customer_reminder",
                columns: new[] { "kind", "ref_id" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "ix_reminder_settings_branch_id",
                table: "reminder_settings",
                column: "branch_id",
                unique: true);

            // Backfill the latest finished wash (Done = 3 / Delivered = 4) of every customer, across all branches.
            migrationBuilder.Sql("""
                UPDATE customer c
                SET last_ticket_id = t.id, last_wash_branch_id = t.branch_id, last_wash_at = t.completed_at
                FROM (
                    SELECT DISTINCT ON (customer_id) id, customer_id, branch_id, completed_at
                    FROM wash_ticket
                    WHERE customer_id IS NOT NULL AND completed_at IS NOT NULL AND status IN (3, 4)
                    ORDER BY customer_id, completed_at DESC
                ) t
                WHERE c.id = t.customer_id;
                """);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "customer_reminder");

            migrationBuilder.DropTable(
                name: "reminder_settings");

            migrationBuilder.DropIndex(
                name: "ix_customer_last_wash_branch_id_last_wash_at",
                table: "customer");

            migrationBuilder.DropColumn(
                name: "accepts_reminders",
                table: "customer");

            migrationBuilder.DropColumn(
                name: "last_ticket_id",
                table: "customer");

            migrationBuilder.DropColumn(
                name: "last_wash_at",
                table: "customer");

            migrationBuilder.DropColumn(
                name: "last_wash_branch_id",
                table: "customer");
        }
    }
}
