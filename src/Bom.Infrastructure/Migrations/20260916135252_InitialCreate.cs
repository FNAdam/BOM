using System;
using Microsoft.EntityFrameworkCore.Migrations;
using Npgsql.EntityFrameworkCore.PostgreSQL.Metadata;

#nullable disable

#pragma warning disable CA1814 // Prefer jagged arrays over multidimensional

namespace Bom.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class InitialCreate : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "item_categories",
                columns: table => new
                {
                    id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    name = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    description = table.Column<string>(type: "character varying(300)", maxLength: 300, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_item_categories", x => x.id);
                });

            migrationBuilder.CreateTable(
                name: "suppliers",
                columns: table => new
                {
                    id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    code = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    name = table.Column<string>(type: "character varying(150)", maxLength: 150, nullable: false),
                    lead_time_days = table.Column<int>(type: "integer", nullable: true),
                    is_active = table.Column<bool>(type: "boolean", nullable: false, defaultValue: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_suppliers", x => x.id);
                });

            migrationBuilder.CreateTable(
                name: "uoms",
                columns: table => new
                {
                    id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    code = table.Column<string>(type: "character varying(10)", maxLength: 10, nullable: false),
                    name = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    type = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    factor_to_base = table.Column<decimal>(type: "numeric(18,6)", precision: 18, scale: 6, nullable: false, defaultValue: 1m),
                    is_base = table.Column<bool>(type: "boolean", nullable: false, defaultValue: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_uoms", x => x.id);
                });

            migrationBuilder.CreateTable(
                name: "items",
                columns: table => new
                {
                    id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    code = table.Column<string>(type: "character varying(30)", maxLength: 30, nullable: false),
                    name = table.Column<string>(type: "character varying(150)", maxLength: 150, nullable: false),
                    type = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    base_uom_id = table.Column<int>(type: "integer", nullable: false),
                    category_id = table.Column<int>(type: "integer", nullable: true),
                    standard_material_cost = table.Column<decimal>(type: "numeric(18,4)", precision: 18, scale: 4, nullable: false, defaultValue: 0m),
                    costing_method = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    density = table.Column<decimal>(type: "numeric(18,6)", precision: 18, scale: 6, nullable: true),
                    is_active = table.Column<bool>(type: "boolean", nullable: false, defaultValue: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_items", x => x.id);
                    table.ForeignKey(
                        name: "fk_items_item_categories_category_id",
                        column: x => x.category_id,
                        principalTable: "item_categories",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "fk_items_uoms_base_uom_id",
                        column: x => x.base_uom_id,
                        principalTable: "uoms",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "uom_conversions",
                columns: table => new
                {
                    id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    from_uom_id = table.Column<int>(type: "integer", nullable: false),
                    to_uom_id = table.Column<int>(type: "integer", nullable: false),
                    factor = table.Column<decimal>(type: "numeric(18,6)", precision: 18, scale: 6, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_uom_conversions", x => x.id);
                    table.ForeignKey(
                        name: "fk_uom_conversions_uoms_from_uom_id",
                        column: x => x.from_uom_id,
                        principalTable: "uoms",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "fk_uom_conversions_uoms_to_uom_id",
                        column: x => x.to_uom_id,
                        principalTable: "uoms",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "item_supplier_prices",
                columns: table => new
                {
                    id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    item_id = table.Column<int>(type: "integer", nullable: false),
                    supplier_id = table.Column<int>(type: "integer", nullable: false),
                    uom_id = table.Column<int>(type: "integer", nullable: false),
                    price = table.Column<decimal>(type: "numeric(18,4)", precision: 18, scale: 4, nullable: false),
                    currency = table.Column<string>(type: "character varying(3)", maxLength: 3, nullable: false, defaultValue: "IDR"),
                    min_order_qty = table.Column<decimal>(type: "numeric(18,4)", precision: 18, scale: 4, nullable: false, defaultValue: 0m),
                    effective_from = table.Column<DateOnly>(type: "date", nullable: true),
                    effective_to = table.Column<DateOnly>(type: "date", nullable: true),
                    is_preferred = table.Column<bool>(type: "boolean", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_item_supplier_prices", x => x.id);
                    table.ForeignKey(
                        name: "fk_item_supplier_prices_items_item_id",
                        column: x => x.item_id,
                        principalTable: "items",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "fk_item_supplier_prices_suppliers_supplier_id",
                        column: x => x.supplier_id,
                        principalTable: "suppliers",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "fk_item_supplier_prices_uoms_uom_id",
                        column: x => x.uom_id,
                        principalTable: "uoms",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.InsertData(
                table: "item_categories",
                columns: new[] { "id", "description", "name" },
                values: new object[,]
                {
                    { 1, null, "Bahan Baku" },
                    { 2, null, "Kemasan" },
                    { 3, null, "Semi-finished" },
                    { 4, null, "Produk Jadi" }
                });

            migrationBuilder.InsertData(
                table: "uoms",
                columns: new[] { "id", "code", "factor_to_base", "is_base", "name", "type" },
                values: new object[] { 1, "kg", 1m, true, "Kilogram", "Weight" });

            migrationBuilder.InsertData(
                table: "uoms",
                columns: new[] { "id", "code", "factor_to_base", "name", "type" },
                values: new object[] { 2, "g", 0.001m, "Gram", "Weight" });

            migrationBuilder.InsertData(
                table: "uoms",
                columns: new[] { "id", "code", "factor_to_base", "is_base", "name", "type" },
                values: new object[] { 3, "L", 1m, true, "Liter", "Volume" });

            migrationBuilder.InsertData(
                table: "uoms",
                columns: new[] { "id", "code", "factor_to_base", "name", "type" },
                values: new object[] { 4, "ml", 0.001m, "Mililiter", "Volume" });

            migrationBuilder.InsertData(
                table: "uoms",
                columns: new[] { "id", "code", "factor_to_base", "is_base", "name", "type" },
                values: new object[] { 5, "pcs", 1m, true, "Pieces", "Count" });

            migrationBuilder.CreateIndex(
                name: "ix_item_categories_name",
                table: "item_categories",
                column: "name",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "ix_item_supplier_prices_item_id",
                table: "item_supplier_prices",
                column: "item_id");

            migrationBuilder.CreateIndex(
                name: "ix_item_supplier_prices_supplier_id",
                table: "item_supplier_prices",
                column: "supplier_id");

            migrationBuilder.CreateIndex(
                name: "ix_item_supplier_prices_uom_id",
                table: "item_supplier_prices",
                column: "uom_id");

            migrationBuilder.CreateIndex(
                name: "ix_items_base_uom_id",
                table: "items",
                column: "base_uom_id");

            migrationBuilder.CreateIndex(
                name: "ix_items_category_id",
                table: "items",
                column: "category_id");

            migrationBuilder.CreateIndex(
                name: "ix_items_code",
                table: "items",
                column: "code",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "ix_suppliers_code",
                table: "suppliers",
                column: "code",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "ix_uom_conversions_from_uom_id_to_uom_id",
                table: "uom_conversions",
                columns: new[] { "from_uom_id", "to_uom_id" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "ix_uom_conversions_to_uom_id",
                table: "uom_conversions",
                column: "to_uom_id");

            migrationBuilder.CreateIndex(
                name: "ix_uoms_code",
                table: "uoms",
                column: "code",
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "item_supplier_prices");

            migrationBuilder.DropTable(
                name: "uom_conversions");

            migrationBuilder.DropTable(
                name: "items");

            migrationBuilder.DropTable(
                name: "suppliers");

            migrationBuilder.DropTable(
                name: "item_categories");

            migrationBuilder.DropTable(
                name: "uoms");
        }
    }
}
