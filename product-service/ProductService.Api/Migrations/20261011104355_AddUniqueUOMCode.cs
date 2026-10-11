using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace ProductService.Api.Migrations
{
    /// <inheritdoc />
    public partial class AddUniqueUOMCode : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateIndex(
                name: "IX_UOMSetup_code",
                table: "UOMSetup",
                column: "code",
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_UOMSetup_code",
                table: "UOMSetup");
        }
    }
}
