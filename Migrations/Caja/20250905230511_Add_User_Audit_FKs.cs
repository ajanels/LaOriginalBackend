using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace LaOriginalBackend.Migrations.Caja
{
    /// <inheritdoc />
    public partial class Add_User_Audit_FKs : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateIndex(
                name: "IX_Ventas_UsuarioId",
                table: "Ventas",
                column: "UsuarioId");

            migrationBuilder.CreateIndex(
                name: "IX_MovimientosInventario_UsuarioId",
                table: "MovimientosInventario",
                column: "UsuarioId");

            migrationBuilder.CreateIndex(
                name: "IX_DevolucionesVenta_UsuarioId",
                table: "DevolucionesVenta",
                column: "UsuarioId");

            migrationBuilder.CreateIndex(
                name: "IX_DevolucionesCompra_UsuarioId",
                table: "DevolucionesCompra",
                column: "UsuarioId");

            migrationBuilder.CreateIndex(
                name: "IX_Compras_UsuarioId",
                table: "Compras",
                column: "UsuarioId");

            migrationBuilder.CreateIndex(
                name: "IX_CajaMovimientos_UsuarioId",
                table: "CajaMovimientos",
                column: "UsuarioId");

            migrationBuilder.CreateIndex(
                name: "IX_CajaAperturas_UsuarioAperturaId",
                table: "CajaAperturas",
                column: "UsuarioAperturaId");

            migrationBuilder.CreateIndex(
                name: "IX_CajaAperturas_UsuarioCierreId",
                table: "CajaAperturas",
                column: "UsuarioCierreId");

            migrationBuilder.AddForeignKey(
                name: "FK_CajaAperturas_Usuarios_UsuarioAperturaId",
                table: "CajaAperturas",
                column: "UsuarioAperturaId",
                principalTable: "Usuarios",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_CajaAperturas_Usuarios_UsuarioCierreId",
                table: "CajaAperturas",
                column: "UsuarioCierreId",
                principalTable: "Usuarios",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_CajaMovimientos_Usuarios_UsuarioId",
                table: "CajaMovimientos",
                column: "UsuarioId",
                principalTable: "Usuarios",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_Compras_Usuarios_UsuarioId",
                table: "Compras",
                column: "UsuarioId",
                principalTable: "Usuarios",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_DevolucionesCompra_Usuarios_UsuarioId",
                table: "DevolucionesCompra",
                column: "UsuarioId",
                principalTable: "Usuarios",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_DevolucionesVenta_Usuarios_UsuarioId",
                table: "DevolucionesVenta",
                column: "UsuarioId",
                principalTable: "Usuarios",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_MovimientosInventario_Usuarios_UsuarioId",
                table: "MovimientosInventario",
                column: "UsuarioId",
                principalTable: "Usuarios",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_Ventas_Usuarios_UsuarioId",
                table: "Ventas",
                column: "UsuarioId",
                principalTable: "Usuarios",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_CajaAperturas_Usuarios_UsuarioAperturaId",
                table: "CajaAperturas");

            migrationBuilder.DropForeignKey(
                name: "FK_CajaAperturas_Usuarios_UsuarioCierreId",
                table: "CajaAperturas");

            migrationBuilder.DropForeignKey(
                name: "FK_CajaMovimientos_Usuarios_UsuarioId",
                table: "CajaMovimientos");

            migrationBuilder.DropForeignKey(
                name: "FK_Compras_Usuarios_UsuarioId",
                table: "Compras");

            migrationBuilder.DropForeignKey(
                name: "FK_DevolucionesCompra_Usuarios_UsuarioId",
                table: "DevolucionesCompra");

            migrationBuilder.DropForeignKey(
                name: "FK_DevolucionesVenta_Usuarios_UsuarioId",
                table: "DevolucionesVenta");

            migrationBuilder.DropForeignKey(
                name: "FK_MovimientosInventario_Usuarios_UsuarioId",
                table: "MovimientosInventario");

            migrationBuilder.DropForeignKey(
                name: "FK_Ventas_Usuarios_UsuarioId",
                table: "Ventas");

            migrationBuilder.DropIndex(
                name: "IX_Ventas_UsuarioId",
                table: "Ventas");

            migrationBuilder.DropIndex(
                name: "IX_MovimientosInventario_UsuarioId",
                table: "MovimientosInventario");

            migrationBuilder.DropIndex(
                name: "IX_DevolucionesVenta_UsuarioId",
                table: "DevolucionesVenta");

            migrationBuilder.DropIndex(
                name: "IX_DevolucionesCompra_UsuarioId",
                table: "DevolucionesCompra");

            migrationBuilder.DropIndex(
                name: "IX_Compras_UsuarioId",
                table: "Compras");

            migrationBuilder.DropIndex(
                name: "IX_CajaMovimientos_UsuarioId",
                table: "CajaMovimientos");

            migrationBuilder.DropIndex(
                name: "IX_CajaAperturas_UsuarioAperturaId",
                table: "CajaAperturas");

            migrationBuilder.DropIndex(
                name: "IX_CajaAperturas_UsuarioCierreId",
                table: "CajaAperturas");
        }
    }
}
