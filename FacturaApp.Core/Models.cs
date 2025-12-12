using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

/// <summary> Aqui se definen los modelos de datos para la aplicacion </summary>

namespace FacturaApp.Core.Models
{
    public abstract class BaseEntity
    {
        [Key]
        public Guid Id { get; set; } = Guid.NewGuid();
		
		[ConcurrencyCheck]
		    [Timestamp]
    public int? RowVersion { get; set; }
		
    }

    /// <summary>
    /// Empresa (empresa emisora).
    /// </summary>
    public class Empresa : BaseEntity
    {
        [Required]
        [StringLength(200)]
        public string Nombre { get; set; } = string.Empty;

        public string? Rnc { get; set; }
        public string? Direccion { get; set; }

        public ICollection<Sucursal>? Sucursales { get; set; }
		
		// ctor
		
		public Empresa()
		{
		  Sucursales = new HashSet<Sucursal>();
		}
		
		
    }

    public class Sucursal : BaseEntity
    {
        [Required]
        [StringLength(150)]
        public string Nombre { get; set; } = string.Empty;

        public string? Direccion { get; set; }

        [Required]
        public Guid EmpresaId { get; set; }
        public Empresa? Empresa { get; set; }
		
		// ctor
		
		public Sucursal()
		{
		}
		
    }
	
	// Roles de usuario
	
	public enum UserRole
{
    Admin = 1,
    Vendedor = 2,
    Inventario = 3,
    Contable = 4,
    Consulta = 5
}

    public class Usuario : BaseEntity
    {
        [Required]
        [StringLength(120)]
        public string Nombre { get; set; } = string.Empty;

        [Required]
        [StringLength(120)]
        public string Username { get; set; } = string.Empty;

        [StringLength(200)]
        public string? Email { get; set; }
		
		    [Required]
    public string Password{ get; set; } = string.Empty;

    [Required]
    public UserRole Rol { get; set; }
		
		// ctor
		
		public Usuario()
		{
		}
		
    }

    public class Categoria : BaseEntity
    {
        [Required]
        public string Nombre { get; set; } = string.Empty;
        public string? Descripcion { get; set; }
        public ICollection<Producto>? Productos { get; set; }
		
		// ctor
		
		public Categoria()
		{
		Productos = new HashSet<Producto>();
		}
		
    }

    public class Producto : BaseEntity
    {
        [Required]
        public string Nombre { get; set; } = string.Empty;

        public string? Codigo { get; set; }

        [Required]
        [Column(TypeName = "decimal(18,2)")]
        public decimal Precio { get; set; }

        public int Stock { get; set; }

        public Guid? CategoriaId { get; set; }
        public Categoria? Categoria { get; set; }
		
		// ctor
		
		public Producto()
		{
		}
		
    }

    public class Cliente : BaseEntity
    {
        [Required]
        public string Nombre { get; set; } = string.Empty;

        public string? Telefono { get; set; }
        public string? Direccion { get; set; }
		
		// ctor
		
		public Cliente()
		{
		}
		
    }

    public class Proveedor : BaseEntity
    {
        [Required]
        public string Nombre { get; set; } = string.Empty;
        public string? Contacto { get; set; }
		
	     // ctor
		
		public Proveedor()
		{
		}
		
    }

    public class Impuesto : BaseEntity
    {
        [Required]
        public string Nombre { get; set; } = string.Empty;

        [Range(0,100)]
        public decimal Porcentaje { get; set; }
		
	    // ctor
		
		public Impuesto()
		{
		}
		
    }

    public class FormaPago : BaseEntity
    {
        [Required]
        public string Nombre { get; set; } = string.Empty;
		
	    // ctor
		
		public FormaPago()
		{
		}
		
    }

    public class InventarioMovimiento : BaseEntity
    {
        public Guid ProductoId { get; set; }
        public Producto? Producto { get; set; }
        public int Cantidad { get; set; }
        public string? Nota { get; set; }
        public DateTime Fecha { get; set; } = DateTime.Now;
		
	    // ctor
		
		public InventarioMovimiento()
		{
		}
		
    }

    public class Compra : BaseEntity
    {
        public Guid ProveedorId { get; set; }
        public Proveedor? Proveedor { get; set; }
        public DateTime Fecha { get; set; } = DateTime.Now;
        public ICollection<CompraDetalle>? Detalles { get; set; }
		
	    // ctor
		
