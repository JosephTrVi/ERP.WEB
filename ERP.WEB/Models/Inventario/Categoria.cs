using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace ERP.WEB.Models.Inventario
{
    [Table("Categorias", Schema = "Inventario")]
    public class Categoria
    {
        [Key]
        public int CategoriaID { get; set; }

        [Required]
        [StringLength(10)]
        public string Codigo { get; set; } = string.Empty;

        [StringLength(10)]
        public string? CodigoPrefijo { get; set; }

        [Required]
        [StringLength(100)]
        public string Nombre { get; set; } = string.Empty;

        [StringLength(250)]
        public string? Descripcion { get; set; }
                public bool Estado { get; set; } = true;
        public bool EsReceta { get; set; } = true;
        public bool EsComponente { get; set; } = true;
    }
}