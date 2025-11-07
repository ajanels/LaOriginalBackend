using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace LaOriginalBackend.Migrations.Caja
{
    /// <inheritdoc />
    public partial class AddCostoPromedioToProductoStock : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<decimal>(
                name: "CostoPromedio",
                table: "ProductoStocks",
                type: "decimal(18,2)",
                precision: 18,
                scale: 2,
                nullable: false,
                defaultValue: 0m);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "CostoPromedio",
                table: "ProductoStocks");
        }
    }
}
