using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Cenvo.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddAttendance : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<decimal>(
                name: "overtime_hourly_rate",
                table: "salary_rate",
                type: "numeric(18,2)",
                precision: 18,
                scale: 2,
                nullable: false,
                defaultValue: 0m);

            migrationBuilder.AddColumn<int>(
                name: "pin_failed_count",
                table: "app_user",
                type: "integer",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<string>(
                name: "pin_hash",
                table: "app_user",
                type: "character varying(500)",
                maxLength: 500,
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "pin_locked_until",
                table: "app_user",
                type: "timestamp with time zone",
                nullable: true);

            migrationBuilder.CreateTable(
                name: "attendance_record",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    branch_id = table.Column<Guid>(type: "uuid", nullable: false),
                    employee_id = table.Column<Guid>(type: "uuid", nullable: false),
                    employee_name = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    work_date = table.Column<DateOnly>(type: "date", nullable: false),
                    shift_assignment_id = table.Column<Guid>(type: "uuid", nullable: true),
                    shift_name = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: true),
                    scheduled_start = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    scheduled_end = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    check_in_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    check_out_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    check_in_method = table.Column<int>(type: "integer", nullable: false),
                    check_out_method = table.Column<int>(type: "integer", nullable: true),
                    on_own_screen = table.Column<bool>(type: "boolean", nullable: false),
                    approval = table.Column<int>(type: "integer", nullable: false),
                    reviewed_by = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: true),
                    review_note = table.Column<string>(type: "character varying(300)", maxLength: 300, nullable: true),
                    overtime_approved_minutes = table.Column<int>(type: "integer", nullable: true),
                    xmin = table.Column<uint>(type: "xid", rowVersion: true, nullable: false),
                    created_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    created_by = table.Column<Guid>(type: "uuid", nullable: true),
                    updated_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    updated_by = table.Column<Guid>(type: "uuid", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_attendance_record", x => x.id);
                });

            migrationBuilder.CreateTable(
                name: "holiday",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    date = table.Column<DateOnly>(type: "date", nullable: false),
                    name = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    multiplier = table.Column<decimal>(type: "numeric(18,2)", precision: 18, scale: 2, nullable: false),
                    is_deleted = table.Column<bool>(type: "boolean", nullable: false),
                    created_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    created_by = table.Column<Guid>(type: "uuid", nullable: true),
                    updated_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    updated_by = table.Column<Guid>(type: "uuid", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_holiday", x => x.id);
                });

            migrationBuilder.CreateTable(
                name: "attendance_edit",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    attendance_record_id = table.Column<Guid>(type: "uuid", nullable: false),
                    at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    editor_id = table.Column<Guid>(type: "uuid", nullable: false),
                    editor_name = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    old_check_in = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    old_check_out = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    new_check_in = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    new_check_out = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    reason = table.Column<string>(type: "character varying(300)", maxLength: 300, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_attendance_edit", x => x.id);
                    table.ForeignKey(
                        name: "fk_attendance_edit_attendance_record_attendance_record_id",
                        column: x => x.attendance_record_id,
                        principalTable: "attendance_record",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "ix_attendance_edit_attendance_record_id",
                table: "attendance_edit",
                column: "attendance_record_id");

            migrationBuilder.CreateIndex(
                name: "ix_attendance_record_branch_id_work_date",
                table: "attendance_record",
                columns: new[] { "branch_id", "work_date" });

            migrationBuilder.CreateIndex(
                name: "ix_attendance_record_employee_id_check_in_at",
                table: "attendance_record",
                columns: new[] { "employee_id", "check_in_at" });

            migrationBuilder.CreateIndex(
                name: "ix_attendance_record_shift_assignment_id",
                table: "attendance_record",
                column: "shift_assignment_id",
                unique: true,
                filter: "shift_assignment_id IS NOT NULL");

            migrationBuilder.CreateIndex(
                name: "ix_holiday_date",
                table: "holiday",
                column: "date",
                unique: true,
                filter: "is_deleted = false");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "attendance_edit");

            migrationBuilder.DropTable(
                name: "holiday");

            migrationBuilder.DropTable(
                name: "attendance_record");

            migrationBuilder.DropColumn(
                name: "overtime_hourly_rate",
                table: "salary_rate");

            migrationBuilder.DropColumn(
                name: "pin_failed_count",
                table: "app_user");

            migrationBuilder.DropColumn(
                name: "pin_hash",
                table: "app_user");

            migrationBuilder.DropColumn(
                name: "pin_locked_until",
                table: "app_user");
        }
    }
}
