using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace LaOriginalBackend.Migrations.Devoluciones
{
    /// <inheritdoc />
    public partial class Devoluciones_Inicial : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "DevolucionesCompra",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    Fecha = table.Column<DateTime>(type: "datetime2", nullable: false),
                    CompraId = table.Column<int>(type: "int", nullable: true),
                    ProveedorId = table.Column<int>(type: "int", nullable: false),
                    Numero = table.Column<string>(type: "nvarchar(40)", maxLength: 40, nullable: true),
                    Observaciones = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: true),
                    Subtotal = table.Column<decimal>(type: "decimal(18,2)", precision: 18, scale: 2, nullable: false),
                    Descuento = table.Column<decimal>(type: "decimal(18,2)", precision: 18, scale: 2, nullable: false),
                    Total = table.Column<decimal>(type: "decimal(18,2)", precision: 18, scale: 2, nullable: false),
                    Estado = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: false),
                    Anulada = table.Column<bool>(type: "bit", nullable: false),
                    FormaPagoId = table.Column<int>(type: "int", nullable: true),
                    UsuarioId = table.Column<int>(type: "int", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_DevolucionesCompra", x => x.Id);
                    table.ForeignKey(
                        name: "FK_DevolucionesCompra_Compras_CompraId",
                        column: x => x.CompraId,
                        principalTable: "Compras",
                        principalColumn: "Id");
                    table.ForeignKey(
                        name: "FK_DevolucionesCompra_FormasPago_FormaPagoId",
                        column: x => x.FormaPagoId,
                        principalTable: "FormasPago",
                        principalColumn: "Id");
                    table.ForeignKey(
                        name: "FK_DevolucionesCompra_Proveedores_ProveedorId",
                        column: x => x.ProveedorId,
                        principalTable: "Proveedores",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "DevolucionesVenta",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    Fecha = table.Column<DateTime>(type: "datetime2", nullable: false),
                    VentaId = table.Column<int>(type: "int", nullable: true),
                    ClienteId = table.Column<int>(type: "int", nullable: true),
                    Numero = table.Column<string>(type: "nvarchar(40)", maxLength: 40, nullable: true),
                    Observaciones = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: true),
                    Subtotal = table.Column<decimal>(type: "decimal(18,2)", precision: 18, scale: 2, nullable: false),
                    Descuento = table.Column<decimal>(type: "decimal(18,2)", precision: 18, scale: 2, nullable: false),
                    Total = table.Column<decimal>(type: "decimal(18,2)", precision: 18, scale: 2, nullable: false),
                    Estado = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: false),
                    Anulada = table.Column<bool>(type: "bit", nullable: false),
                    FormaPagoId = table.Column<int>(type: "int", nullable: true),
                    UsuarioId = table.Column<int>(type: "int", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_DevolucionesVenta", x => x.Id);
                    table.ForeignKey(
                        name: "FK_DevolucionesVenta_Clientes_ClienteId",
                        column: x => x.ClienteId,
                        principalTable: "Clientes",
                        principalColumn: "Id");
                    table.ForeignKey(
                        name: "FK_DevolucionesVenta_FormasPago_FormaPagoId",
                        column: x => x.FormaPagoId,
                        principalTable: "FormasPago",
                        principalColumn: "Id");
                    table.ForeignKey(
                        name: "FK_DevolucionesVenta_Ventas_VentaId",
                        column: x => x.VentaId,
                        principalTable: "Ventas",
                        principalColumn: "Id");
                });

            migrationBuilder.CreateTable(
                name: "DevolucionesCompraDetalle",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    DevolucionCompraId = table.Column<int>(type: "int", nullable: false),
                    PresentacionId = table.Column<int>(type: "int", nullable: false),
                    Cantidad = table.Column<decimal>(type: "decimal(18,2)", precision: 18, scale: 2, nullable: false),
                    CostoUnitario = table.Column<decimal>(type: "decimal(18,2)", precision: 18, scale: 2, nullable: false),
                    TotalLinea = table.Column<decimal>(type: "decimal(18,2)", precision: 18, scale: 2, nullable: false),
                    Notas = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_DevolucionesCompraDetalle", x => x.Id);
                    table.ForeignKey(
                        name: "FK_DevolucionesCompraDetalle_DevolucionesCompra_DevolucionCompraId",
                        column: x => x.DevolucionCompraId,
                        principalTable: "DevolucionesCompra",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_DevolucionesCompraDetalle_Presentaciones_PresentacionId",
                        column: x => x.PresentacionId,
                        principalTable: "Presentaciones",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "DevolucionesVentaDetalle",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    DevolucionVentaId = table.Column<int>(type: "int", nullable: false),
                    PresentacionId = table.Column<int>(type: "int", nullable: false),
                    Cantidad = table.Column<decimal>(type: "decimal(18,2)", precision: 18, scale: 2, nullable: false),
                    PrecioUnitario = table.Column<decimal>(type: "decimal(18,2)", precision: 18, scale: 2, nullable: false),
                    DescuentoUnitario = table.Column<decimal>(type: "decimal(18,2)", precision: 18, scale: 2, nullable: false),
                    TotalLinea = table.Column<decimal>(type: "decimal(18,2)", precision: 18, scale: 2, nullable: false),
                    Notas = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_DevolucionesVentaDetalle", x => x.Id);
                    table.ForeignKey(
                        name: "FK_DevolucionesVentaDetalle_DevolucionesVenta_DevolucionVentaId",
                        column: x => x.DevolucionVentaId,
                        principalTable: "DevolucionesVenta",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_DevolucionesVentaDetalle_Presentaciones_PresentacionId",
                        column: x => x.PresentacionId,
                        principalTable: "Presentaciones",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_DevolucionesCompra_CompraId",
                table: "DevolucionesCompra",
                column: "CompraId");

            migrationBuilder.CreateIndex(
                name: "IX_DevolucionesCompra_FormaPagoId",
                table: "DevolucionesCompra",
                column: "FormaPagoId");

            migrationBuilder.CreateIndex(
                name: "IX_DevolucionesCompra_ProveedorId",
                table: "DevolucionesCompra",
                column: "ProveedorId");

            migrationBuilder.CreateIndex(
                name: "IX_DevolucionesCompraDetalle_DevolucionCompraId",
                table: "DevolucionesCompraDetalle",
                column: "DevolucionCompraId");

            migrationBuilder.CreateIndex(
                name: "IX_DevolucionesCompraDetalle_PresentacionId",
                table: "DevolucionesCompraDetalle",
                column: "PresentacionId");

            migrationBuilder.CreateIndex(
                name: "IX_DevolucionesVenta_ClienteId",
                table: "DevolucionesVenta",
                column: "ClienteId");

            migrationBuilder.CreateIndex(
                name: "IX_DevolucionesVenta_FormaPagoId",
                table: "DevolucionesVenta",
                column: "FormaPagoId");

            migrationBuilder.CreateIndex(
                name: "IX_DevolucionesVenta_VentaId",
                table: "DevolucionesVenta",
                column: "VentaId");

            migrationBuilder.CreateIndex(
                name: "IX_DevolucionesVentaDetalle_DevolucionVentaId",
                table: "DevolucionesVentaDetalle",
                column: "DevolucionVentaId");

            migrationBuilder.CreateIndex(
                name: "IX_DevolucionesVentaDetalle_PresentacionId",
                table: "DevolucionesVentaDetalle",
                column: "PresentacionId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "DevolucionesCompraDetalle");

            migrationBuilder.DropTable(
                name: "DevolucionesVentaDetalle");

            migrationBuilder.DropTable(
                name: "DevolucionesCompra");

            migrationBuilder.DropTable(
                name: "DevolucionesVenta");
        }
    }
}
