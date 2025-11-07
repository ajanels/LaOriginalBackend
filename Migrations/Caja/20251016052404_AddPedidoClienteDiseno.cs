using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace LaOriginalBackend.Migrations.Caja
{
    /// <inheritdoc />
    public partial class AddPedidoClienteDiseno : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<bool>(
                name: "Diseno_Brich",
                table: "PedidoCliente",
                type: "bit",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<string>(
                name: "Diseno_Color",
                table: "PedidoCliente",
                type: "nvarchar(60)",
                maxLength: 60,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "Diseno_Extra",
                table: "PedidoCliente",
                type: "nvarchar(200)",
                maxLength: 200,
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "Diseno_Lienzos",
                table: "PedidoCliente",
                type: "int",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<string>(
                name: "Diseno_Otros",
                table: "PedidoCliente",
                type: "nvarchar(200)",
                maxLength: 200,
                nullable: true);

            migrationBuilder.AddColumn<bool>(
                name: "Diseno_Reportado",
                table: "PedidoCliente",
                type: "bit",
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "Diseno_Brich",
                table: "PedidoCliente");

            migrationBuilder.DropColumn(
                name: "Diseno_Color",
                table: "PedidoCliente");

            migrationBuilder.DropColumn(
                name: "Diseno_Extra",
                table: "PedidoCliente");

            migrationBuilder.DropColumn(
                name: "Diseno_Lienzos",
                table: "PedidoCliente");

            migrationBuilder.DropColumn(
                name: "Diseno_Otros",
                table: "PedidoCliente");

            migrationBuilder.DropColumn(
                name: "Diseno_Reportado",
                table: "PedidoCliente");
        }
    }
}
