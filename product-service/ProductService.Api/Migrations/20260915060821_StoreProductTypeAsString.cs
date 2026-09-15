using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace ProductService.Api.Migrations
{
    /// <inheritdoc />
    public partial class StoreProductTypeAsString : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AlterColumn<string>(
                name: "product_type",
                table: "Products",
                type: "nvarchar(max)",
                nullable: false,
                oldClrType: typeof(int),
                oldType: "int");

            migrationBuilder.Sql("""
                UPDATE Products
                SET product_type = CASE product_type
                    WHEN '0' THEN 'Unknown'
                    WHEN '1' THEN 'Food'
                    WHEN '2' THEN 'Beverage'
                    WHEN '3' THEN 'Addon'
                    WHEN '4' THEN 'Combo'
                    WHEN '5' THEN 'Packaging'
                    ELSE 'Unknown'
                END;
                """);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql("""
                UPDATE Products
                SET product_type = CASE product_type
                    WHEN 'Unknown' THEN '0'
                    WHEN 'Food' THEN '1'
                    WHEN 'Beverage' THEN '2'
                    WHEN 'Addon' THEN '3'
                    WHEN 'Combo' THEN '4'
                    WHEN 'Packaging' THEN '5'
                    ELSE '0'
                END;
                """);

            migrationBuilder.AlterColumn<int>(
                name: "product_type",
                table: "Products",
                type: "int",
                nullable: false,
                oldClrType: typeof(string),
                oldType: "nvarchar(max)");
        }
    }
}
