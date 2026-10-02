using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace ERP.WEB.Models.Personal
{
    [Table("Personal", Schema = "Personal")]
    public class Empleado
    {
        [Key]
        public int PersonalID { get; set; }

        [StringLength(15)]
        public string? CodigoAnexo { get; set; }

        [Required]
        [StringLength(2)]
        public string TipoDocumento { get; set; } = string.Empty;

        [Required]
        [StringLength(15)]
        public string NumeroDocumento { get; set; } = string.Empty;

        [Required]
        [StringLength(50)]
        public string ApellidoPaterno { get; set; } = string.Empty;

        [Required]
        [StringLength(50)]
        public string ApellidoMaterno { get; set; } = string.Empty;

        [Required]
        [StringLength(100)]
        public string Nombres { get; set; } = string.Empty;

        [StringLength(100)]
        public string? CorreoPersonal { get; set; }

        [StringLength(100)]
        public string? CorreoInstitucional { get; set; }

        [StringLength(15)]
        public string? Celular { get; set; }

        public bool Estado { get; set; } = true;

        // Propiedad calculada para obtener el nombre completo
        [NotMapped]
        public string NombreCompleto => $"{Nombres} {ApellidoPaterno} {ApellidoMaterno}".Trim();
    }
}