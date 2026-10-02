using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace ERP.WEB.Models.Seguridad
{
    [Table("Roles", Schema = "Seguridad")]
    public class Rol
    {
        [Key]
        public int RolID { get; set; }

        [Required]
        [StringLength(50)]
        public string NombreRol { get; set; } = string.Empty;

        [StringLength(150)]
        public string? Descripcion { get; set; }

        public bool Estado { get; set; } = true;
    }
}