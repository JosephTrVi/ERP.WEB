using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace ERP.WEB.Models.Inventario
{
    [Table("IngresosAlmacenDetalle", Schema = "Inventario")]
    public class IngresosAlmacenDetalle
    {
        [Key]
        public int IngresoDetalleID { get; set; }

        public int IngresoID { get; set; }

        public int OrdenCompraDetalleID { get; set; }

        public int? ProductoID { get; set; }

        [Column(TypeName = "decimal(12, 4)")]
        public decimal CantidadRecibida { get; set; }

        [ForeignKey("IngresoID")]
        public virtual IngresosAlmacenCabecera? IngresoCabecera { get; set; }
    }
}