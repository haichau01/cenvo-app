using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Cenvo.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddSchedule : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "schedule_settings",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    branch_id = table.Column<Guid>(type: "uuid", nullable: false),
                    max_weekly_hours = table.Column<int>(type: "integer", nullable: false),
                    created_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    created_by = table.Column<Guid>(type: "uuid", nullable: true),
                    updated_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    updated_by = table.Column<Guid>(type: "uuid", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_schedule_settings", x => x.id);
                });

            migrationBuilder.CreateTable(
                name: "shift_assignment",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    branch_id = table.Column<Guid>(type: "uuid", nullable: false),
                    employee_id = table.Column<Guid>(type: "uuid", nullable: false),
                    employee_name = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    date = table.Column<DateOnly>(type: "date", nullable: false),
                    shift_template_id = table.Column<Guid>(type: "uuid", nullable: false),
                    shift_name = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    start_time = table.Column<TimeOnly>(type: "time without time zone", nullable: false),
                    end_time = table.Column<TimeOnly>(type: "time without time zone", nullable: false),
                    starts_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    ends_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    minutes = table.Column<int>(type: "integer", nullable: false),
                    created_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    created_by = table.Column<Guid>(type: "uuid", nullable: true),
                    updated_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    updated_by = table.Column<Guid>(type: "uuid", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_shift_assignment", x => x.id);
                });

            migrationBuilder.CreateTable(
                name: "shift_template",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    branch_id = table.Column<Guid>(type: "uuid", nullable: false),
                    name = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    start_time = table.Column<TimeOnly>(type: "time without time zone", nullable: false),
                    end_time = table.Column<TimeOnly>(type: "time without time zone", nullable: false),
                    sort_order = table.Column<int>(type: "integer", nullable: false),
                    is_active = table.Column<bool>(type: "boolean", nullable: false),
                    is_deleted = table.Column<bool>(type: "boolean", nullable: false),
                    created_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    created_by = table.Column<Guid>(type: "uuid", nullable: true),
                    updated_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    updated_by = table.Column<Guid>(type: "uuid", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_shift_template", x => x.id);
                });

            migrationBuilder.CreateTable(
                name: "shift_requirement",
                columns: table => new
                {
                    shift_template_id = table.Column<Guid>(type: "uuid", nullable: false),
                    department = table.Column<int>(type: "integer", nullable: false),
                    min_staff = table.Column<int>(type: "integer", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_shift_requirement", x => new { x.shift_template_id, x.department });
                    table.ForeignKey(
                        name: "fk_shift_requirement_shift_template_shift_template_id",
                        column: x => x.shift_template_id,
                        principalTable: "shift_template",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "ix_schedule_settings_branch_id",
                table: "schedule_settings",
                column: "branch_id",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "ix_shift_assignment_branch_id_date",
                table: "shift_assignment",
                columns: new[] { "branch_id", "date" });

            migrationBuilder.CreateIndex(
                name: "ix_shift_assignment_employee_id_date_shift_template_id",
                table: "shift_assignment",
                columns: new[] { "employee_id", "date", "shift_template_id" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "ix_shift_assignment_employee_id_starts_at",
                table: "shift_assignment",
                columns: new[] { "employee_id", "starts_at" });

            migrationBuilder.CreateIndex(
                name: "ix_shift_template_branch_id_sort_order",
                table: "shift_template",
                columns: new[] { "branch_id", "sort_order" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "schedule_settings");

            migrationBuilder.DropTable(
                name: "shift_assignment");

            migrationBuilder.DropTable(
                name: "shift_requirement");

            migrationBuilder.DropTable(
                name: "shift_template");
        }
    }
}
