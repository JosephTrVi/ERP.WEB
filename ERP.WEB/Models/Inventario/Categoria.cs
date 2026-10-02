using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace ERP.WEB.Models.Inventario
{
    [Table("Categorias", Schema = "Inventario")]
    public class Categoria
    {
        [Key]
        public int CategoriaID { get; set; }

        [StringLength(10)]
        public string? Codigo { get; set; }

        [StringLength(100)]
        public string? Nombre { get; set; } // <--- Cambiado a anulable (string?)

        [StringLength(255)]
        public string? Descripcion { get; set; }

        public bool Estado { get; set; } = true;
    }
}