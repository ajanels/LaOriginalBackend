using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace LaOriginalBackend.Migrations.Caja
{
    public partial class AddAfectaCajaToFormaPago : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            // 1) Agregar flags
            migrationBuilder.AddColumn<bool>(
                name: "AfectaBanco",
                table: "FormasPago",
                type: "bit",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<bool>(
                name: "AfectaCaja",
                table: "FormasPago",
                type: "bit",
                nullable: false,
                defaultValue: false);

            // 2) Inicializar valores para existentes
            migrationBuilder.Sql(@"
                -- Efectivo: afecta caja (no banco)
                UPDATE FormasPago
                   SET AfectaCaja = 1
                 WHERE LOWER(Nombre) IN (N'efectivo', N'cash', N'contado');

                -- Depósito/Transferencia: afecta caja y banco (si lo manejas como salida de caja)
                UPDATE FormasPago
                   SET AfectaCaja = 1,
                       AfectaBanco = 1
                 WHERE LOWER(Nombre) IN (N'deposito', N'depósito', N'deposit', N'transferencia');
            ");
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "AfectaBanco",
                table: "FormasPago");

            migrationBuilder.DropColumn(
                name: "AfectaCaja",
                table: "FormasPago");
        }
    }
}
