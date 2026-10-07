using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace ERP.WEB.Models.Produccion
{
    [Table("OrdenesProduccionConsumos", Schema = "Produccion")]
    public class OrdenProduccionConsumo
    {
        [Key]
        public int OrdenProduccionConsumoID { get; set; }

        public int OrdenProduccionID { get; set; }

        public int InsumoID { get; set; }

        [Column(TypeName = "decimal(12, 4)")]
        public decimal CantidadRequerida { get; set; }

        [Column(TypeName = "decimal(12, 4)")]
        public decimal CantidadConsumida { get; set; } = 0;

        [Required]
        [StringLength(10)]
        public string UnidadMedida { get; set; } = "UND";

        [ForeignKey("OrdenProduccionID")]
        public virtual OrdenProduccion? OrdenProduccion { get; set; }
    }
}