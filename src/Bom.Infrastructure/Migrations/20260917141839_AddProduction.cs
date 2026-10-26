using System;
using Microsoft.EntityFrameworkCore.Migrations;
using Npgsql.EntityFrameworkCore.PostgreSQL.Metadata;

#nullable disable

namespace Bom.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddProduction : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "production_orders",
                columns: table => new
                {
                    id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    bom_id = table.Column<int>(type: "integer", nullable: false),
                    product_item_id = table.Column<int>(type: "integer", nullable: false),
                    batch_no = table.Column<string>(type: "character varying(40)", maxLength: 40, nullable: false),
                    planned_qty = table.Column<decimal>(type: "numeric(18,6)", precision: 18, scale: 6, nullable: false),
                    actual_qty = table.Column<decimal>(type: "numeric(18,6)", precision: 18, scale: 6, nullable: true),
                    status = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    start_date = table.Column<DateOnly>(type: "date", nullable: true),
                    end_date = table.Column<DateOnly>(type: "date", nullable: true),
                    notes = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_production_orders", x => x.id);
                    table.ForeignKey(
                        name: "fk_production_orders_boms_bom_id",
                        column: x => x.bom_id,
                        principalTable: "boms",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "fk_production_orders_items_product_item_id",
                        column: x => x.product_item_id,
                        principalTable: "items",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "stock_movements",
                columns: table => new
                {
                    id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    item_id = table.Column<int>(type: "integer", nullable: false),
                    type = table.Column<string>(type: "character varying(30)", maxLength: 30, nullable: false),
                    qty = table.Column<decimal>(type: "numeric(18,6)", precision: 18, scale: 6, nullable: false),
                    unit_cost = table.Column<decimal>(type: "numeric(18,4)", precision: 18, scale: 4, nullable: false),
                    movement_date = table.Column<DateOnly>(type: "date", nullable: false),
                    ref_type = table.Column<string>(type: "character varying(40)", maxLength: 40, nullable: true),
                    ref_id = table.Column<int>(type: "integer", nullable: true),
                    notes = table.Column<string>(type: "character varying(300)", maxLength: 300, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_stock_movements", x => x.id);
                    table.ForeignKey(
                        name: "fk_stock_movements_items_item_id",
                        column: x => x.item_id,
                        principalTable: "items",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "production_consumptions",
                columns: table => new
                {
                    id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    production_order_id = table.Column<int>(type: "integer", nullable: false),
                    component_item_id = table.Column<int>(type: "integer", nullable: false),
                    planned_qty = table.Column<decimal>(type: "numeric(18,6)", precision: 18, scale: 6, nullable: false),
                    actual_qty = table.Column<decimal>(type: "numeric(18,6)", precision: 18, scale: 6, nullable: false, defaultValue: 0m),
                    uom_id = table.Column<int>(type: "integer", nullable: false),
                    actual_unit_cost = table.Column<decimal>(type: "numeric(18,4)", precision: 18, scale: 4, nullable: false, defaultValue: 0m),
                    actual_cost = table.Column<decimal>(type: "numeric(18,4)", precision: 18, scale: 4, nullable: false, defaultValue: 0m)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_production_consumptions", x => x.id);
                    table.ForeignKey(
                        name: "fk_production_consumptions_items_component_item_id",
                        column: x => x.component_item_id,
                        principalTable: "items",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "fk_production_consumptions_production_orders_production_order_",
                        column: x => x.production_order_id,
                        principalTable: "production_orders",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "fk_production_consumptions_uoms_uom_id",
                        column: x => x.uom_id,
                        principalTable: "uoms",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "ix_production_consumptions_component_item_id",
                table: "production_consumptions",
                column: "component_item_id");

            migrationBuilder.CreateIndex(
                name: "ix_production_consumptions_production_order_id",
                table: "production_consumptions",
                column: "production_order_id");

            migrationBuilder.CreateIndex(
                name: "ix_production_consumptions_uom_id",
                table: "production_consumptions",
                column: "uom_id");

            migrationBuilder.CreateIndex(
                name: "ix_production_orders_batch_no",
                table: "production_orders",
                column: "batch_no");

            migrationBuilder.CreateIndex(
                name: "ix_production_orders_bom_id",
                table: "production_orders",
                column: "bom_id");

            migrationBuilder.CreateIndex(
                name: "ix_production_orders_product_item_id",
                table: "production_orders",
                column: "product_item_id");

            migrationBuilder.CreateIndex(
                name: "ix_production_orders_status",
                table: "production_orders",
                column: "status");

            migrationBuilder.CreateIndex(
                name: "ix_stock_movements_item_id_movement_date",
                table: "stock_movements",
                columns: new[] { "item_id", "movement_date" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "production_consumptions");

            migrationBuilder.DropTable(
                name: "stock_movements");

            migrationBuilder.DropTable(
                name: "production_orders");
        }
    }
}
