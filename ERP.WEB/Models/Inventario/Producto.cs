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
        [StringLength(20)]
        public string SKU { get; set; } = string.Empty;

        [Required]
        [StringLength(200)]
        public string Nombre { get; set; } = string.Empty;

        public string? Descripcion { get; set; }

        public int CategoriaID { get; set; }

        public int SubcategoriaID { get; set; }

        [Required]
        [StringLength(20)]
        public string UnidadMedida { get; set; } = "UND";
        // NUEVA PROPIEDAD: Permite saber si el artículo afecta o no el inventario
        public bool ControlaStock { get; set; } = true;

        [Column(TypeName = "decimal(18, 2)")]
        public decimal StockMinimo { get; set; } = 0;

        [Column(TypeName = "decimal(18, 2)")]
        public decimal StockMaximo { get; set; } = 0;

        [Column(TypeName = "decimal(18, 2)")]
        public decimal StockActual { get; set; } = 0;

        [Column(TypeName = "decimal(18, 2)")]
        public decimal CostoPromedio { get; set; } = 0;

        [Column(TypeName = "decimal(18, 2)")]
        public decimal PrecioVenta { get; set; } = 0;

        public bool Estado { get; set; } = true;

        [ForeignKey("CategoriaID")]
        public virtual Categoria? Categoria { get; set; }

        [ForeignKey("SubcategoriaID")]
        public virtual SubCategoria? SubCategoria { get; set; }
    }
}