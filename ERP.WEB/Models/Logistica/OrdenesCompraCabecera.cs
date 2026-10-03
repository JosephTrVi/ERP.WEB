using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace ERP.WEB.Models.Logistica
{
    [Table("OrdenesCompraCabecera", Schema = "Compras")]
    public class OrdenesCompraCabecera
    {
        [Key]
        public int OrdenCompraID { get; set; }

        [Required]
        [StringLength(20)]
        public string Codigo { get; set; } = string.Empty;

        public int RequerimientoID { get; set; }

        public int? ProveedorID { get; set; }

        [StringLength(50)]
        public string? NumeroCotizacionProveedor { get; set; }

        public DateTime FechaEmision { get; set; } = DateTime.Now;

        public DateTime? FechaEntrega { get; set; }

        [StringLength(50)]
        public string? FormaPago { get; set; }

        [Required]
        [StringLength(3)]
        public string Moneda { get; set; } = "PEN"; // PEN o USD

        [Column(TypeName = "decimal(12, 4)")]
        public decimal SubTotal { get; set; }

        [Column(TypeName = "decimal(12, 4)")]
        public decimal IGV { get; set; }

        [Column(TypeName = "decimal(12, 4)")]
        public decimal Total { get; set; }

        [StringLength(500)]
        public string? Observaciones { get; set; }

        [Required]
        [StringLength(20)]
        public string Estado { get; set; } = "Emitido"; // Emitido, En Proceso, Recibido, Anulado

        [ForeignKey("RequerimientoID")]
        public virtual RequerimientoCabecera? Requerimiento { get; set; }

        public virtual ICollection<OrdenesCompraDetalle> Detalles { get; set; } = new List<OrdenesCompraDetalle>();
    }
}