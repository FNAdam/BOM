using System;
using Microsoft.EntityFrameworkCore.Migrations;
using Npgsql.EntityFrameworkCore.PostgreSQL.Metadata;

#nullable disable

#pragma warning disable CA1814 // Prefer jagged arrays over multidimensional

namespace Bom.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddPricing : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "pack_levels",
                columns: table => new
                {
                    id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    finished_item_id = table.Column<int>(type: "integer", nullable: false),
                    name = table.Column<string>(type: "character varying(40)", maxLength: 40, nullable: false),
                    qty_per_base = table.Column<decimal>(type: "numeric(18,6)", precision: 18, scale: 6, nullable: false),
                    barcode = table.Column<string>(type: "character varying(40)", maxLength: 40, nullable: true),
                    sequence = table.Column<int>(type: "integer", nullable: false),
                    is_default_sales = table.Column<bool>(type: "boolean", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_pack_levels", x => x.id);
                    table.ForeignKey(
                        name: "fk_pack_levels_items_finished_item_id",
                        column: x => x.finished_item_id,
                        principalTable: "items",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "price_lists",
                columns: table => new
                {
                    id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    product_item_id = table.Column<int>(type: "integer", nullable: false),
                    pricing_method = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    cost_basis = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    target_percent = table.Column<decimal>(type: "numeric(18,6)", precision: 18, scale: 6, nullable: false),
                    manual_base_price = table.Column<decimal>(type: "numeric(18,4)", precision: 18, scale: 4, nullable: true),
                    floor_cost = table.Column<decimal>(type: "numeric(18,4)", precision: 18, scale: 4, nullable: true),
                    tax_mode = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    tax_percent = table.Column<decimal>(type: "numeric(18,6)", precision: 18, scale: 6, nullable: false),
                    effective_from = table.Column<DateOnly>(type: "date", nullable: true),
                    effective_to = table.Column<DateOnly>(type: "date", nullable: true),
                    status = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    notes = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_price_lists", x => x.id);
                    table.ForeignKey(
                        name: "fk_price_lists_items_product_item_id",
                        column: x => x.product_item_id,
                        principalTable: "items",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "pricing_settings",
                columns: table => new
                {
                    id = table.Column<int>(type: "integer", nullable: false),
                    default_target_margin_percent = table.Column<decimal>(type: "numeric(18,6)", precision: 18, scale: 6, nullable: false),
                    default_tax_percent = table.Column<decimal>(type: "numeric(18,6)", precision: 18, scale: 6, nullable: false),
                    min_margin_percent = table.Column<decimal>(type: "numeric(18,6)", precision: 18, scale: 6, nullable: false),
                    rounding_rule = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_pricing_settings", x => x.id);
                });

            migrationBuilder.CreateTable(
                name: "sales_channels",
                columns: table => new
                {
                    id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    code = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    name = table.Column<string>(type: "character varying(60)", maxLength: 60, nullable: false),
                    default_margin_percent = table.Column<decimal>(type: "numeric(18,6)", precision: 18, scale: 6, nullable: false),
                    sequence = table.Column<int>(type: "integer", nullable: false),
                    is_active = table.Column<bool>(type: "boolean", nullable: false, defaultValue: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_sales_channels", x => x.id);
                });

            migrationBuilder.CreateTable(
                name: "price_list_lines",
                columns: table => new
                {
                    id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    price_list_id = table.Column<int>(type: "integer", nullable: false),
                    sales_channel_id = table.Column<int>(type: "integer", nullable: true),
                    pack_level_id = table.Column<int>(type: "integer", nullable: false),
                    tier_name = table.Column<string>(type: "character varying(40)", maxLength: 40, nullable: false),
                    margin_percent = table.Column<decimal>(type: "numeric(18,6)", precision: 18, scale: 6, nullable: false),
                    net_price = table.Column<decimal>(type: "numeric(18,4)", precision: 18, scale: 4, nullable: false),
                    tax_amount = table.Column<decimal>(type: "numeric(18,4)", precision: 18, scale: 4, nullable: false),
                    gross_price = table.Column<decimal>(type: "numeric(18,4)", precision: 18, scale: 4, nullable: false),
                    contribution_margin = table.Column<decimal>(type: "numeric(18,4)", precision: 18, scale: 4, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_price_list_lines", x => x.id);
                    table.ForeignKey(
                        name: "fk_price_list_lines_pack_levels_pack_level_id",
                        column: x => x.pack_level_id,
                        principalTable: "pack_levels",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "fk_price_list_lines_price_lists_price_list_id",
                        column: x => x.price_list_id,
                        principalTable: "price_lists",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "fk_price_list_lines_sales_channels_sales_channel_id",
                        column: x => x.sales_channel_id,
                        principalTable: "sales_channels",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.InsertData(
                table: "pricing_settings",
                columns: new[] { "id", "default_target_margin_percent", "default_tax_percent", "min_margin_percent", "rounding_rule" },
                values: new object[] { 1, 0.45m, 0.11m, 0m, "None" });

            migrationBuilder.InsertData(
                table: "sales_channels",
                columns: new[] { "id", "code", "default_margin_percent", "is_active", "name", "sequence" },
                values: new object[,]
                {
                    { 1, "DIST", 0.12m, true, "Distributor", 1 },
                    { 2, "RETAIL", 0.20m, true, "Retail", 2 }
                });

            migrationBuilder.CreateIndex(
                name: "ix_pack_levels_finished_item_id_name",
                table: "pack_levels",
                columns: new[] { "finished_item_id", "name" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "ix_price_list_lines_pack_level_id",
                table: "price_list_lines",
                column: "pack_level_id");

            migrationBuilder.CreateIndex(
                name: "ix_price_list_lines_price_list_id",
                table: "price_list_lines",
                column: "price_list_id");

            migrationBuilder.CreateIndex(
                name: "ix_price_list_lines_sales_channel_id",
                table: "price_list_lines",
                column: "sales_channel_id");

            migrationBuilder.CreateIndex(
                name: "ix_price_lists_product_item_id",
                table: "price_lists",
                column: "product_item_id",
                unique: true,
                filter: "status = 'Active'");

            migrationBuilder.CreateIndex(
                name: "ix_sales_channels_code",
                table: "sales_channels",
                column: "code",
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "price_list_lines");

            migrationBuilder.DropTable(
                name: "pricing_settings");

            migrationBuilder.DropTable(
                name: "pack_levels");

            migrationBuilder.DropTable(
                name: "price_lists");

            migrationBuilder.DropTable(
                name: "sales_channels");
        }
    }
}
