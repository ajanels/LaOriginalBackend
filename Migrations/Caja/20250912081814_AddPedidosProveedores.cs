using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace LaOriginalBackend.Migrations.Caja
{
    public partial class AddPedidosProveedores : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "FotoUrl",
                table: "Productos",
                type: "nvarchar(300)",
                maxLength: 300,
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "PedidoProveedorDetalleId",
                table: "ComprasDetalle",
                type: "int",
                nullable: true);

            migrationBuilder.AlterColumn<string>(
                name: "NIT",
                table: "Clientes",
                type: "nvarchar(9)",
                maxLength: 9,
                nullable: true,
                oldClrType: typeof(string),
                oldType: "nvarchar(20)",
                oldMaxLength: 20,
                oldNullable: true);

            // ⚠️ RECREAR el FK existente para quitar CASCADE
            migrationBuilder.DropForeignKey(
                name: "FK_ComprasDetalle_Presentaciones_PresentacionId",
                table: "ComprasDetalle");

            migrationBuilder.AddForeignKey(
                name: "FK_ComprasDetalle_Presentaciones_PresentacionId",
                table: "ComprasDetalle",
                column: "PresentacionId",
                principalTable: "Presentaciones",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.CreateTable(
                name: "PedidosProveedores",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    Fecha = table.Column<DateTime>(type: "datetime2", nullable: false),
                    Numero = table.Column<string>(type: "nvarchar(40)", maxLength: 40, nullable: true),
                    ProveedorId = table.Column<int>(type: "int", nullable: false),
                    Observaciones = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: true),
                    Subtotal = table.Column<decimal>(type: "decimal(18,2)", precision: 18, scale: 2, nullable: false),
                    Descuento = table.Column<decimal>(type: "decimal(18,2)", precision: 18, scale: 2, nullable: false),
                    Total = table.Column<decimal>(type: "decimal(18,2)", precision: 18, scale: 2, nullable: false),
                    Estado = table.Column<string>(type: "nvarchar(30)", maxLength: 30, nullable: false),
                    UsuarioCreaId = table.Column<int>(type: "int", nullable: true),
                    UsuarioApruebaId = table.Column<int>(type: "int", nullable: true),
                    AprobadoEl = table.Column<DateTime>(type: "datetime2", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_PedidosProveedores", x => x.Id);
                    table.ForeignKey(
                        name: "FK_PedidosProveedores_Proveedores_ProveedorId",
                        column: x => x.ProveedorId,
                        principalTable: "Proveedores",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_PedidosProveedores_Usuarios_UsuarioApruebaId",
                        column: x => x.UsuarioApruebaId,
                        principalTable: "Usuarios",
                        principalColumn: "Id");
                    table.ForeignKey(
                        name: "FK_PedidosProveedores_Usuarios_UsuarioCreaId",
                        column: x => x.UsuarioCreaId,
                        principalTable: "Usuarios",
                        principalColumn: "Id");
                });

            migrationBuilder.CreateTable(
                name: "PedidosProveedoresDetalle",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    PedidoProveedorId = table.Column<int>(type: "int", nullable: false),
                    PresentacionId = table.Column<int>(type: "int", nullable: false),
                    Cantidad = table.Column<decimal>(type: "decimal(18,2)", precision: 18, scale: 2, nullable: false),
                    PrecioUnitario = table.Column<decimal>(type: "decimal(18,2)", precision: 18, scale: 2, nullable: false),
                    Descuento = table.Column<decimal>(type: "decimal(18,2)", precision: 18, scale: 2, nullable: false),
                    TotalLinea = table.Column<decimal>(type: "decimal(18,2)", precision: 18, scale: 2, nullable: false),
                    CantidadRecibida = table.Column<decimal>(type: "decimal(18,2)", precision: 18, scale: 2, nullable: false),
                    Notas = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_PedidosProveedoresDetalle", x => x.Id);
                    table.ForeignKey(
                        name: "FK_PedidosProveedoresDetalle_PedidosProveedores_PedidoProveedorId",
                        column: x => x.PedidoProveedorId,
                        principalTable: "PedidosProveedores",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_PedidosProveedoresDetalle_Presentaciones_PresentacionId",
                        column: x => x.PresentacionId,
                        principalTable: "Presentaciones",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.NoAction); // sin cascada
                });

            migrationBuilder.CreateIndex(
                name: "IX_ComprasDetalle_PedidoProveedorDetalleId",
                table: "ComprasDetalle",
                column: "PedidoProveedorDetalleId");

            migrationBuilder.CreateIndex(
                name: "IX_PedidosProveedores_Numero",
                table: "PedidosProveedores",
                column: "Numero");

            migrationBuilder.CreateIndex(
                name: "IX_PedidosProveedores_ProveedorId",
                table: "PedidosProveedores",
                column: "ProveedorId");

            migrationBuilder.CreateIndex(
                name: "IX_PedidosProveedores_UsuarioApruebaId",
                table: "PedidosProveedores",
                column: "UsuarioApruebaId");

            migrationBuilder.CreateIndex(
                name: "IX_PedidosProveedores_UsuarioCreaId",
                table: "PedidosProveedores",
                column: "UsuarioCreaId");

            migrationBuilder.CreateIndex(
                name: "IX_PedidosProveedoresDetalle_PedidoProveedorId",
                table: "PedidosProveedoresDetalle",
                column: "PedidoProveedorId");

            migrationBuilder.CreateIndex(
                name: "IX_PedidosProveedoresDetalle_PresentacionId",
                table: "PedidosProveedoresDetalle",
                column: "PresentacionId");

            // 🔴 este FK nuevo también sin cascada para evitar rutas múltiples
            migrationBuilder.AddForeignKey(
                name: "FK_ComprasDetalle_PedidosProveedoresDetalle_PedidoProveedorDetalleId",
                table: "ComprasDetalle",
                column: "PedidoProveedorDetalleId",
                principalTable: "PedidosProveedoresDetalle",
                principalColumn: "Id",
                onDelete: ReferentialAction.NoAction);
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_ComprasDetalle_PedidosProveedoresDetalle_PedidoProveedorDetalleId",
                table: "ComprasDetalle");

            migrationBuilder.DropTable(
                name: "PedidosProveedoresDetalle");

            migrationBuilder.DropTable(
                name: "PedidosProveedores");

            migrationBuilder.DropIndex(
                name: "IX_ComprasDetalle_PedidoProveedorDetalleId",
                table: "ComprasDetalle");

            // Revertir el FK a Presentaciones a CASCADE
            migrationBuilder.DropForeignKey(
                name: "FK_ComprasDetalle_Presentaciones_PresentacionId",
                table: "ComprasDetalle");

            migrationBuilder.AddForeignKey(
                name: "FK_ComprasDetalle_Presentaciones_PresentacionId",
                table: "ComprasDetalle",
                column: "PresentacionId",
                principalTable: "Presentaciones",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.DropColumn(
                name: "FotoUrl",
                table: "Productos");

            migrationBuilder.DropColumn(
                name: "PedidoProveedorDetalleId",
                table: "ComprasDetalle");

            migrationBuilder.AlterColumn<string>(
                name: "NIT",
                table: "Clientes",
                type: "nvarchar(20)",
                maxLength: 20,
                nullable: true,
                oldClrType: typeof(string),
                oldType: "nvarchar(9)",
                oldMaxLength: 9,
                oldNullable: true);
        }
    }
}
