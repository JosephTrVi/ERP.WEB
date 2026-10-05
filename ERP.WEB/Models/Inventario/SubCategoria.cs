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

        [Required]
        [StringLength(10)]
        public string Codigo { get; set; } = string.Empty;

        [StringLength(10)]
        public string? CodigoPrefijo { get; set; }

        [Required]
        [StringLength(100)]
        public string Nombre { get; set; } = string.Empty;

        public bool Estado { get; set; } = true;

        [ForeignKey("CategoriaID")]
        public virtual Categoria? Categoria { get; set; }

        public bool EsReceta { get; set; } = true;
        public bool EsComponente { get; set; } = true;
    }
}