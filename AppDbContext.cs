using LaOriginalBackend.Models;
using Microsoft.EntityFrameworkCore;

namespace LaOriginalBackend.Data
{
    public class AppDbContext : DbContext
    {
        public AppDbContext(DbContextOptions<AppDbContext> options) : base(options) { }

        // Core
        public DbSet<Usuario> Usuarios => Set<Usuario>();
        public DbSet<Rol> Roles => Set<Rol>();

        // Mantenimientos 
        public DbSet<Estado> Estados => Set<Estado>();
        public DbSet<Categoria> Categorias => Set<Categoria>();
        public DbSet<Marca> Marcas => Set<Marca>();
        public DbSet<Cliente> Clientes => Set<Cliente>();
        public DbSet<Proveedor> Proveedores => Set<Proveedor>();
        public DbSet<UnidadMedida> Unidades => Set<UnidadMedida>();
        public DbSet<Color> Colores => Set<Color>();
        public DbSet<FormaPago> FormasPago => Set<FormaPago>();
        public DbSet<Producto> Productos => Set<Producto>();
        public DbSet<Presentacion> Presentaciones => Set<Presentacion>();
        public DbSet<ProductoStock> ProductoStocks => Set<ProductoStock>();
        public DbSet<MovimientoInventario> MovimientosInventario => Set<MovimientoInventario>();
        public DbSet<Compra> Compras => Set<Compra>();
        public DbSet<CompraDetalle> ComprasDetalle => Set<CompraDetalle>();
        public DbSet<ProveedorPresentacion> ProveedoresPresentaciones => Set<ProveedorPresentacion>();
        public DbSet<Venta> Ventas => Set<Venta>();
        public DbSet<VentaDetalle> VentasDetalle => Set<VentaDetalle>();
        public DbSet<DevolucionVenta> DevolucionesVenta => Set<DevolucionVenta>();
        public DbSet<DevolucionVentaDetalle> DevolucionesVentaDetalle => Set<DevolucionVentaDetalle>();
        public DbSet<DevolucionCompra> DevolucionesCompra => Set<DevolucionCompra>();
        public DbSet<DevolucionCompraDetalle> DevolucionesCompraDetalle => Set<DevolucionCompraDetalle>();
        public DbSet<CajaApertura> CajaAperturas { get; set; } = null!;
        public DbSet<CajaMovimiento> CajaMovimientos { get; set; } = null!;

        public DbSet<PedidoProveedor> PedidosProveedores => Set<PedidoProveedor>();
        public DbSet<PedidoProveedorDetalle> PedidosProveedoresDetalle => Set<PedidoProveedorDetalle>();

        // Pedidos cliente
        public DbSet<PedidoCliente> PedidosClientes { get; set; } = null!;
        public DbSet<PedidoClienteDetalle> PedidosClientesDetalles { get; set; } = null!;
        public DbSet<PedidoClienteReserva> PedidosClientesReservas { get; set; } = null!;
        public DbSet<PedidoClientePago> PedidosClientesPagos { get; set; } = null!;

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);

            // ==== Catálogos básicos ====
            modelBuilder.Entity<Rol>().HasIndex(x => x.Nombre).IsUnique();
            modelBuilder.Entity<Categoria>().HasIndex(x => x.Nombre).IsUnique();
            modelBuilder.Entity<Marca>().HasIndex(x => x.Nombre).IsUnique();
            modelBuilder.Entity<UnidadMedida>().HasIndex(x => x.Simbolo).IsUnique();
            modelBuilder.Entity<FormaPago>().HasIndex(x => x.Nombre).IsUnique();
            modelBuilder.Entity<Color>().HasIndex(x => x.Nombre).IsUnique();

            modelBuilder.Entity<Categoria>(e => { e.Property(c => c.Prefijo).HasMaxLength(2); });

            // ==== Clientes / Proveedores ====
            modelBuilder.Entity<Cliente>().HasIndex(x => x.NIT).IsUnique().HasFilter("[NIT] IS NOT NULL");
            modelBuilder.Entity<Proveedor>().HasIndex(x => x.NIT).IsUnique().HasFilter("[NIT] IS NOT NULL");
            modelBuilder.Entity<Proveedor>().HasIndex(x => x.Nombre).IsUnique();

            // ==== Productos ====
            modelBuilder.Entity<Producto>()
                .HasIndex(x => new { x.CategoriaId, x.Codigo })
                .IsUnique()
                .HasFilter("[Codigo] IS NOT NULL");

            // ==== Presentaciones ====
            modelBuilder.Entity<Presentacion>(e =>
            {
                e.Property(p => p.Factor).HasPrecision(12, 4);
                e.Property(p => p.PrecioCompraDefault).HasPrecision(18, 2);
                e.Property(p => p.PrecioVentaDefault).HasPrecision(18, 2);
                e.Property(p => p.StockMinimo).HasPrecision(18, 2);

                e.HasIndex(p => new { p.ProductoId, p.Nombre }).IsUnique();
                e.HasIndex(p => new { p.ProductoId, p.EsPrincipal }).IsUnique().HasFilter("[EsPrincipal] = 1");
            });

