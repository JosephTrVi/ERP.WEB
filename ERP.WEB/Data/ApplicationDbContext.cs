using ERP.WEB.Models.Inventario;
using ERP.WEB.Models.Logistica;
using ERP.WEB.Models.Maestros;
using ERP.WEB.Models.Personal;
using ERP.WEB.Models.Produccion;
using ERP.WEB.Models.Seguridad;
using ERP.WEB.Models.Ventas;
using Microsoft.EntityFrameworkCore;

namespace ERP.WEB.Data
{
    public class ApplicationDbContext : DbContext
    {
        public ApplicationDbContext(DbContextOptions<ApplicationDbContext> options)
            : base(options)
        {
        }
        // Seguridad
        public DbSet<Usuario> Usuarios { get; set; } = null!;
        public DbSet<Rol> Roles { get; set; } = null!;
        public DbSet<Permiso> Permisos { get; set; } = null!;
        public DbSet<Empleado> Empleados { get; set; } = null!;

        // Inventario
        public DbSet<Categoria> Categorias { get; set; } = null!;
        public DbSet<SubCategoria> SubCategorias { get; set; } = null!;
        public DbSet<Producto> Productos { get; set; } = null!;
        public DbSet<Atributo> Atributos { get; set; } = null!;
        public DbSet<ProductoAtributo> ProductoAtributos { get; set; } = null!;

        // Logistica
        public DbSet<RequerimientoCabecera> RequerimientosCabecera { get; set; } = null!;
        public DbSet<RequerimientoDetalle> RequerimientosDetalle { get; set; } = null!;
        public DbSet<OrdenesCompraCabecera> OrdenesCompraCabecera { get; set; } = null!;
        public DbSet<OrdenesCompraDetalle> OrdenesCompraDetalle { get; set; } = null!;
        public DbSet<Proveedor> Proveedores { get; set; } = null!;

        // Almacen
        public DbSet<Almacen> Almacenes { get; set; } = null!;
        public DbSet<IngresosAlmacenCabecera> IngresosAlmacenCabecera { get; set; } = null!;
        public DbSet<IngresosAlmacenDetalle> IngresosAlmacenDetalle { get; set; } = null!;

        // Ventas
        public DbSet<ListasPreciosCabecera> ListasPreciosCabecera { get; set; } = null!;
        public DbSet<ListasPreciosDetalle> ListasPreciosDetalle { get; set; } = null!;
        public DbSet<CotizacionesCabecera> CotizacionesCabecera { get; set; } = null!;
        public DbSet<CotizacionesDetalle> CotizacionesDetalle { get; set; } = null!;
        public DbSet<TipoCambio> TipoCambio { get; set; } = null!;
        public DbSet<Cliente> Clientes { get; set; } = null!;

        // Produccion
       
        public DbSet<Receta> Recetas { get; set; }
        public DbSet<RecetaDetalle> RecetaDetalles { get; set; }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);

            // Mapeo explícito de esquemas y tablas
            modelBuilder.Entity<Usuario>(entity =>
            {
                entity.ToTable("Usuarios", "Seguridad");
                entity.HasKey(e => e.UsuarioID);

                // Relación 1 a 1 / N a 1 con Empleado (Personal.Personal)
                entity.HasOne(d => d.Empleado)
                      .WithMany()
                      .HasForeignKey(d => d.PersonalID)
                      .OnDelete(DeleteBehavior.Restrict);

                // Relación con Rol
                entity.HasOne(d => d.Rol)
                      .WithMany()
                      .HasForeignKey(d => d.RolID)
                      .OnDelete(DeleteBehavior.Restrict);
            });

            modelBuilder.Entity<Rol>(entity =>
            {
                entity.ToTable("Roles", "Seguridad");
                entity.HasKey(e => e.RolID);
            });

            modelBuilder.Entity<Permiso>(entity =>
            {
                entity.ToTable("Permisos", "Seguridad");
                entity.HasKey(e => e.PermisoID);
            });

            modelBuilder.Entity<Empleado>(entity =>
            {
                entity.ToTable("Personal", "Personal");
                entity.HasKey(e => e.PersonalID);
            });

            // Inventario
            modelBuilder.Entity<Categoria>(entity =>
            {
                entity.ToTable("Categorias", "Inventario");
                entity.HasKey(e => e.CategoriaID);
            });

            modelBuilder.Entity<SubCategoria>(entity =>
            {
                entity.ToTable("SubCategorias", "Inventario");
                entity.HasKey(e => e.SubCategoriaID);
            });

            modelBuilder.Entity<Producto>(entity =>
            {
                entity.ToTable("Productos", "Inventario");
                entity.HasKey(e => e.ProductoID);

                entity.HasOne(d => d.Categoria)
                      .WithMany()
                      .HasForeignKey(d => d.CategoriaID)
                      .OnDelete(DeleteBehavior.Restrict);

                entity.HasOne(d => d.SubCategoria)
                      .WithMany()
                      .HasForeignKey(d => d.SubcategoriaID)
                      .OnDelete(DeleteBehavior.Restrict);
            });

            modelBuilder.Entity<Atributo>(entity =>
            {
                entity.ToTable("Atributos", "Inventario");
                entity.HasKey(e => e.AtributoID);
            });

            modelBuilder.Entity<ProductoAtributo>(entity =>
            {
                entity.ToTable("ProductoAtributos", "Inventario");
                entity.HasKey(e => e.ProductoAtributoID);
            });

            // Logistica
            modelBuilder.Entity<RequerimientoCabecera>(entity =>
            {
                entity.ToTable("RequerimientosCabecera", "Compras");
                entity.HasKey(e => e.RequerimientoID);
            });

