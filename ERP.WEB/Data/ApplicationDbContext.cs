using ERP.WEB.Models.Inventario;
using ERP.WEB.Models.Logistica;
using ERP.WEB.Models.Personal;
using ERP.WEB.Models.Seguridad;
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

        // Logistica
        public DbSet<RequerimientoCabecera> RequerimientosCabecera { get; set; } = null!;
        public DbSet<RequerimientoDetalle> RequerimientosDetalle { get; set; } = null!;


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
        }
    }
}