            modelBuilder.Entity<ProductoStock>().HasIndex(x => x.PresentacionId).IsUnique();

            // ==== Ventas ====
            modelBuilder.Entity<Venta>().HasIndex(x => x.Serie);
            modelBuilder.Entity<Venta>().HasIndex(x => x.Numero);
            modelBuilder.Entity<Venta>(e =>
            {
                e.Property(v => v.Subtotal).HasPrecision(18, 2);
                e.Property(v => v.Descuento).HasPrecision(18, 2);
                e.Property(v => v.Total).HasPrecision(18, 2);
            });
            modelBuilder.Entity<VentaDetalle>(e =>
            {
                e.Property(d => d.Cantidad).HasPrecision(18, 2);
                e.Property(d => d.PrecioUnitario).HasPrecision(18, 2);
                e.Property(d => d.DescuentoUnitario).HasPrecision(18, 2);
                e.Property(d => d.TotalLinea).HasPrecision(18, 2);
            });

            // ==== Compras ====
            modelBuilder.Entity<CompraDetalle>(e =>
            {
                e.Property(d => d.Cantidad).HasPrecision(18, 2);
                e.Property(d => d.CostoUnitario).HasPrecision(18, 2);
                e.Property(d => d.TotalLinea).HasPrecision(18, 2);
            });

            // ==== Caja ====
            modelBuilder.Entity<CajaApertura>(e =>
            {
                e.Property(x => x.MontoInicial).HasPrecision(18, 2);
                e.Property(x => x.MontoCierreDeclarado).HasPrecision(18, 2);
                e.Property(x => x.ObservacionesApertura).HasMaxLength(200);
                e.Property(x => x.ObservacionesCierre).HasMaxLength(200);

                e.HasMany(x => x.Movimientos)
                 .WithOne(m => m.CajaApertura)
                 .HasForeignKey(m => m.CajaAperturaId)
                 .OnDelete(DeleteBehavior.Cascade);
            });

            modelBuilder.Entity<CajaMovimiento>(e =>
            {
                e.Property(x => x.Monto).HasPrecision(18, 2);
                e.Property(x => x.Concepto).HasMaxLength(120);
                e.Property(x => x.Observaciones).HasMaxLength(200);
                e.Property(x => x.Documento).HasMaxLength(40);

                e.HasIndex(x => new { x.CajaAperturaId, x.FechaUtc });
                e.HasIndex(x => x.Tipo);
            });

            // ==== FKs a Usuarios (restricción) ====
            modelBuilder.Entity<Venta>().HasOne<Usuario>().WithMany().HasForeignKey(v => v.UsuarioId).OnDelete(DeleteBehavior.Restrict);
            modelBuilder.Entity<Compra>().HasOne<Usuario>().WithMany().HasForeignKey(c => c.UsuarioId).OnDelete(DeleteBehavior.Restrict);
            modelBuilder.Entity<DevolucionVenta>().HasOne<Usuario>().WithMany().HasForeignKey(d => d.UsuarioId).OnDelete(DeleteBehavior.Restrict);
            modelBuilder.Entity<DevolucionCompra>().HasOne<Usuario>().WithMany().HasForeignKey(d => d.UsuarioId).OnDelete(DeleteBehavior.Restrict);
            modelBuilder.Entity<MovimientoInventario>().HasOne<Usuario>().WithMany().HasForeignKey(m => m.UsuarioId).OnDelete(DeleteBehavior.Restrict);
            modelBuilder.Entity<CajaMovimiento>().HasOne<Usuario>().WithMany().HasForeignKey(m => m.UsuarioId).OnDelete(DeleteBehavior.Restrict);

            modelBuilder.ConfigureCaja();

            // ==== Pedidos / Compras (proveedor) ====
            modelBuilder.Entity<PedidoProveedorDetalle>()
                .HasOne(d => d.Presentacion).WithMany()
                .HasForeignKey(d => d.PresentacionId).OnDelete(DeleteBehavior.Restrict);

            modelBuilder.Entity<CompraDetalle>()
                .HasOne(cd => cd.Presentacion).WithMany()
                .HasForeignKey(cd => cd.PresentacionId).OnDelete(DeleteBehavior.Restrict);

            modelBuilder.Entity<PedidoProveedorDetalle>()
                .HasOne(d => d.Pedido).WithMany(p => p.Detalles)
                .HasForeignKey(d => d.PedidoProveedorId).OnDelete(DeleteBehavior.Cascade);

            modelBuilder.Entity<CompraDetalle>()
                .HasOne(cd => cd.PedidoDetalle).WithMany()
                .HasForeignKey(cd => cd.PedidoProveedorDetalleId).OnDelete(DeleteBehavior.Restrict);

