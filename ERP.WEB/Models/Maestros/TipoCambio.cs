using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace ERP.WEB.Models.Maestros
{
    [Table("TipoCambio", Schema = "Maestros")]
    public class TipoCambio
    {
        [Key]
        public int TipoCambioID { get; set; }

        [Required]
        [DataType(DataType.Date)]
        public DateTime Fecha { get; set; }

        [Required]
        [StringLength(3)]
        public string MonedaOrigen { get; set; } = "USD";

        [Required]
        [StringLength(3)]
        public string MonedaDestino { get; set; } = "PEN";

        [Column(TypeName = "decimal(8, 4)")]
        public decimal PrecioCompra { get; set; }

        [Column(TypeName = "decimal(8, 4)")]
        public decimal PrecioVenta { get; set; }

        [Required]
        [StringLength(20)]
        public string OrigenData { get; set; } = "SUNAT";

        public DateTime FechaRegistro { get; set; } = DateTime.Now;
    }
}