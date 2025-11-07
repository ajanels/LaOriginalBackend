using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace LaOriginalBackend.Migrations.Caja
{
    /// <inheritdoc />
    public partial class PedidosClientes_Pagos : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "PedidoClientePago",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    PedidoClienteId = table.Column<int>(type: "int", nullable: false),
                    FechaUtc = table.Column<DateTime>(type: "datetime2", nullable: false),
                    FormaPagoId = table.Column<int>(type: "int", nullable: false),
                    FormaPagoNombre = table.Column<string>(type: "nvarchar(80)", maxLength: 80, nullable: false),
                    Monto = table.Column<decimal>(type: "decimal(18,2)", precision: 18, scale: 2, nullable: false),
                    Referencia = table.Column<string>(type: "nvarchar(60)", maxLength: 60, nullable: true),
                    Notas = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: true),
                    UsuarioId = table.Column<int>(type: "int", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_PedidoClientePago", x => x.Id);
                    table.ForeignKey(
                        name: "FK_PedidoClientePago_PedidoCliente_PedidoClienteId",
                        column: x => x.PedidoClienteId,
                        principalTable: "PedidoCliente",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_PedidoClientePago_FormaPagoId",
                table: "PedidoClientePago",
                column: "FormaPagoId");

            migrationBuilder.CreateIndex(
                name: "IX_PedidoClientePago_PedidoClienteId_FechaUtc",
                table: "PedidoClientePago",
                columns: new[] { "PedidoClienteId", "FechaUtc" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "PedidoClientePago");
        }
    }
}
