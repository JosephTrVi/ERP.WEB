using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace ERP.WEB.Models.Inventario
{
    [Table("Productos", Schema = "Inventario")]
    public class Producto
    {
        [Key]
        public int ProductoID { get; set; }

        [Required]
        [StringLength(30)]
        public string SKU { get; set; } = string.Empty;

        [Required]
        [StringLength(200)]
        public string Nombre { get; set; } = string.Empty;

        [StringLength(500)]
        public string? Descripcion { get; set; }

        public int CategoriaID { get; set; }

        public int SubcategoriaID { get; set; }

        [Required]
        [StringLength(10)]
        public string UnidadMedida { get; set; } = "UND";

        public bool ControlaStock { get; set; } = true;

        [Column(TypeName = "decimal(12, 4)")]
        public decimal StockActual { get; set; } = 0;

        [Column(TypeName = "decimal(12, 4)")]
        public decimal StockMinimo { get; set; } = 0;

        [Column(TypeName = "decimal(12, 4)")]
        public decimal StockMaximo { get; set; } = 0;

        [Column(TypeName = "decimal(12, 4)")]
        public decimal CostoPromedio { get; set; } = 0;

        [Column(TypeName = "decimal(12, 4)")]
        public decimal PrecioVenta { get; set; } = 0;

        public bool Estado { get; set; } = true;

        public DateTime FechaRegistro { get; set; } = DateTime.Now;

        [ForeignKey("CategoriaID")]
        public virtual Categoria? Categoria { get; set; }

        [ForeignKey("SubcategoriaID")]
        public virtual SubCategoria? SubCategoria { get; set; }
    }
}





