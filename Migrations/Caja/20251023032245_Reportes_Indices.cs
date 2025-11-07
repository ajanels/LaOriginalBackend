using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace LaOriginalBackend.Migrations.Caja
{
    /// <inheritdoc />
    public partial class Reportes_Indices : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateIndex(
                name: "IX_Ventas_Fecha",
                table: "Ventas",
                column: "Fecha");

            migrationBuilder.CreateIndex(
                name: "IX_Compras_Fecha",
                table: "Compras",
                column: "Fecha");

            migrationBuilder.CreateIndex(
                name: "IX_CajaMovimientos_CajaAperturaId",
                table: "CajaMovimientos",
                column: "CajaAperturaId");

            migrationBuilder.CreateIndex(
                name: "IX_CajaMovimientos_FechaUtc",
                table: "CajaMovimientos",
                column: "FechaUtc");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_Ventas_Fecha",
                table: "Ventas");

            migrationBuilder.DropIndex(
                name: "IX_Compras_Fecha",
                table: "Compras");

            migrationBuilder.DropIndex(
                name: "IX_CajaMovimientos_CajaAperturaId",
                table: "CajaMovimientos");

            migrationBuilder.DropIndex(
                name: "IX_CajaMovimientos_FechaUtc",
                table: "CajaMovimientos");
        }
    }
}
