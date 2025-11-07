using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace LaOriginalBackend.Migrations.Caja
{
    /// <inheritdoc />
    public partial class CajaApertura_Sync : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_CajaMovimientos_Cajas_CajaId",
                table: "CajaMovimientos");

            migrationBuilder.DropTable(
                name: "CajaArqueos");

            migrationBuilder.DropTable(
                name: "Cajas");

            migrationBuilder.RenameColumn(
                name: "CajaId",
                table: "CajaMovimientos",
                newName: "CajaAperturaId");

            migrationBuilder.RenameIndex(
                name: "IX_CajaMovimientos_CajaId_FechaUtc",
                table: "CajaMovimientos",
                newName: "IX_CajaMovimientos_CajaAperturaId_FechaUtc");

            migrationBuilder.CreateTable(
                name: "CajaAperturas",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    FechaAperturaUtc = table.Column<DateTime>(type: "datetime2", nullable: false),
                    UsuarioAperturaId = table.Column<int>(type: "int", nullable: true),
                    MontoInicial = table.Column<decimal>(type: "decimal(18,2)", precision: 18, scale: 2, nullable: false),
                    ObservacionesApertura = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: true),
                    FechaCierreUtc = table.Column<DateTime>(type: "datetime2", nullable: true),
                    UsuarioCierreId = table.Column<int>(type: "int", nullable: true),
                    MontoCierreDeclarado = table.Column<decimal>(type: "decimal(18,2)", precision: 18, scale: 2, nullable: true),
                    ObservacionesCierre = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_CajaAperturas", x => x.Id);
                });

            migrationBuilder.AddForeignKey(
                name: "FK_CajaMovimientos_CajaAperturas_CajaAperturaId",
                table: "CajaMovimientos",
                column: "CajaAperturaId",
                principalTable: "CajaAperturas",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_CajaMovimientos_CajaAperturas_CajaAperturaId",
                table: "CajaMovimientos");

            migrationBuilder.DropTable(
                name: "CajaAperturas");

            migrationBuilder.RenameColumn(
                name: "CajaAperturaId",
                table: "CajaMovimientos",
                newName: "CajaId");

            migrationBuilder.RenameIndex(
                name: "IX_CajaMovimientos_CajaAperturaId_FechaUtc",
                table: "CajaMovimientos",
                newName: "IX_CajaMovimientos_CajaId_FechaUtc");

            migrationBuilder.CreateTable(
                name: "Cajas",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    Estado = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: false),
                    FechaAperturaUtc = table.Column<DateTime>(type: "datetime2", nullable: false),
                    FechaCierreUtc = table.Column<DateTime>(type: "datetime2", nullable: true),
                    ObservacionesApertura = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: true),
                    ObservacionesCierre = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: true),
                    SaldoApertura = table.Column<decimal>(type: "decimal(18,2)", precision: 18, scale: 2, nullable: false),
                    SaldoCierreContado = table.Column<decimal>(type: "decimal(18,2)", precision: 18, scale: 2, nullable: true),
                    UsuarioAperturaId = table.Column<int>(type: "int", nullable: true),
                    UsuarioCierreId = table.Column<int>(type: "int", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Cajas", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "CajaArqueos",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    CajaId = table.Column<int>(type: "int", nullable: false),
                    Diferencia = table.Column<decimal>(type: "decimal(18,2)", precision: 18, scale: 2, nullable: false),
                    EfectivoContado = table.Column<decimal>(type: "decimal(18,2)", precision: 18, scale: 2, nullable: false),
                    FechaUtc = table.Column<DateTime>(type: "datetime2", nullable: false),
                    Observaciones = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_CajaArqueos", x => x.Id);
                    table.ForeignKey(
                        name: "FK_CajaArqueos_Cajas_CajaId",
                        column: x => x.CajaId,
                        principalTable: "Cajas",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_CajaArqueos_CajaId",
                table: "CajaArqueos",
                column: "CajaId");

            migrationBuilder.CreateIndex(
                name: "IX_Cajas_Estado",
                table: "Cajas",
                column: "Estado");

            migrationBuilder.AddForeignKey(
                name: "FK_CajaMovimientos_Cajas_CajaId",
                table: "CajaMovimientos",
                column: "CajaId",
                principalTable: "Cajas",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);
        }
    }
}
