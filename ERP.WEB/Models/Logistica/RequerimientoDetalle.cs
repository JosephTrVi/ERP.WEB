using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using ERP.WEB.Models.Inventario;

namespace ERP.WEB.Models.Logistica
{
    [Table("RequerimientoDetalle", Schema = "Compras")]
    public class RequerimientoDetalle   
    {
        [Key]
        public int RequerimientoDetalleID { get; set; }

        public int RequerimientoID { get; set; }

        public int? ProductoID { get; set; }

        [Required]
        [StringLength(250)]
        public string DescripcionItem { get; set; } = string.Empty;

        [Column(TypeName = "decimal(12, 4)")]
        public decimal Cantidad { get; set; }

        [Required]
        [StringLength(10)]
        public string UnidadMedida { get; set; } = "UND";

        public string? EspecificacionesTecnicas { get; set; }

        [StringLength(20)]
        public string? EstadoItem { get; set; } = "Pendiente";

        [ForeignKey("RequerimientoID")]
        public virtual RequerimientoCabecera? Cabecera { get; set; }

        [ForeignKey("ProductoID")]
        public virtual Producto? Producto { get; set; }
    }
}