using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using ERP.WEB.Models.Ventas;

namespace ERP.WEB.Models.Produccion
{
    [Table("OrdenesProduccion", Schema = "Produccion")] // <--- Verificar esquema Produccion
    public class OrdenProduccion
    {
        [Key]
        public int OrdenProduccionID { get; set; }

        public int CotizacionID { get; set; }

        public int ProductoID { get; set; }

        [Column(TypeName = "decimal(12, 4)")]
        public decimal CantidadPlanificada { get; set; }

        public DateTime FechaEmision { get; set; } = DateTime.Now;

        [Required]
        [StringLength(30)]
        public string Estado { get; set; } = "Pendiente";

        [StringLength(500)]
        public string? Observaciones { get; set; }

        [ForeignKey("CotizacionID")]
        public virtual CotizacionesCabecera? Cotizacion { get; set; }

        public virtual ICollection<OrdenProduccionConsumo> Consumos { get; set; } = new List<OrdenProduccionConsumo>();
    }
}