            modelBuilder.Entity<PedidoProveedor>().HasIndex(p => p.Numero).IsUnique(true).HasFilter("[Numero] IS NOT NULL");

            modelBuilder.Entity<ProveedorPresentacion>(e =>
            {
                e.Property(x => x.PrecioLista).HasPrecision(18, 2);
                e.Property(x => x.PrecioUltimo).HasPrecision(18, 2);

                e.HasOne(pp => pp.Proveedor).WithMany().HasForeignKey(pp => pp.ProveedorId).OnDelete(DeleteBehavior.Restrict);
                e.HasOne(pp => pp.Presentacion).WithMany().HasForeignKey(pp => pp.PresentacionId).OnDelete(DeleteBehavior.Restrict);

                e.HasIndex(x => new { x.ProveedorId, x.PresentacionId }).IsUnique();
            });

            // ==== Pedidos de Clientes ====
            modelBuilder.Entity<PedidoCliente>(e =>
            {
                e.ToTable("PedidoCliente");
                e.HasKey(x => x.Id);

                e.Property(x => x.Subtotal).HasPrecision(18, 2);
                e.Property(x => x.Descuento).HasPrecision(18, 2);
                e.Property(x => x.Total).HasPrecision(18, 2);

                e.HasMany(x => x.Detalles).WithOne(d => d.Pedido!).HasForeignKey(d => d.PedidoClienteId).OnDelete(DeleteBehavior.Cascade);
                e.HasMany(x => x.Reservas).WithOne(r => r.Pedido!).HasForeignKey(r => r.PedidoClienteId).OnDelete(DeleteBehavior.Cascade);
                e.HasMany<PedidoClientePago>().WithOne(p => p.Pedido!).HasForeignKey(p => p.PedidoClienteId).OnDelete(DeleteBehavior.Cascade);

                e.HasOne<Venta>().WithMany().HasForeignKey(p => p.VentaId).OnDelete(DeleteBehavior.Restrict);
            });

            modelBuilder.Entity<PedidoClienteDetalle>(d =>
            {
                d.ToTable("PedidoClienteDetalle");
                d.HasKey(x => x.Id);
                d.Property(x => x.Cantidad).HasPrecision(18, 2);
                d.Property(x => x.PrecioUnitario).HasPrecision(18, 2);
                d.Property(x => x.DescuentoUnitario).HasPrecision(18, 2);
                d.Property(x => x.TotalLinea).HasPrecision(18, 2);
            });

            modelBuilder.Entity<PedidoClienteReserva>(r =>
            {
                r.ToTable("PedidoClienteReserva");
                r.HasKey(x => x.Id);
                r.Property(x => x.Cantidad).HasPrecision(18, 2);
            });

            modelBuilder.Entity<PedidoClientePago>(p =>
            {
                p.ToTable("PedidoClientePago");
                p.HasKey(x => x.Id);
                p.Property(x => x.Monto).HasPrecision(18, 2);
                p.Property(x => x.FormaPagoNombre).HasMaxLength(80);
                p.Property(x => x.Referencia).HasMaxLength(60);
                p.Property(x => x.Notas).HasMaxLength(200);

                p.HasIndex(x => new { x.PedidoClienteId, x.FechaUtc });
                p.HasIndex(x => x.FormaPagoId);
            });

            modelBuilder.Entity<PedidoClienteDetalle>().HasOne<Presentacion>().WithMany()
                .HasForeignKey(d => d.PresentacionId).OnDelete(DeleteBehavior.Restrict);
            modelBuilder.Entity<PedidoClienteReserva>().HasOne<Presentacion>().WithMany()
                .HasForeignKey(r => r.PresentacionId).OnDelete(DeleteBehavior.Restrict);

            // 👇 Owned: columnas en la tabla de PedidoCliente (prefijo Diseno_)
            modelBuilder.Entity<PedidoCliente>().OwnsOne(p => p.Diseno, d =>
            {
                d.Property(x => x.Color).HasMaxLength(60).HasColumnName("Diseno_Color");
                d.Property(x => x.Otros).HasMaxLength(200).HasColumnName("Diseno_Otros");
                d.Property(x => x.Lienzos).HasColumnName("Diseno_Lienzos");
                d.Property(x => x.Brich).HasColumnName("Diseno_Brich");
                d.Property(x => x.Reportado).HasColumnName("Diseno_Reportado");
                d.Property(x => x.Extra).HasColumnName("Diseno_Extra");
            });

            modelBuilder.Entity<VentaDetalle>(e =>
            {
                e.Property(d => d.Cantidad).HasPrecision(18, 2);
                e.Property(d => d.PrecioUnitario).HasPrecision(18, 2);
                e.Property(d => d.DescuentoUnitario).HasPrecision(18, 2);
                e.Property(d => d.TotalLinea).HasPrecision(18, 2);
                e.Property(d => d.CostoUnitario).HasPrecision(18, 2); // << NUEVO
            });

        }
    }
}
