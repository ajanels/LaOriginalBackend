using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace LaOriginalBackend.Migrations.Caja
{
    /// <inheritdoc />
    public partial class PedidoCliente_AddVentaId : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "VentaId",
                table: "PedidoCliente",
                type: "int",
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_PedidoCliente_VentaId",
                table: "PedidoCliente",
                column: "VentaId");

            migrationBuilder.AddForeignKey(
                name: "FK_PedidoCliente_Ventas_VentaId",
                table: "PedidoCliente",
                column: "VentaId",
                principalTable: "Ventas",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_PedidoCliente_Ventas_VentaId",
                table: "PedidoCliente");

            migrationBuilder.DropIndex(
                name: "IX_PedidoCliente_VentaId",
                table: "PedidoCliente");

            migrationBuilder.DropColumn(
                name: "VentaId",
                table: "PedidoCliente");
        }
    }
}
