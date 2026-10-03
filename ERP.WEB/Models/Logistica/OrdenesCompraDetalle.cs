using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using ERP.WEB.Models.Inventario;

namespace ERP.WEB.Models.Logistica
{
    [Table("OrdenesCompraDetalle", Schema = "Compras")]
    public class OrdenesCompraDetalle
    {
        [Key]
        public int OrdenCompraDetalleID { get; set; }

        public int OrdenCompraID { get; set; }

        public int? ProductoID { get; set; }

        [Required]
        [StringLength(250)]
        public string DescripcionItem { get; set; } = string.Empty;
        

        [Column(TypeName = "decimal(12, 4)")]
        public decimal Cantidad { get; set; }

        [Required]
        [StringLength(10)]
        public string UnidadMedida { get; set; } = "UND";

        [Column(TypeName = "decimal(12, 4)")]
        public decimal PrecioUnitario { get; set; }

        [Column(TypeName = "decimal(12, 4)")]
        public decimal SubTotalItem { get; set; }

        [ForeignKey("OrdenCompraID")]
        public virtual OrdenesCompraCabecera? OrdenCompra { get; set; }

        [ForeignKey("ProductoID")]
        public virtual Producto? Producto { get; set; }
    }
}