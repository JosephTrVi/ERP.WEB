using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace ERP.WEB.Models.Logistica
{
    [Table("RequerimientoCabecera", Schema = "Compras")]
    public class RequerimientoCabecera
    {
        [Key]
        public int RequerimientoID { get; set; }
        // Campos adicionales de trazabilidad de aprobación en RequerimientosCabecera
        public int? AprobadorID { get; set; }
        [Required]
        [StringLength(20)]
        public string Codigo { get; set; } = string.Empty;

        [Required]
        [StringLength(20)]
        public string TipoRequerimiento { get; set; } = "Nacional"; // Nacional, Importacion, Servicio

        [Required]
        [StringLength(10)]
        public string Prioridad { get; set; } = "Normal"; // Baja, Normal, Alta, Urgente

        public DateTime FechaSolicitud { get; set; } = DateTime.Now;

        public DateTime FechaRequerida { get; set; }

        public int SolicitanteID { get; set; }

        public int? AreaID { get; set; }

        public int? CentroCostoID { get; set; }

        [StringLength(500)]
        public string? Observaciones { get; set; }

        [Required]
        [StringLength(20)]
        public string Estado { get; set; } = "Pendiente";

        // PROPIEDADES DE APROBACIÓN (Asegúrate de que estén presentes)
       
        public DateTime? FechaAprobacion { get; set; }

        [StringLength(255)]
        public string? ComentarioAprobacion { get; set; }

        public virtual ICollection<RequerimientoDetalle> Detalles { get; set; } = new List<RequerimientoDetalle>();
    }
}