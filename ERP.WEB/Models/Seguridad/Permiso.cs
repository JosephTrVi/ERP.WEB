using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace ERP.WEB.Models.Seguridad
{
    [Table("Permisos", Schema = "Seguridad")]
    public class Permiso
    {
        [Key]
        public int PermisoID { get; set; }

        [Required]
        [StringLength(50)]
        public string CodigoPermiso { get; set; } = string.Empty;

        [Required]
        [StringLength(50)]
        public string Modulo { get; set; } = string.Empty;

        [StringLength(50)]
        public string? Subproceso { get; set; }

        [StringLength(50)]
        public string? Accion { get; set; }

        [StringLength(200)]
        public string? Descripcion { get; set; }
    }
}