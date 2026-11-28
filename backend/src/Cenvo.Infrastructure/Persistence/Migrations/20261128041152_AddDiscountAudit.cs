using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Cenvo.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddDiscountAudit : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "discount_reason",
                table: "cafe_order_line",
                type: "character varying(500)",
                maxLength: 500,
                nullable: true);

            migrationBuilder.AddColumn<Guid>(
                name: "discount_approved_by",
                table: "cafe_order",
                type: "uuid",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "discount_approved_by_name",
                table: "cafe_order",
                type: "character varying(500)",
                maxLength: 500,
                nullable: true);

            migrationBuilder.AddColumn<decimal>(
                name: "discount_approved_gross",
                table: "cafe_order",
                type: "numeric(18,2)",
                precision: 18,
                scale: 2,
                nullable: true);

            migrationBuilder.AddColumn<decimal>(
                name: "discount_approved_total",
                table: "cafe_order",
                type: "numeric(18,2)",
                precision: 18,
                scale: 2,
                nullable: true);

            migrationBuilder.AddColumn<Guid>(
                name: "discount_by",
                table: "cafe_order",
                type: "uuid",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "discount_by_name",
                table: "cafe_order",
                type: "character varying(500)",
                maxLength: 500,
                nullable: true);

            migrationBuilder.AddColumn<decimal>(
                name: "discount_percent",
                table: "cafe_order",
                type: "numeric(18,2)",
                precision: 18,
                scale: 2,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "discount_reason",
                table: "cafe_order",
                type: "character varying(500)",
                maxLength: 500,
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "discount_reason",
                table: "cafe_order_line");

            migrationBuilder.DropColumn(
                name: "discount_approved_by",
                table: "cafe_order");

            migrationBuilder.DropColumn(
                name: "discount_approved_by_name",
                table: "cafe_order");

            migrationBuilder.DropColumn(
                name: "discount_approved_gross",
                table: "cafe_order");

            migrationBuilder.DropColumn(
                name: "discount_approved_total",
                table: "cafe_order");

            migrationBuilder.DropColumn(
                name: "discount_by",
                table: "cafe_order");

            migrationBuilder.DropColumn(
                name: "discount_by_name",
                table: "cafe_order");

            migrationBuilder.DropColumn(
                name: "discount_percent",
                table: "cafe_order");

            migrationBuilder.DropColumn(
                name: "discount_reason",
                table: "cafe_order");
        }
    }
}
