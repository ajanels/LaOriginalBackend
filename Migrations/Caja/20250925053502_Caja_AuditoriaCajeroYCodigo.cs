using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace LaOriginalBackend.Migrations.Caja
{
    /// <inheritdoc />
    public partial class Caja_AuditoriaCajeroYCodigo : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_CajaAperturas_Usuarios_UsuarioAperturaId",
                table: "CajaAperturas");

            migrationBuilder.DropForeignKey(
                name: "FK_CajaAperturas_Usuarios_UsuarioCierreId",
                table: "CajaAperturas");

            migrationBuilder.DropIndex(
                name: "IX_CajaAperturas_UsuarioAperturaId",
                table: "CajaAperturas");

            migrationBuilder.DropIndex(
                name: "IX_CajaAperturas_UsuarioCierreId",
                table: "CajaAperturas");

            migrationBuilder.DropColumn(
                name: "UsuarioAperturaId",
                table: "CajaAperturas");

            migrationBuilder.DropColumn(
                name: "UsuarioCierreId",
                table: "CajaAperturas");

            migrationBuilder.AddColumn<string>(
                name: "CajeroNombre",
                table: "CajaAperturas",
                type: "nvarchar(120)",
                maxLength: 120,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "Codigo",
                table: "CajaAperturas",
                type: "nvarchar(20)",
                maxLength: 20,
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "CajeroNombre",
                table: "CajaAperturas");

            migrationBuilder.DropColumn(
                name: "Codigo",
                table: "CajaAperturas");

            migrationBuilder.AddColumn<int>(
                name: "UsuarioAperturaId",
                table: "CajaAperturas",
                type: "int",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "UsuarioCierreId",
                table: "CajaAperturas",
                type: "int",
                nullable: true);

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
        }
    }
}
