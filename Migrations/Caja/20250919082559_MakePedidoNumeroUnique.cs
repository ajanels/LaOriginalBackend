using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace LaOriginalBackend.Migrations.Caja
{
    /// <inheritdoc />
    public partial class MakePedidoNumeroUnique : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_PedidosProveedores_Numero",
                table: "PedidosProveedores");

            migrationBuilder.CreateIndex(
                name: "IX_PedidosProveedores_Numero",
                table: "PedidosProveedores",
                column: "Numero",
                unique: true,
                filter: "[Numero] IS NOT NULL");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_PedidosProveedores_Numero",
                table: "PedidosProveedores");

            migrationBuilder.CreateIndex(
                name: "IX_PedidosProveedores_Numero",
                table: "PedidosProveedores",
                column: "Numero");
        }
    }
}
