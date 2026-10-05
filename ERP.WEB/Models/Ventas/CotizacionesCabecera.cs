using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace ERP.WEB.Models.Ventas
{
    [Table("CotizacionesCabecera", Schema = "Ventas")]
    public class CotizacionesCabecera
    {
        [Key]
        public int CotizacionID { get; set; }

        [Required]
        [StringLength(20)]
        public string Codigo { get; set; } = string.Empty;

        public int? ClienteID { get; set; }

        [Required]
        [StringLength(200)]
        public string ClienteNombre { get; set; } = string.Empty;

        [Required]
        [StringLength(300)]
        public string ClienteDireccion { get; set; } = string.Empty;

        public int VendedorID { get; set; } = 1;

        public DateTime FechaEmision { get; set; } = DateTime.Now;

        public DateTime FechaEntregaAprox { get; set; } = DateTime.Now.AddDays(5);

        [Required]
        [StringLength(50)]
        public string FormaPago { get; set; } = "Contado";

        [Column(TypeName = "decimal(8, 4)")]
        public decimal TipoCambio { get; set; } = 3.75m;

        [Required]
        [StringLength(3)]
        public string Moneda { get; set; } = "PEN";

        [Column(TypeName = "decimal(12, 4)")]
        public decimal SubTotal { get; set; }

        [Column(TypeName = "decimal(12, 4)")]
        public decimal IGV { get; set; }

        [Column(TypeName = "decimal(12, 4)")]
        public decimal Total { get; set; }

        [StringLength(500)]
        public string? Observaciones { get; set; }

        [Required]
        [StringLength(25)]
        public string Estado { get; set; } = "Pendiente"; // Pendiente, Aprobado, Rechazado

        public int? AprobadorID { get; set; }

        public DateTime? FechaAprobacion { get; set; }

        [StringLength(500)]
        public string? ComentarioAprobacion { get; set; }

        public virtual ICollection<CotizacionesDetalle> Detalles { get; set; } = new List<CotizacionesDetalle>();
    }
}