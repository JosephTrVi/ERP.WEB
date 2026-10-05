using ERP.WEB.Models.Inventario;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace ERP.WEB.Models.Produccion
{
    [Table("Recetas", Schema = "Inventario")]
    public class Receta
    {
        [Key]
        public int RecetaID { get; set; }

        [Required]
        public int ProductoTerminadoID { get; set; }

        [ForeignKey("ProductoTerminadoID")]
        public virtual Producto? ProductoTerminado { get; set; }

        [Required]
        [StringLength(200)]
        public string NombreReceta { get; set; } = string.Empty;

        public DateTime FechaCreacion { get; set; } = DateTime.Now;

        public bool Estado { get; set; } = true;

        // Relación con los componentes / insumos
        public virtual ICollection<RecetaDetalle> Detalles { get; set; } = new List<RecetaDetalle>();
    }
}