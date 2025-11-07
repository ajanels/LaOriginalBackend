using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace LaOriginalBackend.Migrations.Caja
{
    /// <inheritdoc />
    public partial class FixCascadePaths : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_ComprasDetalle_Presentaciones_PresentacionId",
                table: "ComprasDetalle");

            migrationBuilder.DropForeignKey(
                name: "FK_PedidosProveedoresDetalle_Presentaciones_PresentacionId",
                table: "PedidosProveedoresDetalle");

            migrationBuilder.AddForeignKey(
                name: "FK_ComprasDetalle_Presentaciones_PresentacionId",
                table: "ComprasDetalle",
                column: "PresentacionId",
                principalTable: "Presentaciones",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_PedidosProveedoresDetalle_Presentaciones_PresentacionId",
                table: "PedidosProveedoresDetalle",
                column: "PresentacionId",
                principalTable: "Presentaciones",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_ComprasDetalle_Presentaciones_PresentacionId",
                table: "ComprasDetalle");

            migrationBuilder.DropForeignKey(
                name: "FK_PedidosProveedoresDetalle_Presentaciones_PresentacionId",
                table: "PedidosProveedoresDetalle");

            migrationBuilder.AddForeignKey(
                name: "FK_ComprasDetalle_Presentaciones_PresentacionId",
                table: "ComprasDetalle",
                column: "PresentacionId",
                principalTable: "Presentaciones",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_PedidosProveedoresDetalle_Presentaciones_PresentacionId",
                table: "PedidosProveedoresDetalle",
                column: "PresentacionId",
                principalTable: "Presentaciones",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);
        }
    }
}
