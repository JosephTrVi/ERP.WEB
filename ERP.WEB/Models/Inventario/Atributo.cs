using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace ERP.WEB.Models.Inventario
{
    [Table("Atributos", Schema = "Inventario")]
    public class Atributo
    {
        [Key]
        public int AtributoID { get; set; }

        [Required]
        [StringLength(100)]
        public string NombreAtributo { get; set; } = null!;

        [StringLength(20)]
        public string? UnidadMedida { get; set; }

        [Required]
        [StringLength(20)]
        public string TipoDato { get; set; } = "Texto";

        public bool Estado { get; set; } = true;
    }
}