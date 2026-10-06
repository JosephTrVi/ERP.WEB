using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace ERP.WEB.Models.Inventario
{
    [Table("ProductoAtributos", Schema = "Inventario")]
    public class ProductoAtributo
    {
        [Key]
        public int ProductoAtributoID { get; set; }

        [Required]
        public int ProductoID { get; set; }

        [Required]
        public int AtributoID { get; set; }

        [Required]
        [StringLength(250)]
        public string ValorAtributo { get; set; } = null!;

        // Propiedades de navegación (Relaciones)
        [ForeignKey("ProductoID")]
        public virtual Producto? Producto { get; set; }

        [ForeignKey("AtributoID")]
        public virtual Atributo? Atributo { get; set; }
    }
}