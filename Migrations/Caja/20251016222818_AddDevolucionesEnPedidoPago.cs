using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace LaOriginalBackend.Migrations.Caja
{
    /// <inheritdoc />
    public partial class AddDevolucionesEnPedidoPago : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<bool>(
                name: "EsDevolucion",
                table: "PedidoClientePago",
                type: "bit",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<int>(
                name: "PagoOriginalId",
                table: "PedidoClientePago",
                type: "int",
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "EsDevolucion",
                table: "PedidoClientePago");

            migrationBuilder.DropColumn(
                name: "PagoOriginalId",
                table: "PedidoClientePago");
        }
    }
}
