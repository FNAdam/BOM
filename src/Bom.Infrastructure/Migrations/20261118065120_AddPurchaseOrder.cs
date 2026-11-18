using System;
using Microsoft.EntityFrameworkCore.Migrations;
using Npgsql.EntityFrameworkCore.PostgreSQL.Metadata;

#nullable disable

namespace Bom.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddPurchaseOrder : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "purchase_orders",
                columns: table => new
                {
                    id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    supplier_id = table.Column<int>(type: "integer", nullable: false),
                    order_no = table.Column<string>(type: "character varying(40)", maxLength: 40, nullable: false),
                    status = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    order_date = table.Column<DateOnly>(type: "date", nullable: false),
                    notes = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_purchase_orders", x => x.id);
                    table.ForeignKey(
                        name: "fk_purchase_orders_suppliers_supplier_id",
                        column: x => x.supplier_id,
                        principalTable: "suppliers",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "purchase_order_lines",
                columns: table => new
                {
                    id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    purchase_order_id = table.Column<int>(type: "integer", nullable: false),
                    item_id = table.Column<int>(type: "integer", nullable: false),
                    uom_id = table.Column<int>(type: "integer", nullable: false),
                    qty_ordered = table.Column<decimal>(type: "numeric(18,6)", precision: 18, scale: 6, nullable: false),
                    unit_price = table.Column<decimal>(type: "numeric(18,4)", precision: 18, scale: 4, nullable: false),
                    qty_received = table.Column<decimal>(type: "numeric(18,6)", precision: 18, scale: 6, nullable: false, defaultValue: 0m)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_purchase_order_lines", x => x.id);
                    table.ForeignKey(
                        name: "fk_purchase_order_lines_items_item_id",
                        column: x => x.item_id,
                        principalTable: "items",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "fk_purchase_order_lines_purchase_orders_purchase_order_id",
                        column: x => x.purchase_order_id,
                        principalTable: "purchase_orders",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "fk_purchase_order_lines_uoms_uom_id",
                        column: x => x.uom_id,
                        principalTable: "uoms",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "ix_purchase_order_lines_item_id",
                table: "purchase_order_lines",
                column: "item_id");

            migrationBuilder.CreateIndex(
                name: "ix_purchase_order_lines_purchase_order_id",
                table: "purchase_order_lines",
                column: "purchase_order_id");

            migrationBuilder.CreateIndex(
                name: "ix_purchase_order_lines_uom_id",
                table: "purchase_order_lines",
                column: "uom_id");

            migrationBuilder.CreateIndex(
                name: "ix_purchase_orders_order_no",
                table: "purchase_orders",
                column: "order_no",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "ix_purchase_orders_supplier_id",
                table: "purchase_orders",
                column: "supplier_id");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "purchase_order_lines");

            migrationBuilder.DropTable(
                name: "purchase_orders");
        }
    }
}
