using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace LaOriginalBackend.Migrations
{
    /// <inheritdoc />
    public partial class ModelTweaks_Indices_Precision : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_Presentaciones_ProductoId",
                table: "Presentaciones");

            migrationBuilder.CreateIndex(
                name: "IX_UnidadesMedida_Simbolo",
                table: "UnidadesMedida",
                column: "Simbolo",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_Presentaciones_ProductoId_EsPrincipal",
                table: "Presentaciones",
                columns: new[] { "ProductoId", "EsPrincipal" },
                unique: true,
                filter: "[EsPrincipal] = 1");

            migrationBuilder.CreateIndex(
                name: "IX_Presentaciones_ProductoId_Nombre",
                table: "Presentaciones",
                columns: new[] { "ProductoId", "Nombre" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_Clientes_NIT",
                table: "Clientes",
                column: "NIT",
                unique: true,
                filter: "[NIT] IS NOT NULL");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_UnidadesMedida_Simbolo",
                table: "UnidadesMedida");

            migrationBuilder.DropIndex(
                name: "IX_Presentaciones_ProductoId_EsPrincipal",
                table: "Presentaciones");

            migrationBuilder.DropIndex(
                name: "IX_Presentaciones_ProductoId_Nombre",
                table: "Presentaciones");

            migrationBuilder.DropIndex(
                name: "IX_Clientes_NIT",
                table: "Clientes");

            migrationBuilder.CreateIndex(
                name: "IX_Presentaciones_ProductoId",
                table: "Presentaciones",
                column: "ProductoId");
        }
    }
}