            modelBuilder.Entity<RequerimientoDetalle>(entity =>
            {
                entity.ToTable("RequerimientosDetalle", "Compras");
                entity.HasKey(e => e.RequerimientoDetalleID);

                // Especificar explícitamente <RequerimientosCabecera> sin el signo de interrogación (?)
                entity.HasOne<RequerimientoCabecera>(d => d.Cabecera!)
                      .WithMany(p => p.Detalles)
                      .HasForeignKey(d => d.RequerimientoID)
                      .OnDelete(DeleteBehavior.Cascade);

                entity.HasOne<Producto>(d => d.Producto!)
                      .WithMany()
                      .HasForeignKey(d => d.ProductoID)
                      .OnDelete(DeleteBehavior.Restrict);
            });

            modelBuilder.Entity<OrdenesCompraCabecera>(entity =>
            {
                entity.ToTable("OrdenesCompraCabecera", "Compras");
                entity.HasKey(e => e.OrdenCompraID);

                entity.HasOne(o => o.Requerimiento)
                      .WithMany()
                      .HasForeignKey(o => o.RequerimientoID)
                      .OnDelete(DeleteBehavior.Restrict);
            });

            modelBuilder.Entity<OrdenesCompraDetalle>(entity =>
            {
                entity.ToTable("OrdenesCompraDetalle", "Compras");
                entity.HasKey(e => e.OrdenCompraDetalleID);

                entity.HasOne<OrdenesCompraCabecera>(d => d.OrdenCompra!)
                      .WithMany(p => p.Detalles)
                      .HasForeignKey(d => d.OrdenCompraID)
                      .OnDelete(DeleteBehavior.Cascade);

                entity.HasOne<Producto>(d => d.Producto!)
                      .WithMany()
                      .HasForeignKey(d => d.ProductoID)
                      .OnDelete(DeleteBehavior.Restrict);
            });

            modelBuilder.Entity<Proveedor>(entity =>
            {
                entity.ToTable("Proveedores", "Compras");
                entity.HasKey(e => e.ProveedorID);
            });

            // CONFIGURACIÓN DE TABLAS DE INVENTARIO
            modelBuilder.Entity<Almacen>(entity =>
            {
                entity.ToTable("Almacenes", "Inventario");
                entity.HasKey(e => e.AlmacenID);
            });

            modelBuilder.Entity<IngresosAlmacenCabecera>(entity =>
            {
                entity.ToTable("IngresosAlmacenCabecera", "Inventario");
                entity.HasKey(e => e.IngresoID);

                entity.HasOne(i => i.OrdenCompra)
                      .WithMany()
                      .HasForeignKey(i => i.OrdenCompraID)
                      .OnDelete(DeleteBehavior.Restrict);

                entity.HasOne(i => i.Almacen)
                      .WithMany()
                      .HasForeignKey(i => i.AlmacenID)
                      .OnDelete(DeleteBehavior.Restrict);
            });

            modelBuilder.Entity<IngresosAlmacenDetalle>(entity =>
            {
                entity.ToTable("IngresosAlmacenDetalle", "Inventario");
                entity.HasKey(e => e.IngresoDetalleID);

                entity.HasOne(d => d.IngresoCabecera)
                      .WithMany(c => c.Detalles)
                      .HasForeignKey(d => d.IngresoID)
                      .OnDelete(DeleteBehavior.Cascade);
            });

            // Ventas
            modelBuilder.Entity<ListasPreciosCabecera>(entity => {
                entity.ToTable("ListasPreciosCabecera", "Ventas");
                entity.HasKey(e => e.ListaPrecioID);
            });

            modelBuilder.Entity<ListasPreciosDetalle>(entity => {
                entity.ToTable("ListasPreciosDetalle", "Ventas");
                entity.HasKey(e => e.ListaPrecioDetalleID);
            });

            modelBuilder.Entity<CotizacionesCabecera>(entity => {
                entity.ToTable("CotizacionesCabecera", "Ventas");
                entity.HasKey(e => e.CotizacionID);
            });

            modelBuilder.Entity<CotizacionesDetalle>(entity => {
                entity.ToTable("CotizacionesDetalle", "Ventas");
                entity.HasKey(e => e.CotizacionDetalleID);
            });

            modelBuilder.Entity<Cliente>(entity =>
            {
                entity.ToTable("Clientes", "Ventas");
                entity.HasKey(e => e.ClienteID);
            });

            //Produccion
            // Mapeo explícito de tablas y esquemas (opcional si ya usaste [Table] en las entidades)
            modelBuilder.Entity<Receta>()
                .ToTable("Recetas", schema: "Produccion");

            modelBuilder.Entity<RecetaDetalle>()
                .ToTable("RecetaDetalles", schema: "Produccion");

            // Configuración de precisión decimal para la cantidad requerida
            modelBuilder.Entity<RecetaDetalle>()
                .Property(r => r.CantidadRequerida)
                .HasPrecision(18, 4);


            // Mapeo explicito para Categorias
            modelBuilder.Entity<Categoria>(entity =>
            {
                entity.Property(c => c.EsReceta).HasDefaultValue(true);
                entity.Property(c => c.EsComponente).HasDefaultValue(true);
            });

            // Mapeo explicito para SubCategorias
            modelBuilder.Entity<SubCategoria>(entity =>
            {
                entity.Property(s => s.EsReceta).HasDefaultValue(true);
                entity.Property(s => s.EsComponente).HasDefaultValue(true);
            });
        }
    }
}