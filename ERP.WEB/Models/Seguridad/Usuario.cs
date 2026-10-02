using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using ERP.WEB.Models.Personal;

namespace ERP.WEB.Models.Seguridad
{
    [Table("Usuarios", Schema = "Seguridad")]
    public class Usuario
    {
        [Key]
        public int UsuarioID { get; set; }

        public int PersonalID { get; set; }

        [Required]
        [StringLength(50)]
        public string Username { get; set; } = string.Empty;

        [Required]
        [StringLength(255)]
        public string PasswordHash { get; set; } = string.Empty;

        public int RolID { get; set; }

        public bool Estado { get; set; } = true;

        public DateTime FechaCreacion { get; set; } = DateTime.Now;

        [ForeignKey("RolID")]
        public virtual Rol? Rol { get; set; }

        [ForeignKey("PersonalID")]
        public virtual Empleado? Empleado { get; set; }
    }
}