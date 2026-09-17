using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace ProductService.Api.Migrations
{
    /// <inheritdoc />
    public partial class AddRawMaterials : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "RawMaterials",
                columns: table => new
                {
                    id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    material_code = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    material_name = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: false),
                    material_desc = table.Column<string>(type: "nvarchar(1000)", maxLength: 1000, nullable: true),
                    uom = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: true),
                    is_active = table.Column<bool>(type: "bit", nullable: false),
                    create_date = table.Column<DateTime>(type: "datetime2", nullable: false),
                    update_date = table.Column<DateTime>(type: "datetime2", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_RawMaterials", x => x.id);
                });

            migrationBuilder.CreateIndex(
                name: "IX_RawMaterials_is_active_material_name",
                table: "RawMaterials",
                columns: new[] { "is_active", "material_name" });

            migrationBuilder.CreateIndex(
                name: "IX_RawMaterials_material_code",
                table: "RawMaterials",
                column: "material_code",
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "RawMaterials");
        }
    }
}
