using System;
using Microsoft.EntityFrameworkCore;
using FacturaApp.Core.Models;
using System.IO;

namespace FacturaApp.Data
{
    public class BillingDbContext : DbContext
    {
		public DbSet<Usuario> Usuarios { get; set; } = null!;
		
        public DbSet<Empresa> Empresas { get; set; } = null!;
        public DbSet<Sucursal> Sucursales { get; set; } = null!;
		
		public DbSet<Producto> Productos { get; set; } = null!;
        public DbSet<Categoria> Categorias { get; set; } = null!;
        
        public DbSet<Cliente> Clientes { get; set; } = null!;
        public DbSet<Proveedor> Proveedores { get; set; } = null!;
		public DbSet<Inventario> Inventarios { get; set; } = null!;
		public DbSet<InventarioMovimiento> InventarioMovimientos { get; set; } = null!;
		
        public DbSet<Impuesto> Impuestos { get; set; } = null!;
        public DbSet<FormaPago> FormasPago { get; set; } = null!;
       
        public DbSet<Compra> Compras { get; set; } = null!;
        public DbSet<CompraDetalle> CompraDetalles { get; set; } = null!;
		
        public DbSet<Factura> Facturas { get; set; } = null!;
        public DbSet<FacturaDetalle> FacturaDetalles { get; set; } = null!;
        public DbSet<FacturaPago> FacturaPagos { get; set; } = null!;
	
        public DbSet<Proyecto> Proyectos { get; set; } = null!;
        public DbSet<ProyectoUsuario> ProyectoUsuarios { get; set; } = null!;
        public DbSet<TareaProyecto> Tareas { get; set; } = null!;
		public DbSet<TareaComentario> TareaComentarios { get; set; } = null!;

        private string DbPath { get; }

        public BillingDbContext()
        {
            var folder = Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData);
            var dir = Path.Combine(folder, "FacturaApp");
            if (!Directory.Exists(dir)) Directory.CreateDirectory(dir);
            DbPath = Path.Combine(dir, "facturaapp.db");
        }

        protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
        {
            optionsBuilder.UseSqlite($"Data Source={DbPath}");
        }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);
			
            modelBuilder.Entity<Producto>().Property(p => p.Precio).HasConversion<decimal>().HasPrecision(18,2);
            modelBuilder.Entity<FacturaDetalle>().Ignore(fd => fd.Importe);
			
			 // Configuración de relaciones
			 
        modelBuilder.Entity<Empresa>()
            .HasMany(e => e.Sucursales)
            .WithOne(s => s.Empresa)
            .HasForeignKey(s => s.EmpresaId)
            .OnDelete(DeleteBehavior.Cascade);

        modelBuilder.Entity<Sucursal>()
            .HasOne(s => s.Empresa)
            .WithMany(e => e.Sucursales)
            .HasForeignKey(s => s.EmpresaId);

        modelBuilder.Entity<Categoria>()
            .HasMany(c => c.Productos)
            .WithOne(p => p.Categoria)
            .HasForeignKey(p => p.CategoriaId)
            .OnDelete(DeleteBehavior.SetNull);

        modelBuilder.Entity<Producto>()
            .HasOne(p => p.Categoria)
            .WithMany(c => c.Productos)
            .HasForeignKey(p => p.CategoriaId);

        modelBuilder.Entity<Compra>()
            .HasMany(c => c.Detalles)
            .WithOne(d => d.Compra)
            .HasForeignKey(d => d.CompraId)
            .OnDelete(DeleteBehavior.Cascade);

        modelBuilder.Entity<CompraDetalle>()
            .HasOne(cd => cd.Compra)
            .WithMany(c => c.Detalles)
            .HasForeignKey(cd => cd.CompraId);
			
			modelBuilder.Entity<FacturaDetalle>()
    .HasOne(fd => fd.Producto)
    .WithMany()
    .HasForeignKey(fd => fd.ProductoId)
    .OnDelete(DeleteBehavior.Restrict);

modelBuilder.Entity<FacturaDetalle>().Navigation(fd => fd.Producto).AutoInclude();

            modelBuilder.Entity<Proyecto>()
                .HasMany(p => p.Usuarios)
                .WithOne(pu => pu.Proyecto)
                .HasForeignKey(pu => pu.ProyectoId)
                .OnDelete(DeleteBehavior.Cascade);

            modelBuilder.Entity<Usuario>()
                .HasMany<ProyectoUsuario>()
                .WithOne(pu => pu.Usuario)
                .HasForeignKey(pu => pu.UsuarioId)
                .OnDelete(DeleteBehavior.Cascade);

            modelBuilder.Entity<TareaProyecto>()
                .HasOne(t => t.Proyecto)
                .WithMany(p => p.Tareas)
                .HasForeignKey(t => t.ProyectoId)
                .OnDelete(DeleteBehavior.Cascade);

            modelBuilder.Entity<TareaProyecto>()
                .HasOne(t => t.Usuario)
                .WithMany()
                .HasForeignKey(t => t.UsuarioId)
                .OnDelete(DeleteBehavior.SetNull);

            modelBuilder.Entity<TareaComentario>()
                .HasOne(tc => tc.Tarea)
                .WithMany(t => t.Comentarios)
                .HasForeignKey(tc => tc.TareaId)
                .OnDelete(DeleteBehavior.Cascade);



            // CARGAR LISTAS

            modelBuilder.Entity<Factura>()
        .Navigation(f => f.Detalles)
        .AutoInclude(false); // Controlar manualmente los Includes

    modelBuilder.Entity<Factura>()
        .Navigation(f => f.Pagos)
        .AutoInclude(false);
			
        }

        public void EnsureDatabaseCreatedAndSeed()
        {
            if (Database.EnsureCreated())
            {
                // Seed sample data
                var empresa = new Empresa { Nombre = "Tienda Demo", Rnc = "131313131", Direccion = "Calle Principal 123" };
                Empresas.Add(empresa);

                var suc = new Sucursal { Nombre = "Sucursal Principal", Direccion = "C. Principal", Empresa = empresa };
                Sucursales.Add(suc);

                var cat = new Categoria { Nombre = "General", Descripcion = "Productos generales" };
                Categorias.Add(cat);

                var prod1 = new Producto { Nombre = "Camiseta", Codigo = "CAM001", Precio = 15.50m, Stock = 100, Categoria = cat };
                var prod2 = new Producto { Nombre = "Pantalon", Codigo = "PAN001", Precio = 30.00m, Stock = 50, Categoria = cat };
                Productos.AddRange(prod1, prod2);

                Clientes.Add(new FacturaApp.Core.Models.Cliente { Nombre = "Consumidor Final" });

                FormasPago.Add(new FormaPago { Nombre = "Efectivo" });
                FormasPago.Add(new FormaPago { Nombre = "Tarjeta" });
				
				Usuario admin = new()
				{
				Nombre = "Admin",
				Username = "admin",
				Rol = UserRole.Admin,
				Password = PasswordHelper.HashPassword("admin123"),
				Email = "admin@demo.local"
				};

                Usuarios.Add(admin);

                SaveChanges();
            }
        }
    }
}