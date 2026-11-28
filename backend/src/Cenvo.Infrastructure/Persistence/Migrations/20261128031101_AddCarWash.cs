using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Cenvo.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddCarWash : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "customer",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    phone = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: false),
                    name = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: false),
                    note = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true),
                    is_deleted = table.Column<bool>(type: "boolean", nullable: false),
                    created_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    created_by = table.Column<Guid>(type: "uuid", nullable: true),
                    updated_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    updated_by = table.Column<Guid>(type: "uuid", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_customer", x => x.id);
                });

            migrationBuilder.CreateTable(
                name: "wash_offering",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    code = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: false),
                    name = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: false),
                    duration_minutes = table.Column<int>(type: "integer", nullable: false),
                    commission_type = table.Column<int>(type: "integer", nullable: false),
                    commission_value = table.Column<decimal>(type: "numeric(18,2)", precision: 18, scale: 2, nullable: false),
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
                    table.PrimaryKey("pk_wash_offering", x => x.id);
                });

            migrationBuilder.CreateTable(
                name: "vehicle",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    plate = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: false),
                    vehicle_type = table.Column<int>(type: "integer", nullable: false),
                    description = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true),
                    customer_id = table.Column<Guid>(type: "uuid", nullable: true),
                    is_deleted = table.Column<bool>(type: "boolean", nullable: false),
                    created_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    created_by = table.Column<Guid>(type: "uuid", nullable: true),
                    updated_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    updated_by = table.Column<Guid>(type: "uuid", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_vehicle", x => x.id);
                    table.ForeignKey(
                        name: "fk_vehicle_customer_customer_id",
                        column: x => x.customer_id,
                        principalTable: "customer",
                        principalColumn: "id",
                        onDelete: ReferentialAction.SetNull);
                });

            migrationBuilder.CreateTable(
                name: "membership_package",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    code = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: false),
                    name = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: false),
                    offering_id = table.Column<Guid>(type: "uuid", nullable: false),
                    vehicle_type = table.Column<int>(type: "integer", nullable: true),
                    price = table.Column<decimal>(type: "numeric(18,2)", precision: 18, scale: 2, nullable: false),
                    uses = table.Column<int>(type: "integer", nullable: false),
                    valid_days = table.Column<int>(type: "integer", nullable: false),
                    is_active = table.Column<bool>(type: "boolean", nullable: false),
                    is_deleted = table.Column<bool>(type: "boolean", nullable: false),
                    created_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    created_by = table.Column<Guid>(type: "uuid", nullable: true),
                    updated_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    updated_by = table.Column<Guid>(type: "uuid", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_membership_package", x => x.id);
                    table.ForeignKey(
                        name: "fk_membership_package_wash_offering_offering_id",
                        column: x => x.offering_id,
                        principalTable: "wash_offering",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "wash_offering_price",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    offering_id = table.Column<Guid>(type: "uuid", nullable: false),
                    vehicle_type = table.Column<int>(type: "integer", nullable: false),
                    price = table.Column<decimal>(type: "numeric(18,2)", precision: 18, scale: 2, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_wash_offering_price", x => x.id);
                    table.ForeignKey(
                        name: "fk_wash_offering_price_wash_offering_offering_id",
                        column: x => x.offering_id,
                        principalTable: "wash_offering",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "wash_offering_supply",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    offering_id = table.Column<Guid>(type: "uuid", nullable: false),
                    product_id = table.Column<Guid>(type: "uuid", nullable: false),
                    unit_id = table.Column<Guid>(type: "uuid", nullable: true),
                    quantity = table.Column<decimal>(type: "numeric(18,2)", precision: 18, scale: 2, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_wash_offering_supply", x => x.id);
                    table.ForeignKey(
                        name: "fk_wash_offering_supply_product_product_id",
                        column: x => x.product_id,
                        principalTable: "product",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "fk_wash_offering_supply_wash_offering_offering_id",
                        column: x => x.offering_id,
                        principalTable: "wash_offering",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "wash_ticket",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    branch_id = table.Column<Guid>(type: "uuid", nullable: false),
                    code = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: false),
                    vehicle_id = table.Column<Guid>(type: "uuid", nullable: false),
                    plate = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: false),
                    vehicle_type = table.Column<int>(type: "integer", nullable: false),
                    vehicle_description = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true),
                    customer_id = table.Column<Guid>(type: "uuid", nullable: true),
                    customer_name = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true),
                    customer_phone = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true),
                    note = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true),
                    status = table.Column<int>(type: "integer", nullable: false),
                    subtotal = table.Column<decimal>(type: "numeric(18,2)", precision: 18, scale: 2, nullable: false),
                    membership_covered = table.Column<decimal>(type: "numeric(18,2)", precision: 18, scale: 2, nullable: false),
                    total = table.Column<decimal>(type: "numeric(18,2)", precision: 18, scale: 2, nullable: false),
                    cash_received = table.Column<decimal>(type: "numeric(18,2)", precision: 18, scale: 2, nullable: false),
                    change_amount = table.Column<decimal>(type: "numeric(18,2)", precision: 18, scale: 2, nullable: false),
                    shift_id = table.Column<Guid>(type: "uuid", nullable: true),
                    paid_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    paid_by = table.Column<Guid>(type: "uuid", nullable: true),
                    started_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    completed_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    delivered_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    cancelled_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    cancel_reason = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true),
                    xmin = table.Column<uint>(type: "xid", rowVersion: true, nullable: false),
                    created_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    created_by = table.Column<Guid>(type: "uuid", nullable: true),
                    updated_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    updated_by = table.Column<Guid>(type: "uuid", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_wash_ticket", x => x.id);
                    table.ForeignKey(
                        name: "fk_wash_ticket_customer_customer_id",
                        column: x => x.customer_id,
                        principalTable: "customer",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "fk_wash_ticket_shift_shift_id",
                        column: x => x.shift_id,
                        principalTable: "shift",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "fk_wash_ticket_vehicle_vehicle_id",
                        column: x => x.vehicle_id,
                        principalTable: "vehicle",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "customer_membership",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    code = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: false),
                    sold_branch_id = table.Column<Guid>(type: "uuid", nullable: false),
                    customer_id = table.Column<Guid>(type: "uuid", nullable: false),
                    package_id = table.Column<Guid>(type: "uuid", nullable: false),
                    package_name = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: false),
                    offering_id = table.Column<Guid>(type: "uuid", nullable: false),
                    vehicle_type = table.Column<int>(type: "integer", nullable: true),
                    total_uses = table.Column<int>(type: "integer", nullable: false),
                    remaining_uses = table.Column<int>(type: "integer", nullable: false),
                    price = table.Column<decimal>(type: "numeric(18,2)", precision: 18, scale: 2, nullable: false),
                    expires_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    status = table.Column<int>(type: "integer", nullable: false),
                    shift_id = table.Column<Guid>(type: "uuid", nullable: false),
                    paid_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    paid_by = table.Column<Guid>(type: "uuid", nullable: true),
                    cash_received = table.Column<decimal>(type: "numeric(18,2)", precision: 18, scale: 2, nullable: false),
                    change_amount = table.Column<decimal>(type: "numeric(18,2)", precision: 18, scale: 2, nullable: false),
                    cancelled_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    cancel_reason = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true),
                    xmin = table.Column<uint>(type: "xid", rowVersion: true, nullable: false),
                    created_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    created_by = table.Column<Guid>(type: "uuid", nullable: true),
                    updated_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    updated_by = table.Column<Guid>(type: "uuid", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_customer_membership", x => x.id);
                    table.ForeignKey(
                        name: "fk_customer_membership_customer_customer_id",
                        column: x => x.customer_id,
                        principalTable: "customer",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "fk_customer_membership_membership_package_package_id",
                        column: x => x.package_id,
                        principalTable: "membership_package",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "fk_customer_membership_shift_shift_id",
                        column: x => x.shift_id,
                        principalTable: "shift",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "wash_payment",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    ticket_id = table.Column<Guid>(type: "uuid", nullable: false),
                    method = table.Column<int>(type: "integer", nullable: false),
                    amount = table.Column<decimal>(type: "numeric(18,2)", precision: 18, scale: 2, nullable: false),
                    reference = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_wash_payment", x => x.id);
                    table.ForeignKey(
                        name: "fk_wash_payment_wash_ticket_ticket_id",
                        column: x => x.ticket_id,
                        principalTable: "wash_ticket",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "wash_ticket_photo",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    ticket_id = table.Column<Guid>(type: "uuid", nullable: false),
                    storage_key = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: false),
                    content_type = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: false),
                    size_bytes = table.Column<long>(type: "bigint", nullable: false),
                    taken_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    taken_by = table.Column<Guid>(type: "uuid", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_wash_ticket_photo", x => x.id);
                    table.ForeignKey(
                        name: "fk_wash_ticket_photo_wash_ticket_ticket_id",
                        column: x => x.ticket_id,
                        principalTable: "wash_ticket",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "wash_ticket_staff",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    ticket_id = table.Column<Guid>(type: "uuid", nullable: false),
                    user_id = table.Column<Guid>(type: "uuid", nullable: false),
                    full_name = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: false),
                    commission = table.Column<decimal>(type: "numeric(18,2)", precision: 18, scale: 2, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_wash_ticket_staff", x => x.id);
                    table.ForeignKey(
                        name: "fk_wash_ticket_staff_wash_ticket_ticket_id",
                        column: x => x.ticket_id,
                        principalTable: "wash_ticket",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "membership_payment",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    membership_id = table.Column<Guid>(type: "uuid", nullable: false),
                    method = table.Column<int>(type: "integer", nullable: false),
                    amount = table.Column<decimal>(type: "numeric(18,2)", precision: 18, scale: 2, nullable: false),
                    reference = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_membership_payment", x => x.id);
                    table.ForeignKey(
                        name: "fk_membership_payment_customer_membership_membership_id",
                        column: x => x.membership_id,
                        principalTable: "customer_membership",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "wash_ticket_line",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    ticket_id = table.Column<Guid>(type: "uuid", nullable: false),
                    offering_id = table.Column<Guid>(type: "uuid", nullable: false),
                    name = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: false),
                    price = table.Column<decimal>(type: "numeric(18,2)", precision: 18, scale: 2, nullable: false),
                    membership_id = table.Column<Guid>(type: "uuid", nullable: true),
                    amount = table.Column<decimal>(type: "numeric(18,2)", precision: 18, scale: 2, nullable: false),
                    duration_minutes = table.Column<int>(type: "integer", nullable: false),
                    commission_type = table.Column<int>(type: "integer", nullable: false),
                    commission_value = table.Column<decimal>(type: "numeric(18,2)", precision: 18, scale: 2, nullable: false),
                    sort_order = table.Column<int>(type: "integer", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_wash_ticket_line", x => x.id);
                    table.ForeignKey(
                        name: "fk_wash_ticket_line_customer_membership_membership_id",
                        column: x => x.membership_id,
                        principalTable: "customer_membership",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "fk_wash_ticket_line_wash_offering_offering_id",
                        column: x => x.offering_id,
                        principalTable: "wash_offering",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "fk_wash_ticket_line_wash_ticket_ticket_id",
                        column: x => x.ticket_id,
                        principalTable: "wash_ticket",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "ix_customer_name",
                table: "customer",
                column: "name");

            migrationBuilder.CreateIndex(
                name: "ix_customer_phone",
                table: "customer",
                column: "phone",
                unique: true,
                filter: "is_deleted = false");

            migrationBuilder.CreateIndex(
                name: "ix_customer_membership_customer_id_status",
                table: "customer_membership",
                columns: new[] { "customer_id", "status" });

            migrationBuilder.CreateIndex(
                name: "ix_customer_membership_package_id",
                table: "customer_membership",
                column: "package_id");

            migrationBuilder.CreateIndex(
                name: "ix_customer_membership_shift_id",
                table: "customer_membership",
                column: "shift_id");

            migrationBuilder.CreateIndex(
                name: "ix_customer_membership_sold_branch_id_code",
                table: "customer_membership",
                columns: new[] { "sold_branch_id", "code" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "ix_membership_package_code",
                table: "membership_package",
                column: "code",
                unique: true,
                filter: "is_deleted = false");

            migrationBuilder.CreateIndex(
                name: "ix_membership_package_offering_id",
                table: "membership_package",
                column: "offering_id");

            migrationBuilder.CreateIndex(
                name: "ix_membership_payment_membership_id",
                table: "membership_payment",
                column: "membership_id");

            migrationBuilder.CreateIndex(
                name: "ix_vehicle_customer_id",
                table: "vehicle",
                column: "customer_id");

            migrationBuilder.CreateIndex(
                name: "ix_vehicle_plate",
                table: "vehicle",
                column: "plate",
                unique: true,
                filter: "is_deleted = false");

            migrationBuilder.CreateIndex(
                name: "ix_wash_offering_code",
                table: "wash_offering",
                column: "code",
                unique: true,
                filter: "is_deleted = false");

            migrationBuilder.CreateIndex(
                name: "ix_wash_offering_price_offering_id_vehicle_type",
                table: "wash_offering_price",
                columns: new[] { "offering_id", "vehicle_type" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "ix_wash_offering_supply_offering_id",
                table: "wash_offering_supply",
                column: "offering_id");

            migrationBuilder.CreateIndex(
                name: "ix_wash_offering_supply_product_id",
                table: "wash_offering_supply",
                column: "product_id");

            migrationBuilder.CreateIndex(
                name: "ix_wash_payment_ticket_id",
                table: "wash_payment",
                column: "ticket_id");

            migrationBuilder.CreateIndex(
                name: "ix_wash_ticket_branch_id_code",
                table: "wash_ticket",
                columns: new[] { "branch_id", "code" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "ix_wash_ticket_branch_id_completed_at",
                table: "wash_ticket",
                columns: new[] { "branch_id", "completed_at" });

            migrationBuilder.CreateIndex(
                name: "ix_wash_ticket_branch_id_created_at",
                table: "wash_ticket",
                columns: new[] { "branch_id", "created_at" });

            migrationBuilder.CreateIndex(
                name: "ix_wash_ticket_branch_id_status",
                table: "wash_ticket",
                columns: new[] { "branch_id", "status" });

            migrationBuilder.CreateIndex(
                name: "ix_wash_ticket_branch_id_vehicle_id",
                table: "wash_ticket",
                columns: new[] { "branch_id", "vehicle_id" });

            migrationBuilder.CreateIndex(
                name: "ix_wash_ticket_customer_id",
                table: "wash_ticket",
                column: "customer_id");

            migrationBuilder.CreateIndex(
                name: "ix_wash_ticket_shift_id",
                table: "wash_ticket",
                column: "shift_id");

            migrationBuilder.CreateIndex(
                name: "ix_wash_ticket_vehicle_id",
                table: "wash_ticket",
                column: "vehicle_id");

            migrationBuilder.CreateIndex(
                name: "ix_wash_ticket_line_membership_id",
                table: "wash_ticket_line",
                column: "membership_id");

            migrationBuilder.CreateIndex(
                name: "ix_wash_ticket_line_offering_id",
                table: "wash_ticket_line",
                column: "offering_id");

            migrationBuilder.CreateIndex(
                name: "ix_wash_ticket_line_ticket_id",
                table: "wash_ticket_line",
                column: "ticket_id");

            migrationBuilder.CreateIndex(
                name: "ix_wash_ticket_photo_ticket_id",
                table: "wash_ticket_photo",
                column: "ticket_id");

            migrationBuilder.CreateIndex(
                name: "ix_wash_ticket_staff_ticket_id",
                table: "wash_ticket_staff",
                column: "ticket_id");

            migrationBuilder.CreateIndex(
                name: "ix_wash_ticket_staff_user_id",
                table: "wash_ticket_staff",
                column: "user_id");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "membership_payment");

            migrationBuilder.DropTable(
                name: "wash_offering_price");

            migrationBuilder.DropTable(
                name: "wash_offering_supply");

            migrationBuilder.DropTable(
                name: "wash_payment");

            migrationBuilder.DropTable(
                name: "wash_ticket_line");

            migrationBuilder.DropTable(
                name: "wash_ticket_photo");

            migrationBuilder.DropTable(
                name: "wash_ticket_staff");

            migrationBuilder.DropTable(
                name: "customer_membership");

            migrationBuilder.DropTable(
                name: "wash_ticket");

            migrationBuilder.DropTable(
                name: "membership_package");

            migrationBuilder.DropTable(
                name: "vehicle");

            migrationBuilder.DropTable(
                name: "wash_offering");

            migrationBuilder.DropTable(
                name: "customer");
        }
    }
}
