using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace ProductService.Api.Migrations
{
    /// <inheritdoc />
    public partial class create_multiuom : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "MultiUOMs",
                columns: table => new
                {
                    id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    alt_uom_id = table.Column<int>(type: "int", nullable: false),
                    base_uom_id = table.Column<int>(type: "int", nullable: false),
                    refType = table.Column<int>(type: "int", nullable: false),
                    ref_id = table.Column<string>(type: "nvarchar(450)", nullable: false),
                    conversion_factor = table.Column<decimal>(type: "decimal(10,6)", nullable: true),
                    created_at = table.Column<DateTime>(type: "datetime2", nullable: false),
                    updated_at = table.Column<DateTime>(type: "datetime2", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_MultiUOMs", x => x.id);
                    table.ForeignKey(
                        name: "FK_MultiUOM_UOMSetup_alt_uom_id",
                        column: x => x.alt_uom_id,
                        principalTable: "UOMSetup",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_MultiUOM_UOMSetup_base_uom_id",
                        column: x => x.base_uom_id,
                        principalTable: "UOMSetup",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "IX_MultiUOM_refType_refId_altUOMId",
                table: "MultiUOMs",
                columns: new[] { "refType", "ref_id", "alt_uom_id" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_MultiUOMs_alt_uom_id",
                table: "MultiUOMs",
                column: "alt_uom_id");

            migrationBuilder.CreateIndex(
                name: "IX_MultiUOMs_base_uom_id",
                table: "MultiUOMs",
                column: "base_uom_id");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "MultiUOMs");
        }
    }
}
