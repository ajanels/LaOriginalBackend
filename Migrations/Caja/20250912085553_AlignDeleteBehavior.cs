using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable
namespace LaOriginalBackend.Migrations.Caja
{
    public partial class AlignDeleteBehavior : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            // ComprasDetalle -> Presentaciones  (CASCADE -> RESTRICT)
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

            // ComprasDetalle -> PedidosProveedoresDetalle (NO_ACTION/RESTRICT)
            migrationBuilder.DropForeignKey(
                name: "FK_ComprasDetalle_PedidosProveedoresDetalle_PedidoProveedorDetalleId",
                table: "ComprasDetalle");

            migrationBuilder.AddForeignKey(
                name: "FK_ComprasDetalle_PedidosProveedoresDetalle_PedidoProveedorDetalleId",
                table: "ComprasDetalle",
                column: "PedidoProveedorDetalleId",
                principalTable: "PedidosProveedoresDetalle",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
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

            migrationBuilder.DropForeignKey(
                name: "FK_ComprasDetalle_PedidosProveedoresDetalle_PedidoProveedorDetalleId",
                table: "ComprasDetalle");

            migrationBuilder.AddForeignKey(
                name: "FK_ComprasDetalle_PedidosProveedoresDetalle_PedidoProveedorDetalleId",
                table: "ComprasDetalle",
                column: "PedidoProveedorDetalleId",
                principalTable: "PedidosProveedoresDetalle",
                principalColumn: "Id",
                onDelete: ReferentialAction.NoAction);
        }
    }
}
