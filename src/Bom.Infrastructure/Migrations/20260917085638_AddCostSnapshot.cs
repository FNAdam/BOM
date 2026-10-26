using System;
using Microsoft.EntityFrameworkCore.Migrations;
using Npgsql.EntityFrameworkCore.PostgreSQL.Metadata;

#nullable disable

namespace Bom.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddCostSnapshot : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "cost_snapshots",
                columns: table => new
                {
                    id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    item_id = table.Column<int>(type: "integer", nullable: false),
                    as_of_date = table.Column<DateOnly>(type: "date", nullable: false),
                    material_cost = table.Column<decimal>(type: "numeric(18,4)", precision: 18, scale: 4, nullable: false),
                    labor_cost = table.Column<decimal>(type: "numeric(18,4)", precision: 18, scale: 4, nullable: false),
                    overhead_cost = table.Column<decimal>(type: "numeric(18,4)", precision: 18, scale: 4, nullable: false),
                    total_cost = table.Column<decimal>(type: "numeric(18,4)", precision: 18, scale: 4, nullable: false),
                    output_uom_id = table.Column<int>(type: "integer", nullable: false),
                    source_bom_id = table.Column<int>(type: "integer", nullable: true),
                    bom_version = table.Column<int>(type: "integer", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_cost_snapshots", x => x.id);
                    table.ForeignKey(
                        name: "fk_cost_snapshots_boms_source_bom_id",
                        column: x => x.source_bom_id,
                        principalTable: "boms",
                        principalColumn: "id",
                        onDelete: ReferentialAction.SetNull);
                    table.ForeignKey(
                        name: "fk_cost_snapshots_items_item_id",
                        column: x => x.item_id,
                        principalTable: "items",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "fk_cost_snapshots_uoms_output_uom_id",
                        column: x => x.output_uom_id,
                        principalTable: "uoms",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "ix_cost_snapshots_item_id_as_of_date",
                table: "cost_snapshots",
                columns: new[] { "item_id", "as_of_date" });

            migrationBuilder.CreateIndex(
                name: "ix_cost_snapshots_output_uom_id",
                table: "cost_snapshots",
                column: "output_uom_id");

            migrationBuilder.CreateIndex(
                name: "ix_cost_snapshots_source_bom_id",
                table: "cost_snapshots",
                column: "source_bom_id");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "cost_snapshots");
        }
    }
}