		public Compra()
		{
			Detalles = new HashSet<CompraDetalle>();
		}
		
    }

    public class CompraDetalle : BaseEntity
    {
        public Guid CompraId { get; set; }
        public Compra? Compra { get; set; }
        public Guid ProductoId { get; set; }
        public Producto? Producto { get; set; }
        public int Cantidad { get; set; }
        public decimal Precio{ get; set; }
		
		// ctor
		
		public CompraDetalle()
		{
		}
		
    }

    public class Factura : BaseEntity
    {
        public Guid ClienteId { get; set; }
        public Cliente? Cliente { get; set; }
        public DateTime Fecha { get; set; } = DateTime.Now;
        public decimal Subtotal { get; set; }
        public decimal Total { get; set; }
        public ICollection<FacturaDetalle>? Detalles { get; set; }
        public ICollection<FacturaPago>? Pagos { get; set; }
		
		// ctor
		
		public Factura()
		{
			Detalles = new HashSet<FacturaDetalle>();
			Pagos = new HashSet<FacturaPago>();
			
		}
		
    }

    public class FacturaDetalle : BaseEntity
    {
        public Guid FacturaId { get; set; }
        public Factura? Factura { get; set; }
        public Guid ProductoId { get; set; }
        public Producto? Producto { get; set; }
        public int Cantidad { get; set; }
		
		// Precio historico
        public decimal Precio { get; set; }
		
        public decimal Importe => Cantidad * Precio;
		
		// ctor
		
		public FacturaDetalle()
		{
		}
		
    }

    public class FacturaPago : BaseEntity
    {
        public Guid FacturaId { get; set; }
        public Factura? Factura { get; set; }
        public Guid FormaPagoId { get; set; }
        public FormaPago? FormaPago { get; set; }
        public decimal Monto { get; set; }
		
		// ctor
		
		public FacturaPago()
		{
		}
		
    }

    public class Inventario : BaseEntity
    {
        public Guid ProductoId { get; set; }
        public Producto? Producto { get; set; }
        public int Cantidad { get; set; }
		
	    // ctor
		
		public Inventario()
		{
		}
		
    }
	
	public class Proyecto : BaseEntity
{
    [Required]
    [StringLength(200)]
    public string Nombre { get; set; } = string.Empty;

    public string? Descripcion { get; set; }

    public DateTime FechaInicio { get; set; } = DateTime.Now;
    public DateTime? FechaFin { get; set; }

    public ICollection<TareaProyecto>? Tareas { get; set; }
    public ICollection<ProyectoUsuario>? Usuarios { get; set; }

    public Proyecto()
    {
        Tareas = new HashSet<TareaProyecto>();
        Usuarios = new HashSet<ProyectoUsuario>();
    }
}

public class ProyectoUsuario : BaseEntity
{
    [Required]
    public Guid ProyectoId { get; set; }
    public Proyecto? Proyecto { get; set; }

    [Required]
    public Guid UsuarioId { get; set; }
    public Usuario? Usuario { get; set; }

    public DateTime FechaAsignacion { get; set; } = DateTime.Now;
}

public class TareaProyecto : BaseEntity
{
    [Required]
    public Guid ProyectoId { get; set; }
    public Proyecto? Proyecto { get; set; }

    [Required]
    [StringLength(200)]
    public string Titulo { get; set; } = string.Empty;

    public string? Descripcion { get; set; }

    [Required]
    public Guid UsuarioId { get; set; }
    public Usuario? Usuario { get; set; }

    public string Estado { get; set; } = "Pendiente"; // Pendiente / EnProgreso / Completada
    public string Prioridad { get; set; } = "Media"; // Baja, Media, Alta

    public DateTime FechaCreacion { get; set; } = DateTime.Now;
    public DateTime? FechaCierre { get; set; }
	
	public ICollection<TareaComentario>? Comentarios { get; set; }

    public TareaProyecto()
	{
	   Comentarios = new HashSet<TareaComentario>();
	}

}

    public class TareaComentario : BaseEntity
    {
        [Required]
        public Guid TareaId { get; set; }
        public TareaProyecto? Tarea { get; set; }

        [Required]
        public Guid UsuarioId { get; set; }
        public Usuario? Usuario { get; set; }

        [Required]
        public string Texto { get; set; } = string.Empty;

        public DateTime Fecha { get; set; } = DateTime.Now;
    }


}