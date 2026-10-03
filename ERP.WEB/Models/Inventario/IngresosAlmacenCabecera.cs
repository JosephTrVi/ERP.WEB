using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using ERP.WEB.Models.Logistica;

namespace ERP.WEB.Models.Inventario
{
    [Table("IngresosAlmacenCabecera", Schema = "Inventario")]
    public class IngresosAlmacenCabecera
    {
        [Key]
        public int IngresoID { get; set; }

        [Required]
        [StringLength(20)]
        public string Codigo { get; set; } = string.Empty;

        public int OrdenCompraID { get; set; }

        public int AlmacenID { get; set; }

        public DateTime FechaIngreso { get; set; } = DateTime.Now;

        [Required]
        [StringLength(10)]
        public string SerieGuiaRemision { get; set; } = string.Empty;

        [Required]
        [StringLength(20)]
        public string NumeroGuiaRemision { get; set; } = string.Empty;

        [StringLength(10)]
        public string? SerieFactura { get; set; }

        [StringLength(20)]
        public string? NumeroFactura { get; set; }

        [StringLength(500)]
        public string? ObservacionesAlmacen { get; set; }

        public int UsuarioRegistroID { get; set; } = 1;

        [ForeignKey("OrdenCompraID")]
        public virtual OrdenesCompraCabecera? OrdenCompra { get; set; }

        [ForeignKey("AlmacenID")]
        public virtual Almacen? Almacen { get; set; }

        public virtual ICollection<IngresosAlmacenDetalle> Detalles { get; set; } = new List<IngresosAlmacenDetalle>();
    }
}