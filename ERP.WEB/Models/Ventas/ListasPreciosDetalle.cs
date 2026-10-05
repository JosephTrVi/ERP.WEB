using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace ERP.WEB.Models.Ventas
{
    [Table("ListasPreciosDetalle", Schema = "Ventas")]
    public class ListasPreciosDetalle
    {
        [Key]
        public int ListaPrecioDetalleID { get; set; }

        public int ListaPrecioID { get; set; }

        public int ProductoID { get; set; }

        [Column(TypeName = "decimal(12, 4)")]
        public decimal PrecioVentaSinIGV { get; set; }

        [Required]
        [StringLength(3)]
        public string Moneda { get; set; } = "PEN";

        [ForeignKey("ListaPrecioID")]
        public virtual ListasPreciosCabecera? ListaCabecera { get; set; }
    }
}