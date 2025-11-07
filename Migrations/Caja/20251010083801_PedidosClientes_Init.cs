using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace LaOriginalBackend.Migrations.Caja
{
    /// <inheritdoc />
    public partial class PedidosClientes_Init : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_Proveedores_NIT",
                table: "Proveedores");

            migrationBuilder.CreateTable(
                name: "PedidoCliente",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    FechaCreacionUtc = table.Column<DateTime>(type: "datetime2", nullable: false),
                    ClienteId = table.Column<int>(type: "int", nullable: false),
                    ClienteNombre = table.Column<string>(type: "nvarchar(150)", maxLength: 150, nullable: false),
                    Telefono = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: true),
                    DireccionEntrega = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: true),
                    FechaEntregaCompromisoUtc = table.Column<DateTime>(type: "datetime2", nullable: true),
                    Estado = table.Column<int>(type: "int", nullable: false),
                    Observaciones = table.Column<string>(type: "nvarchar(400)", maxLength: 400, nullable: true),
                    Subtotal = table.Column<decimal>(type: "decimal(18,2)", precision: 18, scale: 2, nullable: false),
                    Descuento = table.Column<decimal>(type: "decimal(18,2)", precision: 18, scale: 2, nullable: false),
                    Total = table.Column<decimal>(type: "decimal(18,2)", precision: 18, scale: 2, nullable: false),
                    UsuarioId = table.Column<int>(type: "int", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_PedidoCliente", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "PedidoClienteDetalle",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    PedidoClienteId = table.Column<int>(type: "int", nullable: false),
                    PresentacionId = table.Column<int>(type: "int", nullable: false),
                    PresentacionNombre = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: true),
                    Cantidad = table.Column<decimal>(type: "decimal(18,2)", precision: 18, scale: 2, nullable: false),
                    PrecioUnitario = table.Column<decimal>(type: "decimal(18,2)", precision: 18, scale: 2, nullable: false),
                    DescuentoUnitario = table.Column<decimal>(type: "decimal(18,2)", precision: 18, scale: 2, nullable: false),
                    TotalLinea = table.Column<decimal>(type: "decimal(18,2)", precision: 18, scale: 2, nullable: false),
                    Notas = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_PedidoClienteDetalle", x => x.Id);
                    table.ForeignKey(
                        name: "FK_PedidoClienteDetalle_PedidoCliente_PedidoClienteId",
                        column: x => x.PedidoClienteId,
                        principalTable: "PedidoCliente",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_PedidoClienteDetalle_Presentaciones_PresentacionId",
                        column: x => x.PresentacionId,
                        principalTable: "Presentaciones",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "PedidoClienteReserva",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    PedidoClienteId = table.Column<int>(type: "int", nullable: false),
                    PresentacionId = table.Column<int>(type: "int", nullable: false),
                    Cantidad = table.Column<decimal>(type: "decimal(18,2)", precision: 18, scale: 2, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_PedidoClienteReserva", x => x.Id);
                    table.ForeignKey(
                        name: "FK_PedidoClienteReserva_PedidoCliente_PedidoClienteId",
                        column: x => x.PedidoClienteId,
                        principalTable: "PedidoCliente",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_PedidoClienteReserva_Presentaciones_PresentacionId",
                        column: x => x.PresentacionId,
                        principalTable: "Presentaciones",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "IX_Proveedores_NIT",
                table: "Proveedores",
                column: "NIT",
                unique: true,
                filter: "[NIT] IS NOT NULL");

            migrationBuilder.CreateIndex(
                name: "IX_PedidoCliente_ClienteId_Estado_FechaCreacionUtc",
                table: "PedidoCliente",
                columns: new[] { "ClienteId", "Estado", "FechaCreacionUtc" });

            migrationBuilder.CreateIndex(
                name: "IX_PedidoClienteDetalle_PedidoClienteId_PresentacionId",
                table: "PedidoClienteDetalle",
                columns: new[] { "PedidoClienteId", "PresentacionId" });

            migrationBuilder.CreateIndex(
                name: "IX_PedidoClienteDetalle_PresentacionId",
                table: "PedidoClienteDetalle",
                column: "PresentacionId");

            migrationBuilder.CreateIndex(
                name: "IX_PedidoClienteReserva_PedidoClienteId_PresentacionId",
                table: "PedidoClienteReserva",
                columns: new[] { "PedidoClienteId", "PresentacionId" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_PedidoClienteReserva_PresentacionId",
                table: "PedidoClienteReserva",
                column: "PresentacionId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "PedidoClienteDetalle");

            migrationBuilder.DropTable(
                name: "PedidoClienteReserva");

            migrationBuilder.DropTable(
                name: "PedidoCliente");

            migrationBuilder.DropIndex(
                name: "IX_Proveedores_NIT",
                table: "Proveedores");

            migrationBuilder.CreateIndex(
                name: "IX_Proveedores_NIT",
                table: "Proveedores",
                column: "NIT",
                unique: true);
        }
    }
}
