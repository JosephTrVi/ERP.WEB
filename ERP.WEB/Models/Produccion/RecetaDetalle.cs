using ERP.WEB.Models.Inventario;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace ERP.WEB.Models.Produccion
{
    [Table("RecetaDetalles", Schema = "Inventario")]
    public class RecetaDetalle
    {
        [Key]
        public int RecetaDetalleID { get; set; }

        [Required]
        public int RecetaID { get; set; }

        [ForeignKey("RecetaID")]
        public virtual Receta? Receta { get; set; }

        [Required]
        public int InsumoID { get; set; }

        [ForeignKey("InsumoID")]
        public virtual Producto? Insumo { get; set; }

        [Column(TypeName = "decimal(18,4)")]
        public decimal CantidadRequerida { get; set; }

        [StringLength(20)]
        public string UnidadMedida { get; set; } = "UND";
    }
}