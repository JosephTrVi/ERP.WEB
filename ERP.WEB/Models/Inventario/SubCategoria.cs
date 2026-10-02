using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace ERP.WEB.Models.Inventario
{
    [Table("SubCategorias", Schema = "Inventario")]
    public class SubCategoria
    {
        [Key]
        public int SubCategoriaID { get; set; }

        public int CategoriaID { get; set; }

        [StringLength(10)]
        public string? Codigo { get; set; }

        [StringLength(100)]
        public string? Nombre { get; set; } // <--- Cambiado a anulable (string?)

        public bool Estado { get; set; } = true;

        [ForeignKey("CategoriaID")]
        public virtual Categoria? Categoria { get; set; }
    }
}