using System.Linq;
using System.Threading.Tasks;
using LaOriginalBackend.Models;
using Microsoft.EntityFrameworkCore;

namespace LaOriginalBackend.Data
{
    public static class DbSeeder
    {
        public static async Task SeedAsync(AppDbContext db)
        {
            // Asegura que las migraciones se apliquen antes de sembrar
            await db.Database.MigrateAsync();

            // ==== Roles ====
            if (!await db.Roles.AnyAsync())
            {
                db.Roles.AddRange(
                    new Rol { Nombre = "Administrador", Activo = true },
                    new Rol { Nombre = "Vendedor", Activo = true },
                    new Rol { Nombre = "Compras", Activo = true },
                    new Rol { Nombre = "Caja", Activo = true }
                );
            }

            // ==== Estados por tipo ====
            if (!await db.Estados.AnyAsync())
            {
                db.Estados.AddRange(
                    // Caja
                    new Estado { Tipo = "Caja", Nombre = "Abierta", Activo = true },
                    new Estado { Tipo = "Caja", Nombre = "Cerrada", Activo = true },

                    // Venta
                    new Estado { Tipo = "Venta", Nombre = "Emitida", Activo = true },
                    new Estado { Tipo = "Venta", Nombre = "Anulada", Activo = true },

                    // Pedido
                    new Estado { Tipo = "Pedido", Nombre = "Borrador", Activo = true },
                    new Estado { Tipo = "Pedido", Nombre = "En produccion", Activo = true },
                    new Estado { Tipo = "Pedido", Nombre = "Finalizado", Activo = true },
                    new Estado { Tipo = "Pedido", Nombre = "Cancelado", Activo = true }
                );
            }

            // ==== Unidad de medida por defecto ====
            if (!await db.Unidades.AnyAsync(u =>
                    u.Activo &&
                    (u.Nombre == "Unidad" || u.Simbolo == "U")))
            {
                db.Unidades.Add(new UnidadMedida
                {
                    Nombre = "Unidad",
                    Simbolo = "U",
                    Activo = true
                });
            }

            // ==== Categorías base ====
            if (!await db.Categorias.AnyAsync())
            {
                db.Categorias.AddRange(
                    new Categoria
                    {
                        Nombre = "Hilos",
                        Descripcion = "Hilos para tejidos",
                        Prefijo = "H",   // prefijo explícito
                        Activo = true
                    },
                    new Categoria
                    {
                        Nombre = "Trajes Típicos",
                        Descripcion = "Indumentaria de Chichicastenango",
                        Prefijo = "T",   // prefijo explícito (usa el que prefieras)
                        Activo = true
                    }
                );
            }

            await db.SaveChangesAsync();

            // ==== Completar Prefijo en categorías faltantes ====
            var categoriasSinPrefijo = await db.Categorias
                .Where(c => c.Activo && (c.Prefijo == null || c.Prefijo == ""))
                .ToListAsync();

            foreach (var c in categoriasSinPrefijo)
            {
                var primera = string.IsNullOrWhiteSpace(c.Nombre)
                    ? "X"
                    : c.Nombre.Trim()[0].ToString().ToUpper();

                c.Prefijo = primera; // p. ej. "Telas" -> "T"
            }

            if (categoriasSinPrefijo.Count > 0)
                await db.SaveChangesAsync();
        }
    }
}
