using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace ERP.WEB.Models.Ventas
{
   
    [Table("CotizacionesDetalle", Schema = "Ventas")]
    public class CotizacionesDetalle
    {
        [Key]
        public int CotizacionDetalleID { get; set; }

        public int CotizacionID { get; set; }

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
        public decimal PrecioUnitarioSinIGV { get; set; }

        [Column(TypeName = "decimal(12, 4)")]
        public decimal SubTotalItem { get; set; }

        [ForeignKey("CotizacionID")]
        public virtual CotizacionesCabecera? Cotizacion { get; set; }
    }
}